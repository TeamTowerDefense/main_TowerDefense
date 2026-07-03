using UnityEngine;
using UnityEngine.AddressableAssets;

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
