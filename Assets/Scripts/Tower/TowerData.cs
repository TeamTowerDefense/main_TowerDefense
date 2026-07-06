using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum StatType
{
    AttackDamage, 
    AttackSpeed, 
    AttackRange, 
    MaxHealth, 
    MoveSpeed
}

public enum TowerAttackMode
{
    Projectile,
    HitBox
}

[CreateAssetMenu(menuName = "TowerDB/Tower Data")]
public class TowerData : BuildingData
{
    [Header("Combat")]
    public int damage = 10;
    public float attackRange = 5f;
    public float attackInterval = 1f;
    public float attackSpeed = 1f;

    [Header("Target")]
    public LayerMask monsterLayer;

    [Header("Attack Mechanism")]
    public AttackBaseData attackMechanism;

    [Header("Addressable Data")]
    public AssetReferenceGameObject towerPrefab;

    [Header("태생 특성")]
    [Tooltip("이 타워가 지어질 때 기본으로 장착하고 시작하는 키워드들 (없어도 무방)")]
    public List<KeywordData> defaultKeywords = new List<KeywordData>();

    public Dictionary<StatType, float> GetInitialStats()
    {
        return new Dictionary<StatType, float>
        {
            { StatType.AttackDamage, this.damage },
            { StatType.AttackSpeed, this.attackSpeed },
            { StatType.AttackRange, this.attackRange }
        };
    }

    public virtual bool CheckAttackData()
    {
        if (attackMechanism == null)
        {
            Debug.LogError($"{name} : 공격 메커니즘(AttackMechanism) 데이터가 누락되었습니다!");
            return false;
        }

        return true;
    }
}
