using System.Collections.Generic;
using RP_MissionSystem;
using TMPro;
using UnityEngine;

public class MissionProgressionScreen : MonoBehaviour
{
    [Header("Init")]
    [SerializeField] private bool isInit;

    [Header("Mission Cards")]
    [SerializeField] private MissionCardUI cardPrefab;
    [Tooltip("Parent with a LayoutGroup where cards are spawned.")]
    [SerializeField] private Transform cardContainer;

    [Header("Header")]
    [Tooltip("Shows claimed / total, e.g. 3/5.")]
    [SerializeField] private TMP_Text headerCountText;

    private View _view;

    // Cards are spawned once and reused across shows / daily refreshes.
    private readonly List<MissionCardUI> _cards = new List<MissionCardUI>();
    private readonly Dictionary<string, MissionCardUI> _cardsById = new Dictionary<string, MissionCardUI>();
    private bool _needsRebuild = true;

    private void Awake()
    {
        _view = GetComponent<View>();
        _view.OnInitEvent.AddListener(OnViewInit);
        _view.OnShowEvent.AddListener(OnViewShow);
        _view.OnHideEvent.AddListener(OnViewHide);

        // Always listened to (even while hidden) so a new day forces a rebuild on next show.
        MissionEvents.OnDailyMissionsRefreshed += HandleDailyMissionsRefreshed;

        RegisterButtonListeners();
    }

    private void OnDestroy()
    {
        _view.OnInitEvent.RemoveListener(OnViewInit);
        _view.OnShowEvent.RemoveListener(OnViewShow);
        _view.OnHideEvent.RemoveListener(OnViewHide);

        MissionEvents.OnDailyMissionsRefreshed -= HandleDailyMissionsRefreshed;
        MissionEvents.OnMissionStateChanged -= HandleMissionStateChanged;

        RemoveButtonListeners();
    }

    #region View Lifecycle

    private void OnViewInit()
    {
        Init();
    }

    private void Init()
    {
        if (isInit)
            return;

        isInit = true;
    }

    private void OnViewShow()
    {
        // Per-mission updates are only needed while the screen is visible.
        MissionEvents.OnMissionStateChanged -= HandleMissionStateChanged; // Guard against double subscription.
        MissionEvents.OnMissionStateChanged += HandleMissionStateChanged;
        RefreshView();
    }

    private void OnViewHide()
    {
        MissionEvents.OnMissionStateChanged -= HandleMissionStateChanged;
    }

    #endregion

    #region Button Listeners

    private void RegisterButtonListeners()
    {

    }

    private void RemoveButtonListeners()
    {

    }

    #endregion

    #region Button Actions

    #endregion

    #region Mission Events

    private void HandleDailyMissionsRefreshed()
    {
        _needsRebuild = true;

        if (_view != null && _view.IsVisible)
            RefreshView();
    }

    private void HandleMissionStateChanged(string missionID)
    {
        if (_cardsById.TryGetValue(missionID, out MissionCardUI card))
            card.Refresh();

        RefreshHeader();
    }

    #endregion

    #region View Refresh

    private void RefreshView()
    {
        if (MissionManager.Instance == null)
        {
            Debug.LogWarning("[MissionProgressionScreen] No MissionManager in scene.", this);
            return;
        }

        if (_needsRebuild)
            RebuildCards();
        else
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i].gameObject.activeSelf) _cards[i].Refresh();

        RefreshHeader();
    }

    /// <summary>Binds today's missions to cards, instantiating only when more are needed than already exist.</summary>
    private void RebuildCards()
    {
        IReadOnlyList<MissionDataSO> missions = MissionManager.Instance.ActiveMissions;
        _cardsById.Clear();

        while (_cards.Count < missions.Count)
            _cards.Add(Instantiate(cardPrefab, cardContainer));

        for (int i = 0; i < _cards.Count; i++)
        {
            bool used = i < missions.Count;
            _cards[i].gameObject.SetActive(used);

            if (!used)
                continue;

            _cards[i].Bind(missions[i]);
            _cardsById[missions[i].MissionID] = _cards[i];
        }

        _needsRebuild = false;
    }

    private void RefreshHeader()
    {
        MissionManager manager = MissionManager.Instance;
        if (headerCountText != null && manager != null)
            headerCountText.SetText("{0}/{1}", manager.ClaimedCount, manager.TotalCount);
    }

    #endregion
}
