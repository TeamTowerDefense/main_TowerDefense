using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Monster", menuName = "Monster/Monster Data")]
public class MonsterData : ScriptableObject
{
    public string monsterName = "New Monster";
    public GameObject Prefab;
    public float maxHP = 100f;
    [Min(0f)]
    public float speed = 1.0f;
    public float defense = 0f;// 방어력 (데미지 감소량)
    public float Att = 0f;   // 공격력 (현제는 회복량으로 상용)
    public int LeakDamage = 1; // 기지 데미지
    [Min(0f)]
    public Vector3 scale = Vector3.one;
    public bool useUniformScale = true;
    [Header("스턴 세팅")]
    public float StunGauge = 10f; // 해당 값 까지 스턴 스택이 쌓이면 스턴이 걸리는 구조

    [Header("힘 배율")]
    public float moveWeight = 1.0f;
    public float separationWeight = 0.3f;
    public float boundaryWeight = 3.0f;
    public float containmentMultiplier = 5f;

    [Header("능력")]
    public List<AbilityData> abilities;

    [Header("Height Offset")]
    public float heightOffset = 0f;

    private void OnValidate()
    {
        // useUniformScale이 켜져 있을 때만 비율을 강제로 맞춤
        if (useUniformScale)
        {
            if (scale.y != scale.x || scale.z != scale.x)
            {
                scale.y = scale.x;
                scale.z = scale.x;
            }
        }
    } 
    
    [Header("태생 특성")]
    [Tooltip("이 몬스터가 소환될 때 기본으로 장착하고 시작하는 키워드 (예: 언데드, 보스 면역 등)")]
    public List<KeywordData> defaultKeywords = new List<KeywordData>();

    public Dictionary<StatType, float> GetInitialStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.MaxHealth, this.maxHP },
            { StatType.MoveSpeed, this.speed }
            // 방어력 등 다른 스탯이 있다면 여기에 계속 추가하면 됩니다.
        };
    }
}