using System;

namespace RP_DataSavingSystem
{
    [Serializable]
    public sealed class CarSaveData
    {
        public int saveVersion = CarSaveSystem.CurrentSaveVersion;
        public CarPlayerProfileSave playerProfile = new CarPlayerProfileSave();
        public CarWalletSave wallet = new CarWalletSave();
        public CarSettingsSave settings = new CarSettingsSave();
        public CarCollectionSave collection = new CarCollectionSave();
    }
}
