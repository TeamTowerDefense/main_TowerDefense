using UnityEngine;

public abstract class AttackBaseData : ScriptableObject
{
    public virtual bool IsValidData(string towerName)
    {
        return true;
    }
}
