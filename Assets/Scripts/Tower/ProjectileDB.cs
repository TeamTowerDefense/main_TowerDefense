using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDB/Projectile Database")]
public class ProjectileDB : ScriptableObject
{
    public List<ProjectileData> projectiles;
}
