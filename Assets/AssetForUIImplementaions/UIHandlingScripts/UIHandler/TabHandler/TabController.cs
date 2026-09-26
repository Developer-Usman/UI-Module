using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class TabEntry
{
    [Tooltip("Unique ID used by TabController.ShowTab(string).")]
    public string tabID;

    [Tooltip("Button that activates this tab.")]
    public Button tabButton;

    [Tooltip("Tab panel. Not registered with ViewHandler.")]
    public TabView tabView;

    [Tooltip("Optional click sound played when this tab button is pressed.")]
    public AudioClip clickClip;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class TabController : MonoBehaviour
{
    [Header("Parent View")]
    [Tooltip("Parent screen/overlay View. Auto-binds ShowDefaultTab on show and HideAllTabs on hide.")]
    [SerializeField] private View parentView;

    [Header("Tabs")]
    [Tooltip("Manually assigned tab entries.")]
    [SerializeField] private TabEntry[] tabs = new TabEntry[0];

    [Tooltip("Tab ID shown when the parent View opens.")]
    [SerializeField] private string defaultTabID;

    [Tooltip("Shared AudioSource used to play tab button click clips.")]
    [SerializeField] private AudioSource tabsAudioSource;

    [Header("Button Sprites")]
    [Tooltip("Sprite applied to the selected tab button. Leave empty to hide the button image (alpha 0).")]
    [SerializeField] private Sprite activeSprite;

    [Tooltip("Sprite applied to non-selected tab buttons. Leave empty to hide the button image (alpha 0).")]
    [SerializeField] private Sprite inactiveSprite;

    private readonly Dictionary<string, TabEntry> tabLookup = new Dictionary<string, TabEntry>(8, StringComparer.Ordinal);

    private UnityAction[] buttonActions;
    private UnityAction parentShowAction;
    private UnityAction parentHideAction;

    private TabEntry currentTab;
    private string currentTabID;
    private bool isInitialized;
    private bool parentBound;

    public string CurrentTabID => currentTabID ?? string.Empty;

    [ContextMenu("Assign Tabs Audio Source")]
    public void AssignTabsAudioSource()
    {
#if UNITY_EDITOR
        tabsAudioSource = GetComponent<AudioSource>();
#endif
    }

    private void Awake()
    {
        Initialize();
        BindParentView();
    }

    private void OnDestroy()
    {
        UnbindParentView();
        UnbindButtons();
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;
        BuildLookup();
        BindButtons();
        HideAllTabs();
    }

    public void ShowTab(string tabID)
    {
        Initialize();

        if (string.IsNullOrEmpty(tabID))
            return;

        if (string.Equals(currentTabID, tabID, StringComparison.Ordinal))
            return;

        if (!tabLookup.TryGetValue(tabID, out TabEntry target) || target.tabView == null)
            return;

        if (currentTab != null && currentTab.tabView != null && currentTab.tabView.IsVisible)
            currentTab.tabView.Hide();

        target.tabView.Show();

        if (!target.tabView.IsVisible)
            return;

        SetButtonSprite(currentTab, inactiveSprite);
        SetButtonSprite(target, activeSprite);

        currentTab = target;
        currentTabID = tabID;
    }

    public void ShowTab(int index)
    {
        Initialize();

        if (tabs == null || index < 0 || index >= tabs.Length)
            return;

        TabEntry entry = tabs[index];
        if (entry == null || string.IsNullOrEmpty(entry.tabID))
            return;

        ShowTab(entry.tabID);
    }

    public void ShowDefaultTab()
    {
        ShowTab(defaultTabID);
    }

    public void HideAllTabs()
    {
        Initialize();

        if (tabs != null)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                TabEntry entry = tabs[i];
                if (entry == null)
                    continue;

                if (entry.tabView != null)
                {
                    if (entry.tabView.IsVisible)
                        entry.tabView.Hide();
                    else
                        entry.tabView.ApplyHiddenWithoutEvents();
                }

                SetButtonSprite(entry, inactiveSprite);
            }
        }

        currentTab = null;
        currentTabID = null;
    }

    public void HideCurrentTab()
    {
        HideAllTabs();
    }

    private void BindParentView()
    {
        if (parentBound || parentView == null)
            return;

        parentShowAction = ShowDefaultTab;
        parentHideAction = HideAllTabs;

        parentView.OnShowEvent.AddListener(parentShowAction);
        parentView.OnHideEvent.AddListener(parentHideAction);
        parentBound = true;
    }

    private void UnbindParentView()
    {
        if (!parentBound || parentView == null)
            return;

        if (parentShowAction != null)
            parentView.OnShowEvent.RemoveListener(parentShowAction);

        if (parentHideAction != null)
            parentView.OnHideEvent.RemoveListener(parentHideAction);

        parentShowAction = null;
        parentHideAction = null;
        parentBound = false;
    }

    private void BuildLookup()
    {
        tabLookup.Clear();

        if (tabs == null)
            return;

        for (int i = 0; i < tabs.Length; i++)
        {
            TabEntry entry = tabs[i];
            if (entry == null || string.IsNullOrEmpty(entry.tabID))
                continue;

            if (tabLookup.ContainsKey(entry.tabID))
            {
                Debug.LogError($"[{nameof(TabController)}] Duplicate Tab ID '{entry.tabID}'.", this);
                continue;
            }

            tabLookup.Add(entry.tabID, entry);
        }
    }

    private void BindButtons()
    {
        if (tabs == null || tabs.Length == 0)
            return;

        buttonActions = new UnityAction[tabs.Length];

        for (int i = 0; i < tabs.Length; i++)
        {
            TabEntry entry = tabs[i];
            if (entry == null || entry.tabButton == null || string.IsNullOrEmpty(entry.tabID))
                continue;

            TabEntry captured = entry;
            buttonActions[i] = () => OnTabButtonClicked(captured);
            entry.tabButton.onClick.AddListener(buttonActions[i]);
        }
    }

    private void OnTabButtonClicked(TabEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.tabID))
            return;

        PlayClickClip(entry);
        ShowTab(entry.tabID);
    }

    private void PlayClickClip(TabEntry entry)
    {
        if (tabsAudioSource == null || entry == null || entry.clickClip == null)
            return;

        tabsAudioSource.PlayOneShot(entry.clickClip);
    }

    private void UnbindButtons()
    {
        if (tabs == null || buttonActions == null)
            return;

        int count = tabs.Length < buttonActions.Length ? tabs.Length : buttonActions.Length;
        for (int i = 0; i < count; i++)
        {
            TabEntry entry = tabs[i];
            if (entry == null || entry.tabButton == null || buttonActions[i] == null)
                continue;

            entry.tabButton.onClick.RemoveListener(buttonActions[i]);
        }

        buttonActions = null;
    }

    private static void SetButtonSprite(TabEntry entry, Sprite sprite)
    {
        if (entry == null || entry.tabButton == null)
            return;

        Image image = entry.tabButton.image;
        if (image == null)
            return;

        Color color = image.color;

        if (sprite == null)
        {
            color.a = 0f;
            image.color = color;
            return;
        }

        image.sprite = sprite;
        color.a = 1f;
        image.color = color;
    }
}
