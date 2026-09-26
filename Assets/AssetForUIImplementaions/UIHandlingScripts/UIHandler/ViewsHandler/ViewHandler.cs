using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ViewHandler : MonoBehaviour
{
    [Header("MainViews")]
    [SerializeField] private View startingView;
    [SerializeField] private View headerView;

    [Header("Registered Views")]
    [Tooltip("All navigable Views. Assigned manually, or via Assign Views in the Editor.")]
    [SerializeField] private View[] views = new View[0];

    private static ViewHandler instance;

    private readonly Dictionary<string, View> viewLookup = new Dictionary<string, View>(16, StringComparer.Ordinal);
    private readonly Stack<View> screenHistory = new Stack<View>(8);
    private readonly Stack<View> overlayStack = new Stack<View>(4);
    private readonly HashSet<View> openOverlays = new HashSet<View>();

    private View currentScreen;

    public static View CurrentScreen => instance != null ? instance.currentScreen : null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError($"[{nameof(ViewHandler)}] Duplicate instance on '{name}'.", this);
            Destroy(this);
            return;
        }

        instance = this;
        BuildLookup();
        PrepareAllViewsHidden();
    }

    private void Start()
    {
        if (instance != this)
            return;

        ShowStartingView();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    [ContextMenu("Assign Views")]
    public void AssignViews()
    {
#if UNITY_EDITOR
        View[] found = GetComponentsInChildren<View>(true);
        int count = 0;

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] == null)
                continue;
            if (headerView != null && found[i] == headerView)
                continue;
            count++;
        }

        View[] assigned = new View[count];
        int writeIndex = 0;

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] == null)
                continue;
            if (headerView != null && found[i] == headerView)
                continue;

            assigned[writeIndex++] = found[i];
        }

        views = assigned;
        Array.Sort(views, CompareViewsById);
#endif
    }

    [ContextMenu("Clear Views")]
    public void ClearViews()
    {
        views = new View[0];
    }

    public static void Show(View target, bool rememberCurrent = true)
    {
        if (instance == null)
            return;

        instance.ShowInternal(target, rememberCurrent);
    }

    public static void Show(string viewID, bool rememberCurrent = true)
    {
        if (instance == null || string.IsNullOrEmpty(viewID))
            return;

        if (!instance.viewLookup.TryGetValue(viewID, out View target) || target == null)
            return;

        instance.ShowInternal(target, rememberCurrent);
    }

    public static void ShowLast()
    {
        if (instance == null)
            return;

        instance.ShowLastInternal();
    }

    private void ShowInternal(View target, bool rememberCurrent)
    {
        if (target == null)
            return;

        if (headerView != null && target == headerView)
            return;

        if (!IsRegistered(target))
            return;

        if (!target.HasValidCanvasGroup())
            return;

        if (target.ViewType == ViewType.Overlay)
            ShowOverlay(target);
        else
            ShowScreen(target, rememberCurrent);
    }

    private void ShowScreen(View target, bool rememberCurrent)
    {
        if (currentScreen == target)
            return;

        HideAllOverlays();

        if (rememberCurrent && currentScreen != null)
            PushHistoryIfNeeded(currentScreen);

        if (currentScreen != null)
            currentScreen.Hide();

        UpdateHeader(target.ShowHeader);
        target.Show();

        if (!target.IsVisible)
            return;

        currentScreen = target;
    }

    private void ShowOverlay(View target)
    {
        if (target.IsVisible || openOverlays.Contains(target))
            return;

        target.Show();

        if (!target.IsVisible)
            return;

        overlayStack.Push(target);
        openOverlays.Add(target);
    }

    private void ShowLastInternal()
    {
        if (overlayStack.Count > 0)
        {
            HideTopOverlay();
            return;
        }

        View previous = PopValidHistoryEntry();
        if (previous == null)
            return;

        if (currentScreen != null)
            currentScreen.Hide();

        UpdateHeader(previous.ShowHeader);
        previous.Show();

        if (!previous.IsVisible)
            return;

        currentScreen = previous;
    }

    private void BuildLookup()
    {
        viewLookup.Clear();

        if (views != null)
        {
            for (int i = 0; i < views.Length; i++)
                TryRegisterView(views[i]);
        }

        TryRegisterView(startingView);
    }

    private void TryRegisterView(View view)
    {
        if (view == null)
            return;

        if (headerView != null && view == headerView)
            return;

        if (string.IsNullOrEmpty(view.ViewID))
        {
            Debug.LogError($"[{nameof(ViewHandler)}] View '{view.name}' has an empty View ID.", view);
            return;
        }

        if (viewLookup.TryGetValue(view.ViewID, out View existing))
        {
            // Same View registered twice (e.g. in Views array and as Starting View) — ignore.
            if (existing == view)
                return;

            Debug.LogError($"[{nameof(ViewHandler)}] Duplicate View ID '{view.ViewID}' on '{view.name}'.", view);
            return;
        }

        viewLookup.Add(view.ViewID, view);
    }

    private void PrepareAllViewsHidden()
    {
        bool startingHidden = false;

        if (views != null)
        {
            for (int i = 0; i < views.Length; i++)
            {
                View view = views[i];
                if (view == null)
                    continue;

                if (headerView != null && view == headerView)
                    continue;

                view.ApplyHiddenWithoutEvents();

                if (view == startingView)
                    startingHidden = true;
            }
        }

        if (startingView != null && !startingHidden && startingView != headerView)
            startingView.ApplyHiddenWithoutEvents();
    }

    private void ShowStartingView()
    {
        if (startingView == null || startingView.ViewType == ViewType.Overlay)
            return;

        if (!IsRegistered(startingView) || !startingView.HasValidCanvasGroup())
            return;

        ShowScreen(startingView, false);
    }

    private void UpdateHeader(bool show)
    {
        if (headerView == null)
            return;

        if (show)
            headerView.Show();
        else
            headerView.Hide();
    }

    private bool IsRegistered(View target)
    {
        if (target == null || string.IsNullOrEmpty(target.ViewID))
            return false;

        return viewLookup.TryGetValue(target.ViewID, out View registered) && registered == target;
    }

    private void PushHistoryIfNeeded(View screen)
    {
        if (screenHistory.Count > 0 && screenHistory.Peek() == screen)
            return;

        screenHistory.Push(screen);
    }

    private View PopValidHistoryEntry()
    {
        while (screenHistory.Count > 0)
        {
            View entry = screenHistory.Pop();
            if (entry != null)
                return entry;
        }

        return null;
    }

    private void HideAllOverlays()
    {
        while (overlayStack.Count > 0)
        {
            View overlay = overlayStack.Pop();
            if (overlay != null && overlay.IsVisible)
                overlay.Hide();
        }

        openOverlays.Clear();
    }

    private void HideTopOverlay()
    {
        while (overlayStack.Count > 0)
        {
            View overlay = overlayStack.Pop();
            if (overlay == null)
                continue;

            openOverlays.Remove(overlay);

            if (overlay.IsVisible)
                overlay.Hide();

            return;
        }
    }

    private static int CompareViewsById(View a, View b)
    {
        if (a == null && b == null)
            return 0;
        if (a == null)
            return 1;
        if (b == null)
            return -1;

        return string.CompareOrdinal(a.ViewID ?? string.Empty, b.ViewID ?? string.Empty);
    }
}
