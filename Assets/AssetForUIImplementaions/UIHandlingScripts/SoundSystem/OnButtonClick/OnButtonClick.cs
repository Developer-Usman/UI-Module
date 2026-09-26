using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Button press feedback only (scale + optional click sound).
/// Does not hook Button.onClick — screen/brains scripts own gameplay actions.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(AudioSource))]
public class OnButtonClick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private AudioSource audioSource;

    [Header("Click Sound")]
    [SerializeField] private bool playSound = true;
    [SerializeField] private AudioClip clickSound;

    [Header("Click Animation")]
    [SerializeField] private bool playAnimation = true;
    [SerializeField] [Range(0.5f, 1f)] private float pressScale = 0.85f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.18f;

    private Transform cachedTransform;
    private Vector3 originalScale;
    private Tween scaleTween;
    private bool isPressed;
    private bool refsReady;

    private void Awake()
    {
        cachedTransform = transform;
        EnsureReferences();
        CacheOriginalScale();
        ResolveButtonTransitionConflict();
    }

    private void OnEnable()
    {
        if (!isPressed)
            CacheOriginalScale();
    }

    private void OnDisable()
    {
        isPressed = false;
        KillTween(true);
    }

    private void OnDestroy()
    {
        KillTween(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractive())
            return;

        isPressed = true;

        if (!playAnimation)
            return;

        KillTween(false);
        scaleTween = cachedTransform
            .DOScale(originalScale * pressScale, pressDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractive())
            return;

        PlayClickSound();
    }

    private void Release()
    {
        if (!isPressed)
            return;

        isPressed = false;

        if (!playAnimation)
        {
            RestoreScale();
            return;
        }

        KillTween(false);
        scaleTween = cachedTransform
            .DOScale(originalScale, releaseDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(RestoreScale);
    }

    private void PlayClickSound()
    {
        if (!playSound || clickSound == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clickSound);
    }

    private bool IsInteractive()
    {
        return refsReady
               && button != null
               && button.IsActive()
               && button.interactable
               && button.enabled;
    }

    private void EnsureReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.playOnAwake = false;

        refsReady = button != null && audioSource != null && cachedTransform != null;
    }

    private void CacheOriginalScale()
    {
        if (cachedTransform == null)
            return;

        originalScale = cachedTransform.localScale;
    }

    /// <summary>
    /// Scale tween fights Button Animation transitions — keep ColorTint/SpriteSwap/None.
    /// </summary>
    private void ResolveButtonTransitionConflict()
    {
        if (!playAnimation || button == null)
            return;

        if (button.transition == Selectable.Transition.Animation)
            button.transition = Selectable.Transition.ColorTint;
    }

    private void KillTween(bool restore)
    {
        if (scaleTween != null && scaleTween.IsActive())
            scaleTween.Kill();

        scaleTween = null;

        if (restore)
            RestoreScale();
    }

    private void RestoreScale()
    {
        if (cachedTransform == null)
            return;

        cachedTransform.localScale = originalScale;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AssignRefs();
    }

    private void OnValidate()
    {
        if (button == null || audioSource == null)
            AssignRefs();
    }

    [ContextMenu("Assign Button & Audio Source")]
    public void AssignRefs()
    {
        button = GetComponent<Button>();
        audioSource = GetComponent<AudioSource>();
    }
#endif
}
