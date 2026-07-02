using System.Collections.Generic;
using UnityEngine;

public class GridDrawer : MonoBehaviour
{
    [SerializeField]
    private MeshFilter meshFilter;
    [SerializeField]
    private MeshRenderer meshRenderer;
    [SerializeField]
    private Material gridMaterial;

    private void Awake()
    {
        InitInternal();
    }

    public void InitInternal()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (gridMaterial != null) meshRenderer.material = gridMaterial;
    }

    public void DrawMesh(Dictionary<Vector2Int, CellData> gridData, float cellSize, Vector3 startPos)
    {
        // 메쉬 생성 및 이름 설정
        Mesh mesh = new Mesh();
        mesh.name = "GridMesh";

        // 점, 삼각형, UV를 저장할 리스트 선언
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        // 점 인덱스 저장용 변수
        int vertexIndex = 0;
        // 셀 정 중앙 계산용 셀 절반 사이즈 변수
        float cellHalfSize = cellSize / 2f;

        Debug.Log(gridData.Count);

        // 모든 셀에 대해서 반복
        foreach(KeyValuePair<Vector2Int, CellData> kvp in gridData)
        {
            Vector2Int index = kvp.Key;
            Debug.Log(index);

            // 각 셀의 중앙 좌표 계산
            float centerX = (index.x * cellSize) + cellHalfSize + startPos.x;
            float centerZ = (index.y * cellSize) + cellHalfSize + startPos.z;

            Vector3 center_global = new Vector3(centerX, 0, centerZ);
            Vector3 center_local = transform.InverseTransformPoint(center_global);
          
            float localCenterX = center_local.x;
            float localCenterZ = center_local.z;

            // 하나의 셀(Quad)을 구성하는 4개의 꼭짓점
            vertices.Add(new Vector3(localCenterX - cellHalfSize, 0, localCenterZ - cellHalfSize)); // 좌하 (0)
            vertices.Add(new Vector3(localCenterX - cellHalfSize, 0, localCenterZ + cellHalfSize)); // 좌상 (1)
            vertices.Add(new Vector3(localCenterX + cellHalfSize, 0, localCenterZ + cellHalfSize)); // 우상 (2)
            vertices.Add(new Vector3(localCenterX + cellHalfSize, 0, localCenterZ - cellHalfSize)); // 우하 (3)

            // UV 매핑 (머티리얼 텍스처가 각 칸마다 예쁘게 들어가도록)
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));

            // 삼각형 2개를 그려서 사각형(Quad) 완성
            triangles.Add(vertexIndex + 0);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);

            triangles.Add(vertexIndex + 0);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }

        // 메쉬에 데이터 적용
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals(); // 빛 반사를 위해 노말 계산

        // MeshFilter에 Mesh 적용
        meshFilter.mesh = mesh;
    }
}
