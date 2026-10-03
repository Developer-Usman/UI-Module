using System;
using System.Collections.Generic;

namespace RP_MissionSystem
{
    /// <summary>
    /// Everything persisted for the daily missions, stored under a single Easy Save 3 key.
    /// A new day simply replaces this object, so no stale per-mission keys are ever left behind.
    /// </summary>
    [Serializable]
    public sealed class MissionSaveData
    {
        public int saveVersion;

        /// <summary>Local date the set was generated for, format yyyyMMdd.</summary>
        public string dayStamp;

        /// <summary>Active daily missions, in display order.</summary>
        public List<MissionProgressEntry> entries = new List<MissionProgressEntry>();
    }

    [Serializable]
    public sealed class MissionProgressEntry
    {
        public string missionID;
        public float progress;
        public bool claimed;

        public MissionProgressEntry() { }

        public MissionProgressEntry(string missionID)
        {
            this.missionID = missionID;
        }
    }
}
