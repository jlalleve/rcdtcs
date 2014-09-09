using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Threading;

//All-in one sample component that demonstrates all tested Rcdtcs features. 
public class RcdtcsSampleSoloMeshComponent : MonoBehaviour {

	//Public settings managed by UI
	//Parameters to use for the rc/dt navmesh computing
    public RcdtcsUnityUtils.RecastMeshParams m_NavMeshParams = new RcdtcsUnityUtils.RecastMeshParams();
	//Set to true to ask for navmesh computing, will revert to false when call is made
	public bool m_dbg_ComputeMesh = true;
	//Wether navmesh computing should be done in a separate thread or in a blocking call
	public bool m_Threaded = true;

	//Debug/Testing Rendering
	public enum PathType { Straight, Smooth };
	
	public PathType m_PathType = PathType.Smooth;
	private PathType m_ComputedPathType = PathType.Smooth;
	
	public enum DebugView { None, PolyMesh, NavMesh, DetailMesh, ContourSet, RawContourSet };
	public DebugView m_DebugView = DebugView.None;
	private DebugView m_CurrentDebugView = DebugView.None;

	//Debug Rendering Material (expects a shader that supports vertex colors)
	public Material m_DebugRenderMeshMaterial = null;

	//Dynamic UI elements
	//A Text element using a world space camera, with settings enabling 1:1 world positioning
	public Text m_3DTextTemplate = null;
	//Displays the build time next to the build button
	public Text m_BuildTimeUIText = null;
	//The log window's text
	public Text m_LogUIText = null;
	//The top level element of the log window to hide/show
	public RectTransform m_LogWindowRoot = null;

	public RectTransform[] m_RaycastBlockingUI;

	//Private variables
	private bool m_ShowLog = false;

	//Computing thread when Threaded is true
    private Thread m_Thread = null;
	//A volatile function accessed by both thread to communicate on task completion
    private volatile bool m_ThreadLock = false;
	//An internal watch variable to catch threadLock freeing
    private bool m_ThreadWatch = false;

	//Self contained nav mesh interface
	private RcdtcsUnityUtils.SystemHelper m_System = new RcdtcsUnityUtils.SystemHelper();

	//Helper classes for path finding, works with a SystemHelper
	private RcdtcsUnityUtils.StraightPath m_StraightPath = null;
	private RcdtcsUnityUtils.SmoothPath m_SmoothPath = null;

	//Helper classes to display debug geometries in the game world
	private DbgRenderMesh m_DbgRenderMesh = new DbgRenderMesh();
	private DbgRenderMesh m_DbgPathRenderer = new DbgRenderMesh();

	//Path finding test picking positions
	private Vector3 m_StartPos = Vector3.zero;
	private Vector3 m_EndPos = Vector3.zero;

	public void Start(){


        if (m_DebugRenderMeshMaterial == null) {
            Shader vertexColor = Shader.Find("Custom/VertexColorShader");
            m_DebugRenderMeshMaterial = new Material(vertexColor);
        }

		m_DbgRenderMesh.CreateGameObject("Dbg Render Mesh",m_DebugRenderMeshMaterial);
		m_DbgPathRenderer.CreateGameObject("Dbg Path Renderer Mesh",m_DebugRenderMeshMaterial);

		m_DbgRenderMesh.SetLabelTemplate(m_3DTextTemplate);
		m_DbgPathRenderer.SetLabelTemplate(m_3DTextTemplate);
	}

	public bool ThreadIsBusy(){
		return m_ThreadWatch;
	}

    private void ComputeSystemThread() {
        m_System.ComputeSystem();
        m_ThreadLock = false;
    }

	public void OnComputeComplete(){
		if (m_BuildTimeUIText != null) {
			m_BuildTimeUIText.text = m_System.m_ctx.getAccumulatedTime(Recast.rcTimerLabel.RC_TIMER_TOTAL) + " ms";
		}
		RecomputePath();
		//Force refresh debug renderer by invalidating watch
		m_CurrentDebugView = DebugView.None;
	}

    public void RecomputeSystem() {

		if (ThreadIsBusy()){
			return;
		}

		m_System.SetNavMeshParams(m_NavMeshParams);
		m_System.ClearComputedData();
		m_System.ClearMesh();

		MeshFilter mf = GetComponent<MeshFilter>();
		if (mf == null) {
			return;
		}

		Mesh mesh = mf.mesh;
		if (mesh == null) {
			return;
		}

		m_System.AddMesh(mesh, transform.gameObject);

		if (m_Threaded){
			m_ThreadWatch = true;
			m_ThreadLock = true;
			m_Thread = new Thread(ComputeSystemThread);
			m_Thread.Start();
		}else{
			m_System.ComputeSystem();
			OnComputeComplete();
		}
    }

    public void UpdateLog() {
        if (!m_ShowLog) {
            return;
        }

		if (ThreadIsBusy()) {
            return;
        }

        if (m_System != null && m_System.m_ctx != null && m_System.m_ctx.m_Dirty && m_LogUIText != null) {
            m_LogUIText.text = m_System.m_ctx.dumpLog();
            m_System.m_ctx.m_Dirty = false;

#if UNITY_STANDALONE
            string filename = Application.persistentDataPath + "/log.txt"; 
            StreamWriter stream = File.CreateText(filename);
            if (stream != null) {
				string fullLog = m_System.GetNavMeshParams().ToString() + "\n" + m_LogUIText.text;
                stream.Write(fullLog);
                stream.Close();
                Debug.Log("Log output saved " + filename);
            }
#endif
        }
         
    }

	public void Update(){
		UpdateThreadWatch();

		if (ThreadIsBusy()){
			return;
		}

        SystemUpdate();
        DebugViewUpdate();
		PathDemoUpdate();
        UpdateLog();
	}

	public void UpdateThreadWatch(){
		//Sync with compute thread by reading m_ThreadLock, to see if it's done
		if (m_ThreadWatch && !m_ThreadLock) {
			OnComputeComplete();
			m_ThreadWatch = false;
		}
	}

    public void SystemUpdate() {
        if (m_dbg_ComputeMesh) {
            RecomputeSystem();
            m_dbg_ComputeMesh = false;
        }
    }

    public void DebugViewUpdate() {
        if (m_CurrentDebugView != m_DebugView) {
            m_CurrentDebugView = m_DebugView;
            m_DbgRenderMesh.Clear();

            switch (m_CurrentDebugView) {
                case DebugView.PolyMesh:
                    RcdtcsUnityUtils.ShowRecastNavmesh(m_DbgRenderMesh, m_System.m_pmesh, m_System.m_cfg);
                    break;
                case DebugView.NavMesh:
                    RcdtcsUnityUtils.ShowTilePolyDetails(m_DbgRenderMesh, m_System.m_navMesh, 0);
                    break;
                case DebugView.DetailMesh:
                    RcdtcsUnityUtils.ShowRecastDetailMesh(m_DbgRenderMesh, m_System.m_dmesh);
                    break;
                case DebugView.None:
                    m_DbgRenderMesh.Rebuild();
                    break;
				case DebugView.ContourSet:
					RcdtcsUnityUtils.ShowContours(m_DbgRenderMesh, m_System.m_cset);
					break;
				case DebugView.RawContourSet:
					RcdtcsUnityUtils.ShowRawContours(m_DbgRenderMesh, m_System.m_cset);
					break;
            }
        }
    }



    public void PathDemoUpdate() {
		bool mainClick = Input.GetButtonDown("Place Start Point"); //Input.GetMouseButtonDown(0);
		bool altClick = Input.GetButtonDown("Place End Point");
		if (mainClick || altClick){

			//Don't raycast through new UI stuff
			if (EventSystemManager.currentSystem.IsPointerOverEventSystemObject()){
				return;
			}

			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
            Debug.DrawRay(ray.origin, ray.direction * 1000.0f, Color.red, 5.0f);
			if (Physics.Raycast (ray.origin, ray.direction, out hit, 1000.0f)) {
				if (altClick){
					m_EndPos = hit.point;
					//Debug.Log ("Set End Point " + hit.point);
				}else if (mainClick){
					m_StartPos = hit.point;
					//Debug.Log ("Set Start Point " + hit.point);
				}
                RecomputePath();
			}
		}
        if (m_PathType != m_ComputedPathType) {
            RecomputePath();
        }
	}

	private void RecomputePath(){
		if (m_EndPos != Vector3.zero && m_StartPos != Vector3.zero){
            m_DbgPathRenderer.Clear();
            m_ComputedPathType = m_PathType;
			if (m_PathType == PathType.Smooth){
                m_SmoothPath = RcdtcsUnityUtils.ComputeSmoothPath(m_System.m_navQuery, m_StartPos, m_EndPos);
                m_DbgPathRenderer.AddPath(m_SmoothPath.m_smoothPath, m_SmoothPath.m_nsmoothPath, 1.25f, Color.black, Color.white);
			}else if (m_PathType == PathType.Straight){
                m_StraightPath = RcdtcsUnityUtils.ComputeStraightPath(m_System.m_navQuery, m_StartPos, m_EndPos);
				m_DbgPathRenderer.AddPath(m_StraightPath.m_straightPath, m_StraightPath.m_straightPathCount, 1.25f, Color.black, Color.white);
			}
            m_DbgPathRenderer.Rebuild();
		}
	}

    //UI interface
    private void UI_SetDebugView(DebugView debugView) {
        m_DebugView = debugView;
    }
    public void UI_ShowNavMesh(bool toggle) {
        if (toggle)
            UI_SetDebugView(DebugView.NavMesh);
    }
    public void UI_ShowPolyMesh(bool toggle) {
        if (toggle)
            UI_SetDebugView(DebugView.PolyMesh);
    }
    public void UI_ShowDetailMesh(bool toggle) {
        if (toggle)
            UI_SetDebugView(DebugView.DetailMesh);
    }
	public void UI_ShowContours(bool toggle) {
		if (toggle)
			UI_SetDebugView(DebugView.ContourSet);
	}
	public void UI_ShowRawContours(bool toggle) {
		if (toggle)
			UI_SetDebugView(DebugView.RawContourSet);
	}
    public void UI_ShowNone(bool toggle) {
        if (toggle)
            UI_SetDebugView(DebugView.None);
    }

    private void UI_SetPath(PathType pathType) {
        m_PathType = pathType;
    }
    public void UI_UseSmoothPath(bool toggle) {
        if (toggle)
            UI_SetPath(PathType.Smooth);
    }
    public void UI_UseStraightPath(bool toggle) {
        if (toggle)
            UI_SetPath(PathType.Straight);
    }
    
    public void UI_Params_SetCellSize(float sliderValue){
        m_NavMeshParams.m_cellSize = sliderValue;
    }
    public void UI_Params_SetCellHeight(float sliderValue){
        m_NavMeshParams.m_cellHeight = sliderValue;
    }
    public void UI_Params_SetAgentHeight(float sliderValue){
        m_NavMeshParams.m_agentHeight = sliderValue;
    }
    public void UI_Params_SetAgentRadius(float sliderValue) {
        m_NavMeshParams.m_agentRadius = sliderValue;
    }
    public void UI_Params_SetAgentMaxClimb(float sliderValue){
        m_NavMeshParams.m_agentMaxClimb = sliderValue;
    }
    public void UI_Params_SetAgentMaxSlope(float sliderValue) {
        m_NavMeshParams.m_agentMaxSlope = sliderValue;
    }

    public void UI_Params_SetRegionMinSize(float sliderValue) {
        m_NavMeshParams.m_regionMinSize = sliderValue;
    }
    public void UI_Params_SetRegionMergeSize(float sliderValue) {
        m_NavMeshParams.m_regionMergeSize = sliderValue;
    }

    public void UP_Params_SetMonotonePartitioning(bool toggleValue) {
        m_NavMeshParams.m_monotonePartitioning = toggleValue;
    }

    public void UI_Params_SetEdgeMaxLen(float sliderValue) {
        m_NavMeshParams.m_edgeMaxLen = sliderValue;
    }
    public void UI_Params_SetEdgeMaxError(float sliderValue) {
        m_NavMeshParams.m_edgeMaxError = sliderValue;
    }

    public void UI_Params_SetVertsPerPoly(float sliderValue) {
        m_NavMeshParams.m_vertsPerPoly = sliderValue;
    }
    public void UI_Params_SetDetailSampleDist(float sliderValue) {
        m_NavMeshParams.m_detailSampleDist = sliderValue;
    }
    public void UI_Params_SetSampleMaxError(float sliderValue) {
        m_NavMeshParams.m_detailSampleMaxError = sliderValue;
    }

	public void UI_SetThreaded(bool threaded){
		if (threaded){
			m_Threaded = true;
		}
	}
	public void UI_SetBlocking(bool blocking){
		if (blocking){
			m_Threaded = false;
		}
	}

    public void UI_RebuildSystem() {
        RecomputeSystem();
    }

    public void UI_ShowLog(bool log) {
        m_ShowLog = log;
        if (m_ShowLog) {
            UpdateLog();
        }
        if (m_LogWindowRoot != null) {
            m_LogWindowRoot.gameObject.SetActive(log);
        }
    }
}
