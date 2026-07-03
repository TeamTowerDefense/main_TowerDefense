using UnityEngine;


public abstract class TowerDataSO : BuildingData
{
    [Header("Combat")]
    public int damage = 10;
    public float attackRange = 5f;
    public float attackInterval = 1f;
    public float attackSpeed = 1f;

    [Header("Target")]
    public LayerMask monsterLayer;

    public abstract bool CheckAttackData();
}
