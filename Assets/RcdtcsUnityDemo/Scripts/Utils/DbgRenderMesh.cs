using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

//Helper classes for procedural geometry rendering
public class DbgRenderTriangle{
	public Vector3[] m_Verts = new Vector3[3];
	public Color[] m_Colors = new Color[3] { Color.white, Color.white, Color.white};

	public DbgRenderTriangle(Vector3 a, Vector3 b, Vector3 c, Color color){
		m_Verts[0] = a;
		m_Verts[1] = b;
		m_Verts[2] = c;
		for (int i=0;i<m_Colors.Length;++i){
			m_Colors[i] = color;
		}
	}
	public DbgRenderTriangle(Vector3 a, Vector3 b, Vector3 c, Color colora, Color colorb, Color colorc){
		m_Verts[0] = a;
		m_Verts[1] = b;
		m_Verts[2] = c;
		m_Colors[0] = colora;
		m_Colors[1] = colorb;
		m_Colors[2] = colorc;
	}
}

//Takes care of rendering a collection of triangles populated through various helper functions
//Can also use world space text to display information in the game world
public class DbgRenderMesh{
	private List<DbgRenderTriangle> m_Triangles = new List<DbgRenderTriangle>();
	
	private GameObject m_GameObject = null;
	private MeshFilter m_MeshFilter = null;
	private MeshRenderer m_MeshRenderer = null;
	private Mesh m_Mesh = null;
	private Text m_LabelTemplate;
	private List<Text> m_Labels = new List<Text>();

	private bool m_BoundsComputed = false;
	private Vector3 m_Min = Vector3.zero;
	private Vector3 m_Max = Vector3.zero;
	private Vector3 m_BBCenter = Vector3.zero;

	//Set this if you want to use labels
	public void SetLabelTemplate(Text text){
		m_LabelTemplate = text;
	}

	public static Vector3 GetBoundingBoxCenter(List<DbgRenderTriangle> tris){
		return GetBoundingBoxCenter(tris, 0, tris.Count);
	}

	public static Vector3 GetBoundingBoxCenter(List<DbgRenderTriangle> tris, int startIndex, int endIndex){
		Vector3 min = Vector3.zero;
		Vector3 max = Vector3.zero;
		return GetBoundingBoxCenter(tris, startIndex, endIndex, ref min, ref max);
	}

	public static Vector3 GetBoundingBoxCenter(List<DbgRenderTriangle> tris, int firstIndex, int lastIndex, ref Vector3 min, ref Vector3 max){
		min = new Vector3( float.MaxValue, float.MaxValue, float.MaxValue);
		max = new Vector3( float.MinValue, float.MinValue, float.MinValue);
		for (int i=firstIndex; i<lastIndex;++i){
			DbgRenderTriangle tri = tris[i];
			foreach( Vector3 vec in tri.m_Verts){
				min.x = Mathf.Min(vec.x, min.x);
				min.y = Mathf.Min(vec.y, min.y);
				min.z = Mathf.Min(vec.z, min.z);
				max.x = Mathf.Max(vec.x, max.x);
				max.y = Mathf.Max(vec.y, max.y);
				max.z = Mathf.Max(vec.z, max.z);
			}
		}
		if (tris.Count > 0){
			return min + (max - min) / 2.0f;
		}
		return Vector3.zero;
	}

	public Vector3 GetBoundingBoxCenter(int startIndex, int endIndex){
		Vector3 min = Vector3.zero;
		Vector3 max = Vector3.zero;
		return GetBoundingBoxCenter(m_Triangles, startIndex, endIndex, ref min, ref max);
	}

	public Vector3 GetBoundingBoxTop(int startIndex, int endIndex){
		Vector3 min = Vector3.zero;
		Vector3 max = Vector3.zero;
		Vector3 bbCenter = GetBoundingBoxCenter(m_Triangles, startIndex, endIndex, ref min, ref max);
		return new Vector3(bbCenter.x, max.y, bbCenter.z);
	}

	public int GetTriangleCount(){
		return m_Triangles.Count;
	}

	private void ComputeBounds(){
		m_BBCenter = GetBoundingBoxCenter(m_Triangles,0, m_Triangles.Count, ref m_Min, ref m_Max);
		m_BoundsComputed = true;
	}

	public void LabelBoundingBoxTop(string textString){
		if (!m_BoundsComputed){
			ComputeBounds();
		}
		if (m_BoundsComputed){
			Vector3 bbTop = new Vector3(m_BBCenter.x, m_Max.y, m_BBCenter.z);
			AddLabel(bbTop, textString);
		}else{
			Debug.LogError("Error, could not find center of render mesh");
		}
	}

	public void LabelCenter(string textString){
		if (!m_BoundsComputed){
			ComputeBounds();
		}
		if (m_BoundsComputed){
			AddLabel(m_BBCenter, textString);
		}else{
			Debug.LogError("Error, could not find center of render mesh");
		}
	}

	public void AddLabel(Vector3 pos, string textString){
		if (m_LabelTemplate){
			Text text= Object.Instantiate( m_LabelTemplate ) as Text;
			text.rectTransform.parent = m_LabelTemplate.transform.parent;
			text.rectTransform.position = pos;
			//text.rectTransform.anchoredPosition = ( pos /*+ Vector3.up * .15f*/ );
			text.gameObject.SetActive(true);
			text.text = textString;
			m_Labels.Add(text);
		}else{
			Debug.LogError("Error, could not find center of render mesh");
		}
	}

	private void RenderTo(Mesh mesh){
		mesh.Clear();
		
		int triCount = m_Triangles.Count;
		
		Vector3[] verts = new Vector3[3 * triCount];
		int[] tris = new int[3* triCount];
		Color[] colors = new Color[3*triCount];
		
		for (int i=0;i<triCount;++i){
			DbgRenderTriangle tri = m_Triangles[i];
			int v = i*3;
			for (int j=0;j<3;++j){
				verts[v+j] = tri.m_Verts[j];
				tris[v+j] = v+j;
				colors[v+j] = tri.m_Colors[j];
			}
		}
		
		mesh.vertices = verts;
		mesh.triangles = tris;
		mesh.colors = colors;
	}
	
	public void Rebuild(){
		if (m_Mesh){
			RenderTo(m_Mesh);
		}
	}
	
	public void Clear(){
		m_Triangles.Clear();
		foreach (Text text in m_Labels){
			Object.Destroy( text.gameObject );
		}
		m_Labels.Clear();
		m_BoundsComputed = false;
	}
	
	public void AddTriangle(DbgRenderTriangle tri){
		m_Triangles.Add(tri);
		m_BoundsComputed = false;
	}
	
	public void AddConvexPolygon(List<Vector3> vertices, Color color){
		int vertCount = vertices.Count;
		
		for (int i=2;i < vertCount;++i){
			AddTriangle( new DbgRenderTriangle ( vertices[0], vertices[i - 1], vertices[i], color ));
		}
	}

	public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool twoSided, Color color){
		// A B C + A C D
		AddTriangle( new DbgRenderTriangle (a,b,c,color) );
		AddTriangle( new DbgRenderTriangle (a,c,d,color) );
		if (twoSided){
			AddTriangle( new DbgRenderTriangle (b,a,d,color) );
			AddTriangle( new DbgRenderTriangle (b,d,c,color) );
		}
	}

	public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool twoSided, Color colora, Color colorb, Color colorc, Color colord){
		// A B C + A C D
		AddTriangle( new DbgRenderTriangle (a,b,c,colora,colorb,colorc) );
		AddTriangle( new DbgRenderTriangle (a,c,d,colora,colorc,colord) );
		if (twoSided){
			AddTriangle( new DbgRenderTriangle (b,a,d,colorb,colora,colord) );
			AddTriangle( new DbgRenderTriangle (b,d,c,colorb,colord,colorc) );
		}
	}

	public void AddPath( float[] path, int vertCount, float size, Color col1, Color col2){
		if (path != null && path.Length > 3){
            for (int i = 1; i < vertCount;++i) {
                int v = i * 3;
				Vector3 a = new Vector3( path[v-3], path[v-2], path[v-1]);
				Vector3 b = new Vector3( path[v+0], path[v+1], path[v+2]);
				AddVerticalQuad(a,b,size,i%2==1 ? col1 : col2);
			}
		}
	}

	public void AddVerticalQuad(Vector3 start, Vector3 end, float size, Color col){
		AddQuad(start + Vector3.up * size, end + Vector3.up * size, end, start, true, col);
	}

	public void AddVerticalQuad(Vector3 start, Vector3 end, float size, Color colstart, Color colend){
		AddQuad(start + Vector3.up * size, end + Vector3.up * size, end, start, true, colstart, colend, colend, colstart);
	}

	public void AddPath( Vector3[] path, int vertCount, float size, Color col1, Color col2){
		for (int i=1;i<vertCount;++i){
			AddVerticalQuad(path[i - 1], path[i], size, i%2==1 ? col1 : col2);
		}
	}

    public void AddMesh(Vector3[] verts, int[] tris, Color col) {
        for (int i = 0; i < tris.Length; i+=3) {
            AddTriangle( new DbgRenderTriangle(
                verts[ tris[i+0] ],
                verts[ tris[i+1] ],
                verts[ tris[i+2] ],
                col
                ));
        }
    }
    public void AddMesh(float[] verts, int[] tris, Color col) {
        Vector3[] av = new Vector3[3];
        for (int i = 0; i < tris.Length; i += 3) {
            for (int j = 0; j < 3; ++j) {
                int vertId = tris[i + j] * 3;
                av[j].x = verts[vertId + 0];
                av[j].y = verts[vertId + 1];
                av[j].z = verts[vertId + 2];
            }
            AddTriangle(new DbgRenderTriangle(
                av[0],
                av[1],
                av[2],
                col
                ));
        }
    }

    public void CreateGameObject(string name, Material material) {
		if (m_GameObject == null){
			GameObject gao = new GameObject(name);
			gao.transform.position = Vector3.zero;
			gao.transform.rotation = Quaternion.identity;
			Setup(gao);
		}else{
			m_GameObject.name = name;
		}
		m_GameObject.renderer.material = material;
	}
	
	private void Setup(GameObject target){
		m_GameObject = target;
		
		m_MeshFilter = target.GetComponent<MeshFilter>();
		if (m_MeshFilter == null){
			m_MeshFilter = target.AddComponent<MeshFilter>();
		}
		m_MeshRenderer = target.GetComponent<MeshRenderer>();
		if (m_MeshRenderer == null){
			m_MeshRenderer = target.AddComponent<MeshRenderer>();
		}
		m_Mesh = new Mesh();
		m_MeshFilter.mesh = m_Mesh;
		m_BoundsComputed = false;
	}
}

