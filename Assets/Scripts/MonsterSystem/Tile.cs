using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Tile : MonoBehaviour
{
    // 타일의 고유 그리드 좌표 (예: 0,0 / 0,1 / 1,1 ...)
    public Vector2Int gridPos;

    // 현재 이 타일 위에 올라와 있는 몬스터들을 담는 바구니
    // (몬스터 담당인 내가 성능 최적화를 위해 쓸 바구니)
    public List<Monster> monstersOnTile = new List<Monster>();

    // 주변 인접 타일들의 정보 (기획/맵 담당이 세팅해주거나 매니저가 채워줌)
    public List<Tile> neighbors = new List<Tile>();
    private void OnDrawGizmos()
    {
        // 1. 타일 전체 크기 (청록색 박스)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1.0f, 0.1f, 1.0f));

        // 2. 만약 MonsterManager에서 설정한 pathWidth가 있다면, 
        // 몬스터가 실제로 다닐 수 있는 '안전 구역'도 같이 그려줍니다.
        // (색상을 다르게 하여 밖으로 나가는지 확인하기 위함)
        Gizmos.color = Color.yellow;
        // pathWidth는 매니저에 있지만, 여기선 임시로 1.5로 가정하거나 
        // MonsterManager.instance.pathWidth 등으로 접근 가능합니다.
        Gizmos.DrawWireCube(transform.position, new Vector3(1.5f, 0.05f, 1.5f));
    }
}