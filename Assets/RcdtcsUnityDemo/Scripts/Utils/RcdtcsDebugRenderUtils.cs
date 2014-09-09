using UnityEngine;

//Various functions to render computed Recast/Detour data
public static partial class RcdtcsUnityUtils{

	public static float VaryColorComponent(float comp) {
		float smallDelta = 1.0f / 20.0f;
		if (comp > 0.5f) {
			smallDelta = -smallDelta;
		}
		return smallDelta + comp;
	}

	public static Color VaryColor(Color color) {
		return new Color(
			VaryColorComponent(color.r),
			VaryColorComponent(color.g),
			VaryColorComponent(color.b));
	}

	private const int c_RandomSeed = 9;

	public static void ShowRecastNavmesh(DbgRenderMesh renderMesh, Recast.rcPolyMesh pmesh, Recast.rcConfig config){
		
		renderMesh.Clear();
		
		UnityEngine.Random.seed = c_RandomSeed;
		
		int npolys = pmesh.npolys;
		int nvp = pmesh.nvp;
		int[] tri = new int[3];
		Vector3[] verts = new Vector3[3];
		for (int i = 0; i < npolys; ++i)
		{
			int pIndex = i * nvp * 2;
			Color col = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
			for (int j = 2; j < nvp; ++j)
			{
				if ( pmesh.polys[pIndex + j] == Recast.RC_MESH_NULL_IDX) 
					break;
				tri[0] = pmesh.polys[pIndex];
				tri[1] = pmesh.polys[pIndex + j - 1];
				tri[2] = pmesh.polys[pIndex + j];
				
				for (int k=0;k<3;++k){
					int vIndex = tri[k]*3;
					verts[k].x = config.bmin[0] + pmesh.verts[vIndex + 0]*pmesh.cs;
					verts[k].y = config.bmin[1] + (pmesh.verts[vIndex + 1]+1)*pmesh.ch + 0.1f;
					verts[k].z = config.bmin[2] + pmesh.verts[vIndex + 2]*pmesh.cs;
				}
				col = VaryColor(col);
				renderMesh.AddTriangle(new DbgRenderTriangle(verts[0], verts[1], verts[2], col));
			}
		}
		renderMesh.Rebuild();
	}

	public static void ShowRecastDetailMesh(DbgRenderMesh renderMesh, Recast.rcPolyMeshDetail dmesh ){
		
		renderMesh.Clear();
		
		UnityEngine.Random.seed = c_RandomSeed;
		
		int nmeshes = dmesh.nmeshes;
		for (int i = 0; i < nmeshes; ++i) {
			
			Color col = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
			
			uint bverts = dmesh.meshes[i*4];
			uint btris = dmesh.meshes[i*4 + 2];
			uint ntris = dmesh.meshes[i*4 + 3];;
			uint trisStart = btris*4;
			for (uint j = 0; j < ntris; ++j)
			{
				Vector3[] verts = new Vector3[3];
				for (int k=0;k<3;++k){
					int vertStart = (int) (bverts+ dmesh.tris[trisStart + j*4 + k ]) * 3 ;
					verts[k].x = dmesh.verts[vertStart + 0];
					verts[k].y = dmesh.verts[vertStart + 1];
					verts[k].z = dmesh.verts[vertStart + 2];
				}
				col = VaryColor(col);
				renderMesh.AddTriangle(new DbgRenderTriangle(
					verts[0]
					, verts[1] 
					, verts[2] 
					, col));
			}
		}
		renderMesh.Rebuild();
	}

	public static void ShowRawContours(DbgRenderMesh renderMesh, Recast.rcContourSet cset){
		
		
		renderMesh.Clear();
		
		UnityEngine.Random.seed = c_RandomSeed;
		
		float[] orig = cset.bmin;
		float cs = cset.cs;
		float ch = cset.ch;
		
		for (int i = 0; i < cset.nconts; ++i)
		{
			Recast.rcContour c = cset.conts[i];
			
			if (c.nrverts == 0){
				continue;
			}
			
			Color col = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
			
			int triStartIndex = renderMesh.GetTriangleCount();
			
			Vector3 start = new Vector3(orig[0] + c.rverts[0]*cs,
			                            orig[1] + (c.rverts[1]+1+(i&1))*ch,
			                            orig[2] + c.rverts[2]*cs);
			Vector3 a = Vector3.zero;
			Vector3 b = start;
			for (int j = 1; j < c.nrverts; ++j)
			{
				a = b;
				int vStart = j*4;
				b = new Vector3(orig[0] + c.rverts[vStart + 0]*cs,
				                orig[1] + (c.rverts[vStart + 1]+1+(i&1))*ch,
				                orig[2] + c.rverts[vStart + 2]*cs);
				if (j > 0){
					renderMesh.AddVerticalQuad(a, b, 0.5f, col);
				}
			}
			// Loop last segment.
			renderMesh.AddVerticalQuad(b, start, 0.5f, col);
			
			int triEndIndex = renderMesh.GetTriangleCount();
			
			Vector3 labelPos = renderMesh.GetBoundingBoxTop(triStartIndex, triEndIndex);
			
			renderMesh.AddLabel(labelPos, "contour " + i + "\nReg: " + c.reg + "\nnrverts:" + c.nrverts + "\nnverts:" + c.nverts);
		}
		/*
			dd->end();
			
			dd->begin(DU_DRAW_POINTS, 2.0f);	
			
			for (int i = 0; i < cset.nconts; ++i)
			{
				const rcContour& c = cset.conts[i];
				unsigned int color = duDarkenCol(duIntToCol(c.reg, a));
				
				for (int j = 0; j < c.nrverts; ++j)
				{
					const int* v = &c.rverts[j*4];
					float off = 0;
					unsigned int colv = color;
					if (v[3] & RC_BORDER_VERTEX)
					{
						colv = duRGBA(255,255,255,a);
						off = ch*2;
					}
					
					float fx = orig[0] + v[0]*cs;
					float fy = orig[1] + (v[1]+1+(i&1))*ch + off;
					float fz = orig[2] + v[2]*cs;
					dd->vertex(fx,fy,fz, colv);
				}
			}
			dd->end();
			*/
		renderMesh.Rebuild();
	}

	public static void ShowContours(DbgRenderMesh renderMesh, Recast.rcContourSet cset)
	{
		renderMesh.Clear();
		
		UnityEngine.Random.seed = c_RandomSeed;
		
		float[] orig = cset.bmin;
		float cs = cset.cs;
		float ch = cset.ch;
		
		for (int i = 0; i < cset.nconts; ++i)
		{
			Recast.rcContour c = cset.conts[i];
			if (c.nverts == 0)
				continue;
			
			Color col = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
			Color bcol = VaryColor(col);
			
			for (int j = 0, k = c.nverts-1; j < c.nverts; k=j++)
			{
				int vaStart = k*4;
				int vbStart = j*4;
				Color segCol = ((c.verts[vaStart + 3] & Recast.RC_AREA_BORDER) != 0) ? bcol : col; 
				
				Vector3 start = new Vector3(orig[0] + c.verts[vaStart + 0]*cs, 
				                            orig[1] + (c.verts[vaStart + 1]+1+(i&1))*ch, 
				                            orig[2] + c.verts[vaStart + 2]*cs);
				Vector3 end = new Vector3(orig[0] + c.verts[vbStart + 0]*cs, 
				                          orig[1] + (c.verts[vbStart + 1]+1+(i&1))*ch, 
				                          orig[2] + c.verts[vbStart + 2]*cs);
				
				renderMesh.AddVerticalQuad(start, end, 0.5f, segCol);
			}
		}
		
		/*
			dd->begin(DU_DRAW_POINTS, 3.0f);
			
			for (int i = 0; i < cset.nconts; ++i)
			{
				const rcContour& c = cset.conts[i];
				unsigned int color = duDarkenCol(duIntToCol(c.reg, a));
				for (int j = 0; j < c.nverts; ++j)
				{
					const int* v = &c.verts[j*4];
					float off = 0;
					unsigned int colv = color;
					if (v[3] & RC_BORDER_VERTEX)
					{
						colv = duRGBA(255,255,255,a);
						off = ch*2;
					}
					
					float fx = orig[0] + v[0]*cs;
					float fy = orig[1] + (v[1]+1+(i&1))*ch + off;
					float fz = orig[2] + v[2]*cs;
					dd->vertex(fx,fy,fz, colv);
				}
			}
			*/
		renderMesh.Rebuild();
	}

	public static void ShowTilePolyDetails(DbgRenderMesh renderMesh, Detour.dtNavMesh navMesh, int tileId) {
		
		renderMesh.Clear();
		
		UnityEngine.Random.seed = c_RandomSeed;
		
		if (navMesh == null) {
			renderMesh.Rebuild();
			return;
		}
		
		Detour.dtMeshTile tile = navMesh.getTile(tileId);
		
		if (tile == null) {
			Debug.LogError("RcdtcsUnityUtils.ShowTilePolyDetails : Tile " + tileId + " does not exist.");
			return;
		}

		int detailMeshCount = tile.detailMeshes.Length;
		for (int i = 0; i < detailMeshCount; ++i) {
			Detour.dtPolyDetail pd = tile.detailMeshes[i];
			Detour.dtPoly poly = tile.polys[i];
			
			Color col = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
			
			for (int j = 0; j < pd.triCount; ++j) {
				int tStart = (int)(pd.triBase + j) * 4;
				int[] vStarts = new int[3];
				float[][] vSrc = new float[3][];
				for (int k = 0; k < 3; ++k) {
					byte tk = tile.detailTris[tStart + k];
					byte vCount = poly.vertCount;
					if (tk < vCount) {
						vStarts[k] = poly.verts[tk] * 3;
						vSrc[k] = tile.verts;
					} else {
						vStarts[k] = (int)(pd.vertBase + (tk - vCount)) * 3;
						vSrc[k] = tile.detailVerts;
					}
				}
				Vector3 a = new Vector3(vSrc[0][vStarts[0] + 0], vSrc[0][vStarts[0] + 1], vSrc[0][vStarts[0] + 2]);
				Vector3 b = new Vector3(vSrc[1][vStarts[1] + 0], vSrc[1][vStarts[1] + 1], vSrc[1][vStarts[1] + 2]);
				Vector3 c = new Vector3(vSrc[2][vStarts[2] + 0], vSrc[2][vStarts[2] + 1], vSrc[2][vStarts[2] + 2]);
				
				col = VaryColor(col);
				renderMesh.AddTriangle(new DbgRenderTriangle(a, b, c, col));
			}
		}
		renderMesh.Rebuild();
	}
}