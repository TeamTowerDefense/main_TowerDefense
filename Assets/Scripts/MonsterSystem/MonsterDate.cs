using UnityEngine;

[CreateAssetMenu(fileName = "New Monster Data", menuName = "Game/Monster Data")]
public class MonsterData : ScriptableObject
{
    public string monsterName;
    public GameObject Prefab;
    public float maxHP = 100f;
    public float speed = 1.0f;
    public float defense = 0f;// 방어력 (데미지 감소량)
    public float Att = 0f;   // 공격력 (현제는 회복량으로 상용)
    public int LeakDamage = 1; // 기지 데미지

    [Header("Stun Settings")]
    public float StunGauge = 10f; // 해당 값 까지 스턴 스택이 쌓이면 스턴이 걸리는 구조
    public float Stunstack = 0f;// 스톤 될떄 가지고 있는 기본 스턴 스택
}