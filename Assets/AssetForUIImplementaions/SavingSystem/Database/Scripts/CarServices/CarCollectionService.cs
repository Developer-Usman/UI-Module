using UnityEngine;

namespace RP_DataSavingSystem
{
    public sealed class CarCollectionService : MonoBehaviour
    {
        public static CarCollectionService Instance { get; private set; }

        private CarSaveSystem saveSystem;

        private void Awake()
        {
            Instance = this;
            saveSystem = CarSaveSystem.Instance;
        }

        public bool UnlockCar(string carId)
        {
            return saveSystem != null && saveSystem.UnlockCar(carId);
        }

        public bool TryUnlockWithCurrency(string carId, CarCurrencyType currencyType)
        {
            return saveSystem != null && saveSystem.TryUnlockCarWithCurrency(carId, currencyType);
        }

        public bool SetSelectedCar(string carId)
        {
            return saveSystem != null && saveSystem.SetSelectedCar(carId);
        }

        public bool SetUpgradeLevel(string carId, int level)
        {
            return saveSystem != null && saveSystem.SetCarUpgradeLevel(carId, level);
        }
    }
}
