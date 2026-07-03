using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum HitBoxDamageMode
{
    OncePerTarget,
    TickDamage
}

[CreateAssetMenu(menuName = "Attack/HitBox")]
public class HitBoxData : ScriptableObject
{
    [Header("Addressable")]
    public string addressKey;
    public string label;
    public int hitBoxID;
    public int hitEffectID;

    public float activeTime = 0.3f;
    public float damageInterval = 0.2f;
    public EffectData hitEffectData;

    [Header("Shape")]
    public Vector3 center = new Vector3(0f, 0f, 3f);

    [Header("Box")]
    public Vector3 boxSize = new Vector3(1.5f, 1.5f, 6f);

    [Header("Sphere")]
    public float sphereRadius = 1f;

    [Header("Capsule")]
    public float capsuleRadius = 1f;
    public float capsuleHeight = 1f;
    // 0 = X, 1 = Y, 2 = Z
    public int capsuleDirection = 0;

    [Header("Damage Type")]
    public HitBoxDamageMode damageMode = HitBoxDamageMode.OncePerTarget;
    [Header("Effect Option")]
    public bool stopEffectOutOfRange = true;

    [Header("Hit Timing")]
    public float colliderActiveTime = 0.2f;

    [Header("Debuff")]
    public bool applyDebuff;
    public List<DebuffEffectData> debuffs;

    [Header("Debuff Tick")]
    public float debuffApplyInterval = 0.5f;

    public AssetReferenceGameObject hitboxPF;

    [HideInInspector] public GameObject loadedPrefab;
}
