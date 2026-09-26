using UnityEngine;

namespace RP_DataSavingSystem
{
    [CreateAssetMenu(menuName = "Car Database/Car Definition")]
    public sealed class CarDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string carId;
        public string displayName;

        [Header("Visuals")]
        public GameObject prefab;
        public Sprite thumbnail;

        [Header("Classification")]
        public CarControllerType controllerType = CarControllerType.DailyDrivers;
        public CarClass carClass = CarClass.D;
        public DriveWheel driveWheel = DriveWheel.FWD;
        public string country;
        [Min(0)] public int year;

        [Header("Stats")]
        [Min(0f)] public float topSpeed;
        [Min(0f)] public float power;
        [Min(0f)] public float zeroToHundred;
        [Min(0f)] public float handling;
        [Min(0f)] public float scoring;

        [Header("Ownership")]
        public bool initiallyOwned;
        [Min(0)] public int maxUpgradeLevel = 5;

        [Header("Unlocking")]
        public CarUnlockOption unlockOptions;
        [Min(0)] public int coinPrice;
        [Min(0)] public int silverPrice;
        [Min(0)] public int diamondPrice;

        public bool SupportsUnlockOption(CarUnlockOption option)
        {
            return option != CarUnlockOption.None && (unlockOptions & option) == option;
        }

        public int GetUnlockPrice(CarCurrencyType currencyType)
        {
            switch (currencyType)
            {
                case CarCurrencyType.Coins:
                    return coinPrice;
                case CarCurrencyType.Silver:
                    return silverPrice;
                case CarCurrencyType.Diamonds:
                    return diamondPrice;
                default:
                    return -1;
            }
        }

        public CarUnlockOption GetUnlockOption(CarCurrencyType currencyType)
        {
            switch (currencyType)
            {
                case CarCurrencyType.Coins:
                    return CarUnlockOption.UnlockWithCoins;
                case CarCurrencyType.Silver:
                    return CarUnlockOption.UnlockWithSilver;
                case CarCurrencyType.Diamonds:
                    return CarUnlockOption.UnlockWithDiamonds;
                default:
                    return CarUnlockOption.None;
            }
        }
    }
}
