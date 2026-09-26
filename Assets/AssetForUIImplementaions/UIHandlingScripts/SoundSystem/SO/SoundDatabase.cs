using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSoundDatabase", menuName = "Audio/Sound Database")]
public class SoundDatabase : ScriptableObject
{
    [Header("BG Music's")]
    public List<MusicData> musics;

    [Header("Sound Effects")]
    public List<SoundData> sfx;

    [System.Serializable]
    public class MusicData
    {
        public string sceneName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [System.Serializable]
    public class SoundData
    {
        public string soundName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }
}