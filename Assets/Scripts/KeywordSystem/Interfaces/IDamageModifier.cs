using UnityEngine;

public interface IDamageModifier
{
    int ModifyDamage(int currentDamage, Transform target);
}
