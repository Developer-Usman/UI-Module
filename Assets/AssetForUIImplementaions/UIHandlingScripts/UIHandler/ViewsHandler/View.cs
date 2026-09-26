using UnityEngine;
using UnityEngine.Events;

public enum ViewType
{
    Screen,
    Overlay
}

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class View : MonoBehaviour
{
    [Header("Header")]
    [Tooltip("When this Screen is active, show the Header View. Ignored for Overlays.")]
    [SerializeField] private bool showHeader = true;

    [Header("Identity")]
    [Tooltip("Unique ID used by ViewHandler.Show(string).")]
    [SerializeField] private string viewID;

    [Tooltip("Screen replaces the current screen. Overlay stacks on top without hiding it.")]
    [SerializeField] private ViewType viewType = ViewType.Screen;

    [Header("Visibility")]
    [Tooltip("Manually assign the CanvasGroup used for Show/Hide. Keep the GameObject active.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Events")]
    [SerializeField] private UnityEvent onInit;
    [SerializeField] private UnityEvent onShow;
    [SerializeField] private UnityEvent onHide;

    private bool isInitialized;
    private bool isVisible;

    public string ViewID => viewID;
    public ViewType ViewType => viewType;
    public bool ShowHeader => showHeader;
    public bool IsInitialized => isInitialized;
    public bool IsVisible => isVisible;

    /// <summary>Subscribe from brain scripts. Prefer code listeners over Inspector wiring.</summary>
    public UnityEvent OnInitEvent => onInit;

    /// <summary>Subscribe from brain scripts. Prefer code listeners over Inspector wiring.</summary>
    public UnityEvent OnShowEvent => onShow;

    /// <summary>Subscribe from brain scripts. Prefer code listeners over Inspector wiring.</summary>
    public UnityEvent OnHideEvent => onHide;

    private void Reset()
    {
#if UNITY_EDITOR
        canvasGroup = GetComponent<CanvasGroup>();
        if (string.IsNullOrEmpty(viewID))
            viewID = name;
#endif
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
#endif
    }

    [ContextMenu("Assign View ID")]
    public void AssignViewIDFromName()
    {
        viewID = name;
    }

    [ContextMenu("Assign Canvas Group")]
    public void AssignCanvasGroup()
    {
#if UNITY_EDITOR
        canvasGroup = GetComponent<CanvasGroup>();
#endif
    }

    public void Show()
    {
        if (isVisible)
            return;

        if (canvasGroup == null)
        {
            Debug.LogError($"[{nameof(View)}] '{name}' is missing a CanvasGroup reference.", this);
            return;
        }

        if (!isInitialized)
        {
            isInitialized = true;
            if (onInit != null)
                onInit.Invoke();
        }

        ApplyVisibleState();
        isVisible = true;

        if (onShow != null)
            onShow.Invoke();
    }

    public void Hide()
    {
        if (!isVisible)
            return;

        if (canvasGroup == null)
        {
            Debug.LogError($"[{nameof(View)}] '{name}' is missing a CanvasGroup reference.", this);
            return;
        }

        ApplyHiddenState();
        isVisible = false;

        if (onHide != null)
            onHide.Invoke();
    }

    internal void ApplyHiddenWithoutEvents()
    {
        if (canvasGroup == null)
            return;

        ApplyHiddenState();
        isVisible = false;
    }

    internal void ApplyVisibleWithoutEvents()
    {
        if (canvasGroup == null)
            return;

        ApplyVisibleState();
        isVisible = true;
    }

    internal bool HasValidCanvasGroup()
    {
        return canvasGroup != null;
    }

    [ContextMenu("Toggle View State")]
    public void ToggleViewState()
    {
        if (canvasGroup == null)
            return;

        if (canvasGroup.alpha > 0f)
            ApplyHiddenWithoutEvents();
        else
            ApplyVisibleWithoutEvents();
    }

    private void ApplyVisibleState()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void ApplyHiddenState()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
