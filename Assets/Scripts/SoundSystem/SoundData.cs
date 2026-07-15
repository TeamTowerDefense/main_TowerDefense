using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "SoundData", menuName = "Sound/SoundData")]
public class SoundData : ScriptableObject
{
    [Header("ID")]
    public int soundID;

    [Header("Addressable Audio Clip")]
    public AssetReferenceT<AudioClip> audioClipReference;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Pitch")]
    public Vector2 pitchRange = new Vector2(1f, 1f);

    [Header("Spatial Sound")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f;

    public float minDistance = 1f;
    public float maxDistance = 30f;

    [Header("Playback")]
    public bool loop;
}
