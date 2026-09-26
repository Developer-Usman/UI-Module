using System;

namespace RP_DataSavingSystem
{
    [Serializable]
    public sealed class CarOwnedState
    {
        public string carId;
        public bool unlocked;
        public int upgradeLevel;

        public CarOwnedState(string carId, bool unlocked = false)
        {
            this.carId = carId;
            this.unlocked = unlocked;
            upgradeLevel = 0;
        }
    }
}
