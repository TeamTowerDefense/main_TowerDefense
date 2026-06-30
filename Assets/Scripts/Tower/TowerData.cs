using UnityEngine;

public enum TowerType
{
    Basic,
    Cannon,
    Laser,
    Support,
    Slow,
    HitBox
}

public enum TowerAttackMode
{
    Projectile,
    HitBox
}

[CreateAssetMenu(menuName = "Tower/Data")]
public class TowerData : BuildingData
{
    [Header("Combat")]
    public int damage = 10;
    public float attackRange = 5f;
    public float attackInterval = 1f;

    [Header("Tower Type")]
    public TowerType type = TowerType.Basic;

    [Header("Target")]
    public LayerMask monsterLayer;

    [Header("Attack Mode")]
    public TowerAttackMode attackMode;

    [Header("ProjectTile")]
    public ProjectileData projectileData;

    [Header("HitBox Attack")]
    public HitBoxAttackData hitBoxAttackData;

    public GameObject bullet;
}
