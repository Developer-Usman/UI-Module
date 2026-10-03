using System;
using System.Collections.Generic;
using UnityEngine;

namespace RP_MissionSystem
{
    /// <summary>A single reward entry shown on a mission card and granted on claim.</summary>
    [Serializable]
    public struct MissionReward
    {
        public RewardType type;
        [Min(1)] public int amount;
        public Sprite icon;
    }

    /// <summary>
    /// Static, designer-authored definition of a mission. Contains NO runtime state –
    /// progress / claim flags live in <see cref="MissionSaveData"/> so assets are never dirtied at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "Mission_", menuName = "Mission System/Mission Data", order = 0)]
    public sealed class MissionDataSO : ScriptableObject
    {
        [Tooltip("Unique, stable identifier used for saving. Auto-generated if left empty. Do NOT change after release.")]
        [SerializeField] private string missionID;

        [SerializeField] private string missionTitle = "New Mission";
        [SerializeField] private MissionType missionType;

        [Tooltip("Value required to complete the mission (e.g. 200 km/h, 5 drifts, 1000 m).")]
        [SerializeField, Min(0.01f)] private float targetValue = 1f;

        [Tooltip("Rewards granted on claim. Cards display up to their number of reward slots.")]
        [SerializeField] private List<MissionReward> rewards = new List<MissionReward>();

        public string MissionID => missionID;
        public string MissionTitle => missionTitle;
        public MissionType MissionType => missionType;
        public float TargetValue => targetValue;
        public IReadOnlyList<MissionReward> Rewards => rewards;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign a GUID once so every asset has a stable save key.
            if (string.IsNullOrWhiteSpace(missionID))
            {
                missionID = Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        /// <summary>Regenerates the ID – use only for duplicated assets (Ctrl+D copies the ID too).</summary>
        [ContextMenu("Regenerate Mission ID")]
        private void RegenerateID()
        {
            missionID = Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
