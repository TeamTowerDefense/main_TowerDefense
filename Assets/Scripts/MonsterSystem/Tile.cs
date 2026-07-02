using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Tile : MonoBehaviour
{
    // 타일의 고유 그리드 좌표 (예: 0,0 / 0,1 / 1,1 ...)
    public Vector2Int gridPos;

    // 현재 이 타일 위에 올라와 있는 몬스터들을 담는 바구니
    // (몬스터 담당인 내가 성능 최적화를 위해 쓸 바구니)
    private List<Monster> _monstersOnTile = new List<Monster>();
    public IReadOnlyList<Monster> Monsters => _monstersOnTile;

    // 주변 인접 타일들의 정보 
    public List<Tile> neighbors = new List<Tile>();

    private void Start()
    {
        // 타일이 생성되자마자 매니저에게 '나 여기 있어!'라고 등록
        MonsterManager.Instance.RegisterTile(this);
    }
    public void AddMonster(Monster m) { if (!_monstersOnTile.Contains(m)) _monstersOnTile.Add(m); }
    public void RemoveMonster(Monster m) { _monstersOnTile.Remove(m); }
    private void OnDrawGizmos()
    {
        // 1. 타일 영역 표시
        Gizmos.color = Color.cyan;
        float tileSize = MonsterManager.Instance != null ? MonsterManager.Instance.tileSize : 1.0f;
        Gizmos.DrawWireCube(transform.position, new Vector3(tileSize, 0.1f, tileSize));

        // 2. 경로 폭(pathWidth) 표시 (MonsterManager에서 가져오기)
        Gizmos.color = Color.yellow;
        float pathWidth = MonsterManager.Instance != null ? MonsterManager.Instance.pathWidth : 1.5f;
        Gizmos.DrawWireCube(transform.position, new Vector3(pathWidth, 0.05f, pathWidth));

        // 3. [추가] 이웃 타일로 선 그리기 (연결 확인용)
        Gizmos.color = Color.green;
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null)
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }
}