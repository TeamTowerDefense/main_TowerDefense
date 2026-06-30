using UnityEngine;

[CreateAssetMenu(fileName = "MonsterDataSO", menuName = "Scriptable Objects/MonsterDataSO")]
public class MonsterDataSO : ScriptableObject
{
    public string Name;
    public GameObject Prefab;
    public int Hp;
    public float Speed;
    public int AttakPower;
    public int LeakDamage;
}
