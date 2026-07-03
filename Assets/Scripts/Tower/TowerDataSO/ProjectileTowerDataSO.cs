using System.Xml.Serialization;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDB/ProjectileTowerDataSO")]
public class ProjectileTowerDataSO : TowerDataSO
{
    [Header("ProjectTile")]
    public ProjectileData projectileData;

    public override bool CheckAttackData()
    {
        if (projectileData == null)
        {
            Debug.LogError($"{name} : ProjectileData ¾øÀ½");
            return false;
        }
        return true;
    }
}
