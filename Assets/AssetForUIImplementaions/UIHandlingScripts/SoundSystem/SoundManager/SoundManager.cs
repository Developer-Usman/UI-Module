using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    private const float DefaultMusicVolume = 0.5f;
    private const float DefaultSfxVolume = 0.5f;

    [Header("References")]
    [SerializeField] private SoundDatabase soundDb;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = DefaultMusicVolume;
    [Range(0f, 1f)] public float sfxVolume = DefaultSfxVolume;

    [SerializeField] private string saveFileName = "SoundSettings.json";

    public static event Action OnSettingsChanged;

    public bool IsMusicOn { get; private set; } = true;
    public bool IsSFXOn { get; private set; } = true;

    public string SaveDirectory => Application.persistentDataPath;
    public string SaveFileName => string.IsNullOrWhiteSpace(saveFileName) ? "SoundSettings.json" : saveFileName.Trim();
    public string SaveFilePath => Path.Combine(SaveDirectory, SaveFileName);

    private Dictionary<string, SoundDatabase.MusicData> musicDict;
    private Dictionary<string, SoundDatabase.SoundData> sfxDict;

    private SoundDatabase.MusicData currentMusic;
    private bool musicPausedForCutscene;
    private float lastNonZeroMusicVolume = DefaultMusicVolume;
    private float lastNonZeroSfxVolume = DefaultSfxVolume;

    [Serializable]
    private class SoundSettingsData
    {
        public float musicVolume = DefaultMusicVolume;
        public float sfxVolume = DefaultSfxVolume;
        public bool isMusicOn = true;
        public bool isSfxOn = true;
    }

    #region INIT

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildDictionaries();
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusic(scene.name);
    }

    #endregion

    #region SETUP

    private void BuildDictionaries()
    {
        musicDict = new Dictionary<string, SoundDatabase.MusicData>();
        sfxDict = new Dictionary<string, SoundDatabase.SoundData>();

        if (soundDb == null)
            return;

        foreach (var m in soundDb.musics)
        {
            if (!musicDict.ContainsKey(m.sceneName))
                musicDict.Add(m.sceneName, m);
        }

        foreach (var s in soundDb.sfx)
        {
            if (!sfxDict.ContainsKey(s.soundName))
                sfxDict.Add(s.soundName, s);
        }
    }

    private void LoadSettings()
    {
        string path = SaveFilePath;
        bool fileExists = File.Exists(path);
        SoundSettingsData data = fileExists ? ReadSettingsFromDisk() : CreateDefaultSettings();

        IsMusicOn = data.isMusicOn;
        IsSFXOn = data.isSfxOn;
        musicVolume = Mathf.Clamp01(data.musicVolume);
        sfxVolume = Mathf.Clamp01(data.sfxVolume);

        if (musicVolume > 0f)
            lastNonZeroMusicVolume = musicVolume;

        if (sfxVolume > 0f)
            lastNonZeroSfxVolume = sfxVolume;

        ApplySettings();

        if (!fileExists)
            SaveSettings();
    }

    private SoundSettingsData ReadSettingsFromDisk()
    {
        string path = SaveFilePath;

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return CreateDefaultSettings();

            SoundSettingsData data = JsonUtility.FromJson<SoundSettingsData>(json);
            return data ?? CreateDefaultSettings();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{nameof(SoundManager)}] Failed to read '{path}'. Using defaults. {e.Message}", this);
            return CreateDefaultSettings();
        }
    }

    private static SoundSettingsData CreateDefaultSettings()
    {
        return new SoundSettingsData
        {
            musicVolume = DefaultMusicVolume,
            sfxVolume = DefaultSfxVolume,
            isMusicOn = true,
            isSfxOn = true
        };
    }

    private void SaveSettings()
    {
        SoundSettingsData data = new SoundSettingsData
        {
            musicVolume = musicVolume,
            sfxVolume = sfxVolume,
            isMusicOn = IsMusicOn,
            isSfxOn = IsSFXOn
        };

        try
        {
            string path = SaveFilePath;
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"[{nameof(SoundManager)}] Failed to save '{SaveFilePath}'. {e.Message}", this);
        }
    }

    private void ApplySettings()
    {
        if (musicSource != null)
            musicSource.mute = !IsMusicOn;

        if (sfxSource != null)
            sfxSource.mute = !IsSFXOn;

        ApplyMusicPlaybackVolume();
    }

    private void ApplyMusicPlaybackVolume()
    {
        if (currentMusic != null && musicSource != null)
            musicSource.volume = currentMusic.volume * musicVolume;
    }

    private void NotifySettingsChanged()
    {
        OnSettingsChanged?.Invoke();
    }

    private void ResetToDefaults()
    {
        IsMusicOn = true;
        IsSFXOn = true;
        musicVolume = DefaultMusicVolume;
        sfxVolume = DefaultSfxVolume;
        lastNonZeroMusicVolume = DefaultMusicVolume;
        lastNonZeroSfxVolume = DefaultSfxVolume;

        ApplySettings();
        NotifySettingsChanged();
    }

    /// <summary>
    /// Deletes the JSON save file and resets runtime values to defaults.
    /// </summary>
    [ContextMenu("Clear Saved Settings")]
    public void ClearSavedSettings()
    {
        string path = SaveFilePath;
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SoundManager)}] Failed to delete '{path}'. {e.Message}", this);
            }
        }

        ResetToDefaults();
        SaveSettings();
    }

    /// <summary>
    /// Last non-zero music volume (for UI restore when toggle turns on at 0).
    /// </summary>
    public float GetRestoreMusicVolume()
    {
        return lastNonZeroMusicVolume > 0f ? lastNonZeroMusicVolume : DefaultMusicVolume;
    }

    /// <summary>
    /// Last non-zero SFX volume (for UI restore when toggle turns on at 0).
    /// </summary>
    public float GetRestoreSfxVolume()
    {
        return lastNonZeroSfxVolume > 0f ? lastNonZeroSfxVolume : DefaultSfxVolume;
    }

    #endregion

    #region SETTINGS

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicVolume > 0f)
            lastNonZeroMusicVolume = musicVolume;

        ApplyMusicPlaybackVolume();
        SaveSettings();
        NotifySettingsChanged();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxVolume > 0f)
            lastNonZeroSfxVolume = sfxVolume;

        SaveSettings();
        NotifySettingsChanged();
    }

    public void ToggleMusic(bool value)
    {
        IsMusicOn = value;

        if (musicSource != null)
            musicSource.mute = !value;

        SaveSettings();
        NotifySettingsChanged();
    }

    public void ToggleSFX(bool value)
    {
        IsSFXOn = value;

        if (sfxSource != null)
            sfxSource.mute = !value;

        SaveSettings();
        NotifySettingsChanged();
    }

    #endregion

    #region PLAYBACK

    public void PauseMusicForCutscene()
    {
        musicPausedForCutscene = true;
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ResumeMusicAfterCutscene()
    {
        if (!musicPausedForCutscene)
            return;

        musicPausedForCutscene = false;
        if (musicSource == null || !IsMusicOn || musicSource.clip == null)
            return;

        musicSource.UnPause();
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void PlayMusic(string sceneName)
    {
        if (musicDict == null || musicSource == null)
            return;

        if (!musicDict.TryGetValue(sceneName, out var music) || music.clip == null)
        {
            if (musicSource.isPlaying) musicSource.Stop();
            currentMusic = null;
            return;
        }

        if (musicSource.clip == music.clip && musicSource.isPlaying)
            return;

        if (musicPausedForCutscene && musicSource.clip == music.clip)
            return;

        currentMusic = music;

        musicSource.Stop();
        musicSource.clip = music.clip;
        musicSource.volume = music.volume * musicVolume;

        if (IsMusicOn)
            musicSource.Play();

        if (musicPausedForCutscene)
            musicSource.Pause();
    }

    public void PlaySFX(string sfxName)
    {
        if (sfxDict == null || sfxSource == null)
            return;

        if (!sfxDict.TryGetValue(sfxName, out var sfx) || sfx.clip == null)
            return;

        sfxSource.PlayOneShot(sfx.clip, sfx.volume * sfxVolume);
    }

    #endregion
}
