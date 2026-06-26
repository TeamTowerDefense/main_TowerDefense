using UnityEngine;

public enum TowerType
{
    Basic,
    Cannon,
    Laser,
    Support,
    Slow
}

[CreateAssetMenu(menuName = "Tower/Data")]
public class TowerData : ScriptableObject
{
    [Header("Basic")]
    public int towerID = 0;
    public string towerName = "TowerName";
    public int cost = 10;

    [Header("Combat")]
    public int damage = 10;
    public float attackRange = 5f;
    public float attackInterval = 1f;

    [Header("Target")]
    public LayerMask monsterLayer;

    [Header("ProjectTile")]
    public ProjectileData projectileData;

    public GameObject bullet;
    public GameObject towerPF;
}
