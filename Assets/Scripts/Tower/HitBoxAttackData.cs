using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Attack/HitBox")]
public class HitBoxAttackData : ScriptableObject
{
    [Header("HitBox")]
    public GameObject hitBoxPrefab;
    public float activeTime = 0.3f;
    public float damageInterval = 0.2f;

    [Header("Shape")]
    public Vector3 boxSize = new Vector3(1.5f, 1.5f, 6f);
    public Vector3 boxCenter = new Vector3(0f, 0f, 3f);

    [Header("Debuff")]
    public bool applyDebuff;
    public List<DebuffEffectData> debuffs;

    [Header("Debuff Tick")]
    public float debuffApplyInterval = 0.5f;

    [Header("Effect")]
    public GameObject effectPrefab;
}
