namespace RP_MissionSystem
{
    /// <summary>
    /// Every gameplay category a mission can track.
    /// Explicit integer values keep serialized assets / saves stable if entries are reordered.
    /// </summary>
    public enum MissionType
    {
        TargetSpeed       = 0,  // Reach a top speed (best value counts).
        TargetDistance    = 1,  // Drive a cumulative distance.
        TargetDrifts      = 2,  // Perform N drifts.
        TargetHeight      = 3,  // Reach a jump height (best value counts).
        AnyMission        = 4,  // Complete N other daily missions (driven by the system itself).
        RaceMission       = 5,  // Finish N races.
        CheckpointMission = 6,  // Pass N checkpoints.
        StuntMission      = 7,  // Perform N stunts.
        CollectGold       = 8,  // Collect N gold in gameplay.
        CollectSilver     = 9,  // Collect N silver in gameplay.
        CollectDiamonds   = 10, // Collect N diamonds in gameplay.
        IncreaseXPs       = 11, // Earn N XP in gameplay.
        CrashVehicles     = 12  // Crash into N vehicles.
    }

    /// <summary>Currency / resource granted when a mission is claimed.</summary>
    public enum RewardType
    {
        XP      = 0,
        Gold    = 1,
        Silver  = 2,
        Diamond = 3
    }

    /// <summary>Lifecycle of a single daily mission. Drives the card visuals.</summary>
    public enum MissionState
    {
        InProgress = 0, // Target not reached yet – claim button disabled.
        Claimable  = 1, // Target reached, reward not taken – claim button enabled.
        Claimed    = 2  // Reward taken – "Claimed" label shown.
    }

    /// <summary>How an incoming progress value is merged into the stored progress.</summary>
    public enum ProgressMode
    {
        Additive = 0, // progress += value   (distance, drifts, collectibles...)
        Max      = 1  // progress = max(progress, value)   (speed, height)
    }

    public static class MissionTypeExtensions
    {
        /// <summary>
        /// Single source of truth for how each mission type accumulates progress.
        /// Kept in code (not on the asset) so designers cannot misconfigure it.
        /// </summary>
        public static ProgressMode GetProgressMode(this MissionType type)
        {
            switch (type)
            {
                case MissionType.TargetSpeed:
                case MissionType.TargetHeight:
                    return ProgressMode.Max;
                default:
                    return ProgressMode.Additive;
            }
        }
    }
}
