using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

// 즉발성, 지속형 틱데미지
public enum HitBoxDamageMode
{
    OncePerTarget,
    TickDamage
}

public class HitBoxData : AttackBaseData
{
    [Header("Addressable")]
    public int hitBoxID;
    public int hitEffectID;

    public float activeTime = 0.3f;
    public float damageInterval = 0.2f;
    public EffectData hitEffectData;

    [Header("Shape")]
    public Vector3 center = new Vector3(0f, 0f, 3f);

    [Header("Damage Type")]
    public HitBoxDamageMode damageMode = HitBoxDamageMode.OncePerTarget;
    [Header("Effect Option")]
    public bool stopEffectOutOfRange = true;

    [Header("HitBox Time(TickDamage)")]
    public float colliderActiveTime = 0.2f;

    [Header("TickDamage EffectTime")]
    public float effectDespawnDelay = 1f;

    [Header("Synchronized Attack(Howitzer Only)")]
    public bool isMortar;
    public float impactDelay = 1.2f;
    public float hitColliderActiveTime = 0.2f;
    public int warningEffectID;
    public int launchEffectID;

    [Header("Sound")]
    public int attackSoundID;
    public int hitSoundID;

    [Header("Loop Sound")]
    public int loopSoundID;

    [Header("Asset GameObject")]
    public AssetReferenceGameObject hitboxPF;

    [HideInInspector] public GameObject loadedPrefab;
    public float hitEffectYOffset = 0.2f;
}
