using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDB/Hitbox Database")]
public class HitBoxDB : ScriptableObject
{
    public List<HitBoxData> hitBoxes;
}
