using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundDB", menuName = "Sound/SoundDB")]
public class SoundDB : ScriptableObject
{
    public List<SoundData> sounds = new List<SoundData>();
}
