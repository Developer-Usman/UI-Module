using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class TabView : MonoBehaviour
{
    [Header("Visibility")]
    [Tooltip("Manually assign the CanvasGroup used for Show/Hide. Keep the GameObject active.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Events")]
    [SerializeField] private UnityEvent onInit;
    [SerializeField] private UnityEvent onShow;
    [SerializeField] private UnityEvent onHide;

    private bool isInitialized;
    private bool isVisible;

    public bool IsInitialized => isInitialized;
    public bool IsVisible => isVisible;

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
            Debug.LogError($"[{nameof(TabView)}] '{name}' is missing a CanvasGroup reference.", this);
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
            Debug.LogError($"[{nameof(TabView)}] '{name}' is missing a CanvasGroup reference.", this);
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
