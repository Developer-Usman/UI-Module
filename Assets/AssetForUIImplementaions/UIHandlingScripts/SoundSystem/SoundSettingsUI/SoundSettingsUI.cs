using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class SoundSettingsUI : MonoBehaviour
{
    private const float VolumeEpsilon = 0.001f;

    [Header("Features")]
    [SerializeField] private bool useMusicToggle = true;
    [SerializeField] private bool useMusicSlider = true;
    [SerializeField] private bool useSfxToggle = true;
    [SerializeField] private bool useSfxSlider = true;
    [SerializeField] private bool showPercentageText = true;

    [Header("Link")]
    [Tooltip("When toggle and slider are both used, keep them in sync.")]
    [SerializeField] private bool linkToggleAndSlider = true;
    [SerializeField] private float restoreOnToggleOn = 0.5f;
    [SerializeField] private float lerpDuration = 0.2f;

    [Header("UI Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip toggleClip;
    [SerializeField] private AudioClip sliderReleaseClip;

    [Header("Music")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TextMeshProUGUI musicPercentText;

    [Header("SFX")]
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxPercentText;

    private bool suppressCallbacks;
    private Coroutine musicLerpRoutine;
    private Coroutine sfxLerpRoutine;
    private SliderPointerUpListener musicSliderReleaseListener;
    private SliderPointerUpListener sfxSliderReleaseListener;

    [ContextMenu("Assign UI Audio Source")]
    public void AssignUiAudioSource()
    {
#if UNITY_EDITOR
        uiAudioSource = GetComponent<AudioSource>();
#endif
    }

    private void OnEnable()
    {
        SoundManager.OnSettingsChanged += RefreshFromManager;
        RegisterListeners();
        RefreshFromManager();
    }

    private void OnDisable()
    {
        SoundManager.OnSettingsChanged -= RefreshFromManager;
        UnregisterListeners();
        StopLerpRoutines();
    }

    #region Bind

    private void RegisterListeners()
    {
        if (useMusicToggle && musicToggle != null)
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);

        if (useMusicSlider && musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            musicSliderReleaseListener = EnsureSliderReleaseListener(musicSlider, OnMusicSliderReleased);
        }

        if (useSfxToggle && sfxToggle != null)
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);

        if (useSfxSlider && sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            sfxSliderReleaseListener = EnsureSliderReleaseListener(sfxSlider, OnSfxSliderReleased);
        }
    }

    private void UnregisterListeners()
    {
        if (musicToggle != null)
            musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);

        if (sfxToggle != null)
            sfxToggle.onValueChanged.RemoveListener(OnSfxToggleChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);

        ClearSliderReleaseListener(ref musicSliderReleaseListener);
        ClearSliderReleaseListener(ref sfxSliderReleaseListener);
    }

    private static SliderPointerUpListener EnsureSliderReleaseListener(Slider slider, System.Action onRelease)
    {
        SliderPointerUpListener listener = slider.GetComponent<SliderPointerUpListener>();
        if (listener == null)
            listener = slider.gameObject.AddComponent<SliderPointerUpListener>();

        listener.onPointerUp = onRelease;
        return listener;
    }

    private static void ClearSliderReleaseListener(ref SliderPointerUpListener listener)
    {
        if (listener == null)
            return;

        listener.onPointerUp = null;
        listener = null;
    }

    #endregion

    #region UI Audio

    private void PlayToggleClip()
    {
        PlayUiClip(toggleClip);
    }

    private void PlaySliderReleaseClip()
    {
        PlayUiClip(sliderReleaseClip);
    }

    private void PlayUiClip(AudioClip clip)
    {
        if (uiAudioSource == null || clip == null)
            return;

        uiAudioSource.PlayOneShot(clip);
    }

    #endregion

    #region Refresh

    private void RefreshFromManager()
    {
        if (SoundManager.Instance == null)
            return;

        if (musicLerpRoutine != null || sfxLerpRoutine != null)
            return;

        suppressCallbacks = true;

        SoundManager manager = SoundManager.Instance;

        if (useMusicToggle && musicToggle != null)
            musicToggle.SetIsOnWithoutNotify(manager.IsMusicOn);

        if (useMusicSlider && musicSlider != null)
        {
            float displayVolume = GetLinkedDisplayVolume(useMusicToggle, useMusicSlider, manager.IsMusicOn, manager.musicVolume);
            musicSlider.SetValueWithoutNotify(displayVolume);
            if (showPercentageText)
                UpdatePercentText(musicPercentText, displayVolume);
        }

        if (useSfxToggle && sfxToggle != null)
            sfxToggle.SetIsOnWithoutNotify(manager.IsSFXOn);

        if (useSfxSlider && sfxSlider != null)
        {
            float displayVolume = GetLinkedDisplayVolume(useSfxToggle, useSfxSlider, manager.IsSFXOn, manager.sfxVolume);
            sfxSlider.SetValueWithoutNotify(displayVolume);
            if (showPercentageText)
                UpdatePercentText(sfxPercentText, displayVolume);
        }

        suppressCallbacks = false;
    }

    private float GetLinkedDisplayVolume(bool useToggle, bool useSlider, bool isOn, float volume)
    {
        if (linkToggleAndSlider && useToggle && useSlider && !isOn)
            return 0f;

        return volume;
    }

    private static void UpdatePercentText(TextMeshProUGUI text, float volume01)
    {
        if (text == null)
            return;

        int percent = Mathf.RoundToInt(Mathf.Clamp01(volume01) * 100f);
        text.text = percent + "%";
    }

    #endregion

    #region Music Handlers

    private void OnMusicToggleChanged(bool isOn)
    {
        if (suppressCallbacks || SoundManager.Instance == null)
            return;

        PlayToggleClip();

        if (linkToggleAndSlider && useMusicSlider && musicSlider != null)
        {
            if (!isOn)
            {
                StartMusicLerp(0f, false);
                return;
            }

            float current = musicSlider.value;
            if (current <= VolumeEpsilon)
            {
                float restore = GetMusicRestoreVolume();
                StartMusicLerp(restore, true);
                return;
            }

            SoundManager.Instance.ToggleMusic(true);
            return;
        }

        SoundManager.Instance.ToggleMusic(isOn);
    }

    private void OnMusicSliderChanged(float value)
    {
        if (suppressCallbacks || SoundManager.Instance == null)
            return;

        value = Mathf.Clamp01(value);
        if (showPercentageText)
            UpdatePercentText(musicPercentText, value);

        if (linkToggleAndSlider && useMusicToggle)
        {
            bool shouldBeOn = value > VolumeEpsilon;
            SoundManager.Instance.SetMusicVolume(value);
            SoundManager.Instance.ToggleMusic(shouldBeOn);

            if (musicToggle != null)
            {
                suppressCallbacks = true;
                musicToggle.SetIsOnWithoutNotify(shouldBeOn);
                suppressCallbacks = false;
            }

            return;
        }

        SoundManager.Instance.SetMusicVolume(value);
    }

    private void OnMusicSliderReleased()
    {
        if (suppressCallbacks || musicLerpRoutine != null)
            return;

        PlaySliderReleaseClip();
    }

    private float GetMusicRestoreVolume()
    {
        if (SoundManager.Instance != null)
            return SoundManager.Instance.GetRestoreMusicVolume();

        return Mathf.Clamp01(restoreOnToggleOn);
    }

    private void StartMusicLerp(float target, bool turnOn)
    {
        if (musicLerpRoutine != null)
            StopCoroutine(musicLerpRoutine);

        musicLerpRoutine = StartCoroutine(LerpMusicSlider(target, turnOn));
    }

    private IEnumerator LerpMusicSlider(float target, bool turnOn)
    {
        if (musicSlider == null)
        {
            musicLerpRoutine = null;
            yield break;
        }

        float start = musicSlider.value;
        float duration = Mathf.Max(0f, lerpDuration);
        float elapsed = 0f;

        suppressCallbacks = true;

        if (duration <= 0f)
        {
            musicSlider.SetValueWithoutNotify(target);
            if (showPercentageText)
                UpdatePercentText(musicPercentText, target);
        }
        else
        {
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float value = Mathf.Lerp(start, target, t);
                musicSlider.SetValueWithoutNotify(value);
                if (showPercentageText)
                    UpdatePercentText(musicPercentText, value);
                yield return null;
            }

            musicSlider.SetValueWithoutNotify(target);
            if (showPercentageText)
                UpdatePercentText(musicPercentText, target);
        }

        suppressCallbacks = false;
        musicLerpRoutine = null;

        if (SoundManager.Instance == null)
            yield break;

        SoundManager.Instance.SetMusicVolume(target);
        SoundManager.Instance.ToggleMusic(turnOn);

        if (musicToggle != null)
        {
            suppressCallbacks = true;
            musicToggle.SetIsOnWithoutNotify(turnOn);
            suppressCallbacks = false;
        }
    }

    #endregion

    #region SFX Handlers

    private void OnSfxToggleChanged(bool isOn)
    {
        if (suppressCallbacks || SoundManager.Instance == null)
            return;

        PlayToggleClip();

        if (linkToggleAndSlider && useSfxSlider && sfxSlider != null)
        {
            if (!isOn)
            {
                StartSfxLerp(0f, false);
                return;
            }

            float current = sfxSlider.value;
            if (current <= VolumeEpsilon)
            {
                float restore = GetSfxRestoreVolume();
                StartSfxLerp(restore, true);
                return;
            }

            SoundManager.Instance.ToggleSFX(true);
            return;
        }

        SoundManager.Instance.ToggleSFX(isOn);
    }

    private void OnSfxSliderChanged(float value)
    {
        if (suppressCallbacks || SoundManager.Instance == null)
            return;

        value = Mathf.Clamp01(value);
        if (showPercentageText)
            UpdatePercentText(sfxPercentText, value);

        if (linkToggleAndSlider && useSfxToggle)
        {
            bool shouldBeOn = value > VolumeEpsilon;
            SoundManager.Instance.SetSFXVolume(value);
            SoundManager.Instance.ToggleSFX(shouldBeOn);

            if (sfxToggle != null)
            {
                suppressCallbacks = true;
                sfxToggle.SetIsOnWithoutNotify(shouldBeOn);
                suppressCallbacks = false;
            }

            return;
        }

        SoundManager.Instance.SetSFXVolume(value);
    }

    private void OnSfxSliderReleased()
    {
        if (suppressCallbacks || sfxLerpRoutine != null)
            return;

        PlaySliderReleaseClip();
    }

    private float GetSfxRestoreVolume()
    {
        if (SoundManager.Instance != null)
            return SoundManager.Instance.GetRestoreSfxVolume();

        return Mathf.Clamp01(restoreOnToggleOn);
    }

    private void StartSfxLerp(float target, bool turnOn)
    {
        if (sfxLerpRoutine != null)
            StopCoroutine(sfxLerpRoutine);

        sfxLerpRoutine = StartCoroutine(LerpSfxSlider(target, turnOn));
    }

    private IEnumerator LerpSfxSlider(float target, bool turnOn)
    {
        if (sfxSlider == null)
        {
            sfxLerpRoutine = null;
            yield break;
        }

        float start = sfxSlider.value;
        float duration = Mathf.Max(0f, lerpDuration);
        float elapsed = 0f;

        suppressCallbacks = true;

        if (duration <= 0f)
        {
            sfxSlider.SetValueWithoutNotify(target);
            if (showPercentageText)
                UpdatePercentText(sfxPercentText, target);
        }
        else
        {
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float value = Mathf.Lerp(start, target, t);
                sfxSlider.SetValueWithoutNotify(value);
                if (showPercentageText)
                    UpdatePercentText(sfxPercentText, value);
                yield return null;
            }

            sfxSlider.SetValueWithoutNotify(target);
            if (showPercentageText)
                UpdatePercentText(sfxPercentText, target);
        }

        suppressCallbacks = false;
        sfxLerpRoutine = null;

        if (SoundManager.Instance == null)
            yield break;

        SoundManager.Instance.SetSFXVolume(target);
        SoundManager.Instance.ToggleSFX(turnOn);

        if (sfxToggle != null)
        {
            suppressCallbacks = true;
            sfxToggle.SetIsOnWithoutNotify(turnOn);
            suppressCallbacks = false;
        }
    }

    #endregion

    private void StopLerpRoutines()
    {
        if (musicLerpRoutine != null)
        {
            StopCoroutine(musicLerpRoutine);
            musicLerpRoutine = null;
        }

        if (sfxLerpRoutine != null)
        {
            StopCoroutine(sfxLerpRoutine);
            sfxLerpRoutine = null;
        }

        suppressCallbacks = false;
    }

    /// <summary>
    /// Internal helper so slider release (pointer up) can fire without extra public types.
    /// </summary>
    private sealed class SliderPointerUpListener : MonoBehaviour, IPointerUpHandler
    {
        public System.Action onPointerUp;

        public void OnPointerUp(PointerEventData eventData)
        {
            onPointerUp?.Invoke();
        }
    }
}
