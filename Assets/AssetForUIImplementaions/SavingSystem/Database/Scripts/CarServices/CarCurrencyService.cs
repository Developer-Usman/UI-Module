using UnityEngine;

namespace RP_DataSavingSystem
{
    public sealed class CarCurrencyService : MonoBehaviour
    {
        public static CarCurrencyService Instance { get; private set; }

        private CarSaveSystem saveSystem;

        private void Awake()
        {
            Instance = this;
            saveSystem = CarSaveSystem.Instance;
        }

        public int GetBalance(CarCurrencyType type)
        {
            return saveSystem != null ? saveSystem.GetBalance(type) : 0;
        }

        public void Add(CarCurrencyType type, int amount)
        {
            if (saveSystem != null)
                saveSystem.AddCurrency(type, amount);
        }

        public bool Spend(CarCurrencyType type, int amount)
        {
            return saveSystem != null && saveSystem.SpendCurrency(type, amount);
        }

        public bool HasEnough(CarCurrencyType type, int amount)
        {
            return saveSystem != null && saveSystem.HasEnoughCurrency(type, amount);
        }
    }
}
