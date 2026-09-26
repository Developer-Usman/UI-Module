using System;

namespace RP_DataSavingSystem
{
    [Serializable]
    public sealed class CarSettingsSave
    {
        public int frameRate = 60;
        public float masterVolume = 1f;
        public float musicVolume = 0.5f;
        public float sensitivity = 0.35f;
    }
}
