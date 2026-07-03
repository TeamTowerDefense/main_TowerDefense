using UnityEngine;

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

    [Header("Attack Mode")]
    public TowerAttackMode attackMode;

    [Header("ProjectTile")]
    public ProjectileData projectileData;

    [Header("HitBox Attack")]
    public HitBoxData hitBoxAttackData;

    public virtual bool CheckAttackData()
    {
        switch (attackMode)
        {
            case TowerAttackMode.Projectile:

                if (projectileData == null)
                {
                    Debug.LogError($"{name} : ProjectileData 없음");
                    return false;
                }
                return true;

            case TowerAttackMode.HitBox:

                if (hitBoxAttackData == null)
                {
                    Debug.LogError($"{name} : HitBoxAttackData 없음");
                    return false;
                }
                return true;
            default:
                return false;
        }
    }
}
