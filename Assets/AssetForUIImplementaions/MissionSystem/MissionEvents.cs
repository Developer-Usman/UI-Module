using System;

namespace RP_MissionSystem
{
    /// <summary>
    /// Central, decoupled event hub for the mission system.
    /// Gameplay code only needs to call <see cref="ReportProgress"/>; it never references the manager.
    /// </summary>
    public static class MissionEvents
    {
        /// <summary>Gameplay → System. Raised whenever a trackable action happens.</summary>
        public static event Action<MissionType, float> OnProgressUpdated;

        /// <summary>System → UI. A mission's progress or state changed (argument = missionID).</summary>
        public static event Action<string> OnMissionStateChanged;

        /// <summary>System → Economy. A reward was claimed and should be granted.</summary>
        public static event Action<RewardType, int> OnRewardClaimed;

        /// <summary>System → UI. A new daily set was generated (or loaded); cards must be rebuilt.</summary>
        public static event Action OnDailyMissionsRefreshed;

        /// <summary>
        /// Call from gameplay. Examples:
        /// <code>
        /// MissionEvents.ReportProgress(MissionType.TargetSpeed, currentKmh);   // Max mode – send the current value
        /// MissionEvents.ReportProgress(MissionType.TargetDrifts, 1);           // Additive – send the delta
        /// MissionEvents.ReportProgress(MissionType.TargetDistance, metersThisFrame);
        /// </code>
        /// </summary>
        public static void ReportProgress(MissionType type, float value)
        {
            if (value > 0f)
                OnProgressUpdated?.Invoke(type, value);
        }

        internal static void RaiseMissionStateChanged(string missionID) => OnMissionStateChanged?.Invoke(missionID);
        internal static void RaiseRewardClaimed(RewardType type, int amount) => OnRewardClaimed?.Invoke(type, amount);
        internal static void RaiseDailyMissionsRefreshed() => OnDailyMissionsRefreshed?.Invoke();
    }
}
