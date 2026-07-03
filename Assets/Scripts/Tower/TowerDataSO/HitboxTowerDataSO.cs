using UnityEngine;

[CreateAssetMenu(fileName = "HitboxTowerDataSO", menuName = "Scriptable Objects/HitboxTowerDataSO")]
public class HitboxTowerDataSO : TowerDataSO
{
    [Header("HitBox Attack")]
    public HitBoxData hitBoxAttackData;

    public override bool CheckAttackData()
    {
        if (hitBoxAttackData == null)
        {
            Debug.LogError($"{name} : HitBoxAttackData ¾øÀ½");
            return false;
        }
        return true;
    }
}
