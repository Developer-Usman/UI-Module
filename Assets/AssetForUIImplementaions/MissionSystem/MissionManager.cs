using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RP_MissionSystem
{
    /// <summary>
    /// Persistent, UI-agnostic owner of the daily mission state.
    /// <list type="bullet">
    /// <item>Picks <see cref="dailyMissionCount"/> missions per local day (date-seeded, distinct types preferred).</item>
    /// <item>Listens to <see cref="MissionEvents.OnProgressUpdated"/> and updates progress.</item>
    /// <item>Persists everything under ONE Easy Save 3 key with a dirty-flag + throttled flush.</item>
    /// <item>Handles claiming and broadcasts rewards via <see cref="MissionEvents.OnRewardClaimed"/>.</item>
    /// </list>
    /// UI reads state through the public API and reacts to <see cref="MissionEvents"/>; it never mutates state directly.
    /// </summary>
    [DefaultExecutionOrder(-90)] // After CarSaveSystem (-100), before regular UI scripts.
    public sealed class MissionManager : MonoBehaviour
    {
        public const int CurrentSaveVersion = 1;
        private const string DayStampFormat = "yyyyMMdd";
        private const float DayCheckInterval = 30f;

        public static MissionManager Instance { get; private set; }

        [Header("Mission Pool")]
        [Tooltip("All missions that can be picked as daily missions.")]
        [SerializeField] private List<MissionDataSO> missionPool = new List<MissionDataSO>();
        [SerializeField, Min(1)] private int dailyMissionCount = 5;

        [Header("Save")]
        [SerializeField] private string saveKey = "DailyMissionSave";
        [Tooltip("Minimum seconds between automatic saves while progress is changing.")]
        [SerializeField, Min(0.5f)] private float saveInterval = 3f;

        /// <summary>Runtime pairing of a definition with its persisted progress entry.</summary>
        private sealed class ActiveMission
        {
            public readonly MissionDataSO Data;
            public readonly MissionProgressEntry Entry;

            public ActiveMission(MissionDataSO data, MissionProgressEntry entry)
            {
                Data = data;
                Entry = entry;
            }

            public bool IsCompleted => Entry.progress >= Data.TargetValue;

            public MissionState State =>
                Entry.claimed ? MissionState.Claimed :
                IsCompleted ? MissionState.Claimable :
                MissionState.InProgress;
        }

        private readonly Dictionary<string, MissionDataSO> _poolLookup = new Dictionary<string, MissionDataSO>();
        private readonly List<ActiveMission> _active = new List<ActiveMission>();
        private readonly List<MissionDataSO> _activeData = new List<MissionDataSO>();
        private readonly Dictionary<string, ActiveMission> _activeById = new Dictionary<string, ActiveMission>();
        private readonly Dictionary<MissionType, List<ActiveMission>> _activeByType = new Dictionary<MissionType, List<ActiveMission>>();

        private MissionSaveData _saveData;
        private bool _isDirty;
        private float _lastSaveTime;
        private float _nextDayCheckTime;

        #region Public API

        /// <summary>Today's missions in display order.</summary>
        public IReadOnlyList<MissionDataSO> ActiveMissions => _activeData;

        public int TotalCount => _active.Count;

        public int ClaimedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _active.Count; i++)
                    if (_active[i].Entry.claimed) count++;
                return count;
            }
        }

        /// <summary>Local date (yyyyMMdd) the current set belongs to.</summary>
        public string CurrentDayStamp => _saveData != null ? _saveData.dayStamp : string.Empty;

        public float GetProgress(string missionID) =>
            _activeById.TryGetValue(missionID, out ActiveMission m) ? m.Entry.progress : 0f;

        public MissionState GetState(string missionID) =>
            _activeById.TryGetValue(missionID, out ActiveMission m) ? m.State : MissionState.InProgress;

        /// <summary>
        /// Claims a completed mission: marks it claimed, broadcasts every reward and saves immediately.
        /// Returns false if the mission is unknown, not completed or already claimed.
        /// </summary>
        public bool TryClaim(string missionID)
        {
            if (string.IsNullOrEmpty(missionID) ||
                !_activeById.TryGetValue(missionID, out ActiveMission mission) ||
                mission.State != MissionState.Claimable)
                return false;

            mission.Entry.claimed = true;
            SaveNow(); // Persist the claim before granting, so a crash can't allow a double claim.

            IReadOnlyList<MissionReward> rewards = mission.Data.Rewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i].amount > 0)
                    MissionEvents.RaiseRewardClaimed(rewards[i].type, rewards[i].amount);
            }

            MissionEvents.RaiseMissionStateChanged(missionID);
            return true;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildPoolLookup();
            LoadOrCreate();
        }

        private void OnEnable() => MissionEvents.OnProgressUpdated += HandleProgressUpdated;

        private void OnDisable() => MissionEvents.OnProgressUpdated -= HandleProgressUpdated;

        private void Update()
        {
            float now = Time.unscaledTime;

            // Throttled flush: high-frequency events (speed, distance) only mark dirty.
            if (_isDirty && now - _lastSaveTime >= saveInterval)
                SaveNow();

            // Handles sessions that run past local midnight.
            if (now >= _nextDayCheckTime)
            {
                _nextDayCheckTime = now + DayCheckInterval;
                CheckDayRollover();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveIfDirty();
            else CheckDayRollover();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) CheckDayRollover();
        }

        private void OnApplicationQuit() => SaveIfDirty();

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            SaveIfDirty();
            Instance = null;
        }

        #endregion

        #region Progress

        private void HandleProgressUpdated(MissionType type, float value)
        {
            if (!_activeByType.TryGetValue(type, out List<ActiveMission> missions))
                return; // No active mission of this type today – zero cost.

            ProgressMode mode = type.GetProgressMode();
            for (int i = 0; i < missions.Count; i++)
                ApplyProgress(missions[i], value, mode);
        }

        private void ApplyProgress(ActiveMission mission, float value, ProgressMode mode)
        {
            MissionProgressEntry entry = mission.Entry;
            if (entry.claimed || mission.IsCompleted)
                return;

            float target = mission.Data.TargetValue;
            float oldProgress = entry.progress;
            float newProgress = mode == ProgressMode.Max ? Mathf.Max(oldProgress, value) : oldProgress + value;
            newProgress = Mathf.Min(newProgress, target);

            if (newProgress <= oldProgress)
                return;

            entry.progress = newProgress;
            _isDirty = true;

            bool justCompleted = newProgress >= target;

            // Only notify UI when the displayed (whole-number) value or the state changes,
            // so per-frame distance/speed events don't trigger per-frame UI refreshes.
            if (justCompleted || Mathf.FloorToInt(newProgress) != Mathf.FloorToInt(oldProgress))
                MissionEvents.RaiseMissionStateChanged(entry.missionID);

            if (!justCompleted)
                return;

            SaveNow(); // Completion is a milestone – persist immediately.

            // "Complete N missions" – fed by the system, never by AnyMission itself (prevents recursion).
            if (mission.Data.MissionType != MissionType.AnyMission)
                HandleProgressUpdated(MissionType.AnyMission, 1f);
        }

        #endregion

        #region Daily Set

        private void BuildPoolLookup()
        {
            _poolLookup.Clear();
            for (int i = 0; i < missionPool.Count; i++)
            {
                MissionDataSO data = missionPool[i];
                if (data == null || string.IsNullOrEmpty(data.MissionID))
                    continue;

                if (_poolLookup.ContainsKey(data.MissionID))
                {
                    Debug.LogWarning($"[MissionManager] Duplicate missionID '{data.MissionID}' on '{data.name}'. " +
                                     "Use 'Regenerate Mission ID' on duplicated assets. Skipping.", data);
                    continue;
                }

                _poolLookup.Add(data.MissionID, data);
            }
        }

        private static string TodayStamp() => DateTime.Now.ToString(DayStampFormat, CultureInfo.InvariantCulture);

        private void CheckDayRollover()
        {
            if (_saveData != null && _saveData.dayStamp != TodayStamp())
                GenerateNewDay(TodayStamp());
        }

        /// <summary>Replaces the whole save with a fresh, date-seeded set. Old progress/claims vanish with it.</summary>
        private void GenerateNewDay(string dayStamp)
        {
            _saveData = new MissionSaveData
            {
                saveVersion = CurrentSaveVersion,
                dayStamp = dayStamp
            };

            List<MissionDataSO> picked = PickMissions(dayStamp);
            for (int i = 0; i < picked.Count; i++)
                _saveData.entries.Add(new MissionProgressEntry(picked[i].MissionID));

            RebuildRuntime();
            SaveNow();
            MissionEvents.RaiseDailyMissionsRefreshed();
        }

        /// <summary>
        /// Deterministic per day: same date → same set. Two passes:
        /// 1) one mission per distinct MissionType, 2) fill remaining slots with any unused mission.
        /// </summary>
        private List<MissionDataSO> PickMissions(string dayStamp)
        {
            List<MissionDataSO> candidates = new List<MissionDataSO>(_poolLookup.Values);
            candidates.Sort((a, b) => string.CompareOrdinal(a.MissionID, b.MissionID)); // Stable base order.

            int seed = int.TryParse(dayStamp, NumberStyles.None, CultureInfo.InvariantCulture, out int s) ? s : Environment.TickCount;
            System.Random rng = new System.Random(seed);

            for (int i = candidates.Count - 1; i > 0; i--) // Fisher–Yates
            {
                int j = rng.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            int count = Mathf.Min(dailyMissionCount, candidates.Count);
            List<MissionDataSO> result = new List<MissionDataSO>(count);
            HashSet<MissionType> usedTypes = new HashSet<MissionType>();

            for (int i = 0; i < candidates.Count && result.Count < count; i++)
            {
                if (usedTypes.Add(candidates[i].MissionType))
                    result.Add(candidates[i]);
            }

            for (int i = 0; i < candidates.Count && result.Count < count; i++)
            {
                if (!result.Contains(candidates[i]))
                    result.Add(candidates[i]);
            }

            if (result.Count < dailyMissionCount)
                Debug.LogWarning($"[MissionManager] Pool has only {result.Count} valid missions (wanted {dailyMissionCount}).", this);

            return result;
        }

        /// <summary>Rebuilds lookup tables from save data, dropping entries whose asset no longer exists.</summary>
        private void RebuildRuntime()
        {
            _active.Clear();
            _activeData.Clear();
            _activeById.Clear();
            _activeByType.Clear();

            for (int i = 0; i < _saveData.entries.Count; i++)
            {
                MissionProgressEntry entry = _saveData.entries[i];

                // Drop null, removed-from-pool or duplicated entries (e.g. after a content update).
                if (entry == null || string.IsNullOrEmpty(entry.missionID) ||
                    !_poolLookup.ContainsKey(entry.missionID) || _activeById.ContainsKey(entry.missionID))
                {
                    _saveData.entries.RemoveAt(i--);
                    _isDirty = true;
                    continue;
                }

                ActiveMission mission = new ActiveMission(_poolLookup[entry.missionID], entry);

                _active.Add(mission);
                _activeData.Add(mission.Data);
                _activeById.Add(entry.missionID, mission);

                if (!_activeByType.TryGetValue(mission.Data.MissionType, out List<ActiveMission> list))
                {
                    list = new List<ActiveMission>(1);
                    _activeByType.Add(mission.Data.MissionType, list);
                }
                list.Add(mission);
            }
        }

        #endregion

        #region Save / Load (Easy Save 3)

        private void LoadOrCreate()
        {
            string today = TodayStamp();
            MissionSaveData loaded = null;

            if (ES3.KeyExists(saveKey))
            {
                try
                {
                    loaded = ES3.Load<MissionSaveData>(saveKey);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[MissionManager] Corrupt mission save, regenerating. {e.Message}", this);
                }
            }

            bool reusable = loaded != null &&
                            loaded.saveVersion == CurrentSaveVersion &&
                            loaded.dayStamp == today &&
                            loaded.entries != null;

            if (!reusable)
            {
                GenerateNewDay(today);
                return;
            }

            _saveData = loaded;
            RebuildRuntime();
            SaveIfDirty();
            MissionEvents.RaiseDailyMissionsRefreshed();
        }

        private void SaveIfDirty()
        {
            if (_isDirty) SaveNow();
        }

        public void SaveNow()
        {
            if (_saveData == null || string.IsNullOrEmpty(saveKey))
                return;

            _saveData.saveVersion = CurrentSaveVersion;
            ES3.Save(saveKey, _saveData);
            _isDirty = false;
            _lastSaveTime = Time.unscaledTime;
        }

        #endregion

        #region Debug

        [ContextMenu("Debug/Force New Daily Set (reset progress)")]
        private void DebugForceNewDay()
        {
            if (!Application.isPlaying) return;
            BuildPoolLookup();
            GenerateNewDay(TodayStamp());
        }

        [ContextMenu("Debug/Complete All Missions")]
        private void DebugCompleteAll()
        {
            if (!Application.isPlaying) return;
            for (int i = 0; i < _active.Count; i++)
                ApplyProgress(_active[i], _active[i].Data.TargetValue, ProgressMode.Max);
        }

        [ContextMenu("Debug/Delete Mission Save")]
        private void DebugDeleteSave()
        {
            if (ES3.KeyExists(saveKey))
                ES3.DeleteKey(saveKey);

            _isDirty = false;
            Debug.Log("[MissionManager] Mission save deleted.");
        }

        #endregion
    }
}
