using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelDesignTool))]
public class LevelDesignToolEditor : Editor
{
    private void OnSceneGUI()
    {
        LevelDesignTool tool = (LevelDesignTool)target;
        if (!tool.isPlacementMode) return;

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            // 바닥에 콜라이더(Collider)가 있어야 레이캐스트가 작동합니다.
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                float tileSize = (MonsterManager.Instance != null) ? MonsterManager.Instance.tileSize : 1.0f;
                Vector3 targetPos = hit.point;

                // 1. 가장 가까운 타일 찾기
                Tile nearest = MonsterManager.Instance.GetNearestTile(targetPos);

                Vector3 finalPos;
                if (nearest != null)
                {
                    // 2. 가장 가까운 타일이 있다면, 그 타일의 위치를 기준으로 그리드 계산
                    // 클릭한 위치가 nearest 타일의 어느 방향인지 계산하여 스냅
                    Vector3 offset = targetPos - nearest.transform.position;

                    // 방향 판별 (X축 혹은 Z축 중 더 많이 치우친 쪽으로 한 칸 이동)
                    int moveX = Mathf.Abs(offset.x) > Mathf.Abs(offset.z) ? (offset.x > 0 ? 1 : -1) : 0;
                    int moveZ = Mathf.Abs(offset.z) >= Mathf.Abs(offset.x) ? (offset.z > 0 ? 1 : -1) : 0;

                    finalPos = nearest.transform.position + new Vector3(moveX * tileSize, 0, moveZ * tileSize);
                }
                else
                {
                    float y = (MonsterManager.Instance != null) ? MonsterManager.Instance.spawnY : 0f;
                    finalPos = new Vector3(0, y, 0);
                }

                // 4. 타일 생성 및 좌표 갱신
                GameObject newTileObj = (GameObject)PrefabUtility.InstantiatePrefab(tool.tilePrefab);
                newTileObj.transform.position = finalPos;

                Tile tile = newTileObj.GetComponent<Tile>();
                if (tile != null) tile.UpdateGridInfo();

                Undo.RegisterCreatedObjectUndo(newTileObj, "Place Tile");
            }
            e.Use();
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        LevelDesignTool tool = (LevelDesignTool)target;

        // 버튼 표시
        string label = tool.isPlacementMode ? "배치 모드: ON (클릭으로 배치)" : "배치 모드: OFF";
        if (GUILayout.Button(label, GUILayout.Height(40)))
        {
            tool.isPlacementMode = !tool.isPlacementMode;
            SceneView.RepaintAll(); // 화면 갱신
        }
    }
}