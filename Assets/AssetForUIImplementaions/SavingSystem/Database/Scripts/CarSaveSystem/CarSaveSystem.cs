using System;
using System.Collections.Generic;
using UnityEngine;

namespace RP_DataSavingSystem
{
    [DefaultExecutionOrder(-100)]
    public sealed class CarSaveSystem : MonoBehaviour
    {
        public const int CurrentSaveVersion = 1;
        private const string LegacyDinoSaveKey = "DinoSave";

        public static CarSaveSystem Instance { get; private set; }

        [Header("Database")]
        [SerializeField] private CarGameDatabase database;

        [Header("Save")]
        [SerializeField] private string saveKey = "CarSave";

        public CarSaveData SaveData { get; private set; }
        public CarGameDatabase Database => database;

        public event Action OnCurrencyChanged;
        public event Action OnSelectedCarChanged;
        public event Action OnCarStateChanged;

        private readonly Dictionary<string, CarOwnedState> carStateLookup =
            new Dictionary<string, CarOwnedState>();

        private bool isDirty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (database != null)
                database.BuildLookup();

            DeleteLegacyDinoSave();
            LoadOrCreate();
        }

        public void LoadOrCreate()
        {
            bool needsSave;

            if (ES3.KeyExists(saveKey))
            {
                SaveData = ES3.Load<CarSaveData>(saveKey);
                needsSave = MigrateSaveIfNeeded();
            }
            else
            {
                SaveData = CreateDefaultSave();
                needsSave = true;
            }

            needsSave |= ValidateAndRepair();

            if (needsSave)
                SaveNow();
        }

        public void ResetToDefaultSave()
        {
            SaveData = CreateDefaultSave();
            BuildStateLookup();
            SaveNow();
            NotifyAllChanged();
        }

        public void DeleteSaveFile()
        {
            if (ES3.KeyExists(saveKey))
                ES3.DeleteKey(saveKey);

            SaveData = CreateDefaultSave();
            BuildStateLookup();
            MarkDirty();
            NotifyAllChanged();
        }

        public void ReloadSave()
        {
            LoadOrCreate();
            NotifyAllChanged();
        }

        public void ValidateSaveData()
        {
            ValidateAndRepair();
            MarkDirty();
        }

        public int GetUnlockedCarCount()
        {
            if (SaveData == null || SaveData.collection == null || SaveData.collection.cars == null)
                return 0;

            int count = 0;
            List<CarOwnedState> cars = SaveData.collection.cars;

            for (int i = 0; i < cars.Count; i++)
            {
                CarOwnedState state = cars[i];
                if (state != null && state.unlocked)
                    count++;
            }

            return count;
        }

        public void UnlockAllCars()
        {
            if (SaveData == null || SaveData.collection == null || SaveData.collection.cars == null)
                return;

            bool changed = false;
            List<CarOwnedState> cars = SaveData.collection.cars;

            for (int i = 0; i < cars.Count; i++)
            {
                CarOwnedState state = cars[i];
                if (state == null || state.unlocked)
                    continue;

                state.unlocked = true;
                changed = true;
            }

            if (!changed)
                return;

            bool selectionChanged = RepairSelectedCar();
            MarkDirty();

            if (selectionChanged)
                NotifySelectedCarChanged();

            NotifyCarStateChanged();
        }

        public void LockAllCars()
        {
            if (SaveData == null || SaveData.collection == null || SaveData.collection.cars == null)
                return;

            bool changed = false;
            List<CarOwnedState> cars = SaveData.collection.cars;

            for (int i = 0; i < cars.Count; i++)
            {
                CarOwnedState state = cars[i];
                if (state == null || !state.unlocked)
                    continue;

                state.unlocked = false;
                changed = true;
            }

            if (!changed)
                return;

            RepairSelectedCar();
            MarkDirty();
            NotifySelectedCarChanged();
            NotifyCarStateChanged();
        }

        public bool UnlockCar(string carId)
        {
            CarOwnedState state = FindCarState(carId);
            if (state == null)
                return false;

            if (state.unlocked)
                return true;

            state.unlocked = true;
            bool selectionChanged = RepairSelectedCar();
            MarkDirty();

            if (selectionChanged)
                NotifySelectedCarChanged();

            NotifyCarStateChanged();
            return true;
        }

        public bool TryUnlockCarWithCurrency(string carId, CarCurrencyType currencyType)
        {
            CarOwnedState state = FindCarState(carId);
            if (state == null)
                return false;

            if (state.unlocked)
                return true;

            CarDefinition definition = database != null ? database.GetCarDefinition(carId) : null;
            if (definition == null)
                return false;

            CarUnlockOption option = definition.GetUnlockOption(currencyType);
            if (!definition.SupportsUnlockOption(option))
                return false;

            int price = definition.GetUnlockPrice(currencyType);
            if (price < 0 || !SpendCurrency(currencyType, price))
                return false;

            return UnlockCar(carId);
        }

        public bool LockCar(string carId)
        {
            CarOwnedState state = FindCarState(carId);
            if (state == null)
                return false;

            if (!state.unlocked)
                return true;

            state.unlocked = false;
            bool selectionChanged = RepairSelectedCar();
            MarkDirty();

            if (selectionChanged)
                NotifySelectedCarChanged();

            NotifyCarStateChanged();
            return true;
        }

        public bool ResetCarProgress(string carId)
        {
            CarOwnedState state = FindCarState(carId);
            CarDefinition definition = database != null ? database.GetCarDefinition(carId) : null;
            if (state == null || definition == null)
                return false;

            state.unlocked = definition.initiallyOwned;
            state.upgradeLevel = 0;
            bool selectionChanged = RepairSelectedCar();
            MarkDirty();

            if (selectionChanged)
                NotifySelectedCarChanged();

            NotifyCarStateChanged();
            return true;
        }

        public void ResetCarCollection()
        {
            if (SaveData == null)
                return;

            CarSaveData defaults = CreateDefaultSave();
            SaveData.collection = defaults.collection;
            SaveData.playerProfile.selectedCarId = defaults.playerProfile.selectedCarId;
            BuildStateLookup();
            MarkDirty();
            NotifySelectedCarChanged();
            NotifyCarStateChanged();
        }

        public bool SetCarUpgradeLevel(string carId, int level)
        {
            CarOwnedState state = FindCarState(carId);
            CarDefinition definition = database != null ? database.GetCarDefinition(carId) : null;

            if (state == null || definition == null || !state.unlocked)
                return false;

            int clampedLevel = Mathf.Clamp(level, 0, Mathf.Max(0, definition.maxUpgradeLevel));
            if (state.upgradeLevel == clampedLevel)
                return true;

            state.upgradeLevel = clampedLevel;
            MarkDirty();
            NotifyCarStateChanged();
            return true;
        }

        public bool SetSelectedCar(string carId)
        {
            CarOwnedState state = FindCarState(carId);
            if (state == null || !state.unlocked || database == null || database.GetCarDefinition(carId) == null)
                return false;

            if (SaveData.playerProfile.selectedCarId == carId)
                return true;

            SaveData.playerProfile.selectedCarId = carId;
            MarkDirty();
            NotifySelectedCarChanged();
            return true;
        }

        public void SetSelectedCarForDebug(string carId)
        {
            SetSelectedCar(carId);
        }

        public CarOwnedState FindCarState(string carId)
        {
            if (string.IsNullOrEmpty(carId))
                return null;

            return carStateLookup.TryGetValue(carId, out CarOwnedState state) ? state : null;
        }

        public int GetBalance(CarCurrencyType type)
        {
            CarCurrencyBalance balance = FindBalance(type);
            return balance != null ? balance.amount : 0;
        }

        public void AddCurrency(CarCurrencyType type, int amount)
        {
            if (amount <= 0)
                return;

            CarCurrencyBalance balance = GetOrCreateBalance(type);
            if (balance == null)
                return;

            balance.amount = amount > int.MaxValue - balance.amount
                ? int.MaxValue
                : balance.amount + amount;

            MarkDirty();
            NotifyCurrencyChanged();
        }

        public void SetCurrency(CarCurrencyType type, int amount)
        {
            CarCurrencyBalance balance = GetOrCreateBalance(type);
            if (balance == null)
                return;

            int clampedAmount = Mathf.Max(0, amount);
            if (balance.amount == clampedAmount)
                return;

            balance.amount = clampedAmount;
            MarkDirty();
            NotifyCurrencyChanged();
        }

        public void ClearWallet()
        {
            if (SaveData == null || SaveData.wallet == null || SaveData.wallet.balances == null)
                return;

            bool changed = false;
            List<CarCurrencyBalance> balances = SaveData.wallet.balances;

            for (int i = 0; i < balances.Count; i++)
            {
                CarCurrencyBalance balance = balances[i];
                if (balance == null || balance.amount == 0)
                    continue;

                balance.amount = 0;
                changed = true;
            }

            if (!changed)
                return;

            MarkDirty();
            NotifyCurrencyChanged();
        }

        public void SetAllOwnedCarUpgradeLevels(int level)
        {
            if (database == null || SaveData == null || SaveData.collection == null || SaveData.collection.cars == null)
                return;

            bool changed = false;
            List<CarOwnedState> cars = SaveData.collection.cars;

            for (int i = 0; i < cars.Count; i++)
            {
                CarOwnedState state = cars[i];
                if (state == null || !state.unlocked)
                    continue;

                CarDefinition definition = database.GetCarDefinition(state.carId);
                if (definition == null)
                    continue;

                int clampedLevel = Mathf.Clamp(level, 0, Mathf.Max(0, definition.maxUpgradeLevel));
                if (state.upgradeLevel == clampedLevel)
                    continue;

                state.upgradeLevel = clampedLevel;
                changed = true;
            }

            if (!changed)
                return;

            MarkDirty();
            NotifyCarStateChanged();
        }

        public void MaxUpgradeAllOwnedCars()
        {
            SetAllOwnedCarUpgradeLevels(int.MaxValue);
        }

        public bool SpendCurrency(CarCurrencyType type, int amount)
        {
            if (amount < 0)
                return false;

            CarCurrencyBalance balance = FindBalance(type);
            if (balance == null || balance.amount < amount)
                return false;

            if (amount == 0)
                return true;

            balance.amount -= amount;
            MarkDirty();
            NotifyCurrencyChanged();
            return true;
        }

        public bool HasEnoughCurrency(CarCurrencyType type, int amount)
        {
            return amount >= 0 && GetBalance(type) >= amount;
        }

        public void MarkDirty()
        {
            isDirty = true;
        }

        public void SaveNow()
        {
            if (SaveData == null || string.IsNullOrEmpty(saveKey))
                return;

            SaveData.saveVersion = CurrentSaveVersion;
            ES3.Save(saveKey, SaveData);
            isDirty = false;
        }

        public void NotifyCurrencyChanged()
        {
            OnCurrencyChanged?.Invoke();
        }

        public void NotifySelectedCarChanged()
        {
            OnSelectedCarChanged?.Invoke();
        }

        public void NotifyCarStateChanged()
        {
            OnCarStateChanged?.Invoke();
        }

        private CarSaveData CreateDefaultSave()
        {
            CarSaveData data = new CarSaveData();

            if (database == null || database.cars == null)
                return data;

            for (int i = 0; i < database.cars.Count; i++)
            {
                CarDefinition definition = database.cars[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.carId))
                    continue;

                if (database.GetCarDefinition(definition.carId) != definition)
                    continue;

                CarOwnedState state = new CarOwnedState(definition.carId, definition.initiallyOwned);
                data.collection.cars.Add(state);

                if (definition.initiallyOwned && string.IsNullOrEmpty(data.playerProfile.selectedCarId))
                    data.playerProfile.selectedCarId = definition.carId;
            }

            return data;
        }

        private bool MigrateSaveIfNeeded()
        {
            if (SaveData == null)
                return false;

            bool migrated = SaveData.saveVersion < CurrentSaveVersion;
            SaveData.saveVersion = CurrentSaveVersion;
            return migrated;
        }

        private bool ValidateAndRepair()
        {
            bool changed = false;

            if (SaveData == null)
            {
                SaveData = CreateDefaultSave();
                BuildStateLookup();
                return true;
            }

            if (SaveData.playerProfile == null)
            {
                SaveData.playerProfile = new CarPlayerProfileSave();
                changed = true;
            }

            if (SaveData.wallet == null)
            {
                SaveData.wallet = new CarWalletSave();
                changed = true;
            }

            if (SaveData.settings == null)
            {
                SaveData.settings = new CarSettingsSave();
                changed = true;
            }

            if (SaveData.collection == null)
            {
                SaveData.collection = new CarCollectionSave();
                changed = true;
            }

            if (SaveData.collection.cars == null)
            {
                SaveData.collection.cars = new List<CarOwnedState>();
                changed = true;
            }

            if (SaveData.wallet.balances == null)
            {
                SaveData.wallet.balances = new List<CarCurrencyBalance>();
                changed = true;
            }

            changed |= EnsureCurrencyEntry(CarCurrencyType.Coins);
            changed |= EnsureCurrencyEntry(CarCurrencyType.Silver);
            changed |= EnsureCurrencyEntry(CarCurrencyType.Diamonds);

            BuildStateLookup();
            changed |= SyncMissingDatabaseEntries();
            changed |= RepairCarStates();
            changed |= RepairSelectedCar();
            return changed;
        }

        private bool EnsureCurrencyEntry(CarCurrencyType type)
        {
            List<CarCurrencyBalance> balances = SaveData.wallet.balances;

            for (int i = 0; i < balances.Count; i++)
            {
                CarCurrencyBalance balance = balances[i];
                if (balance == null || balance.type != type)
                    continue;

                if (balance.amount >= 0)
                    return false;

                balance.amount = 0;
                return true;
            }

            balances.Add(new CarCurrencyBalance(type));
            return true;
        }

        private void BuildStateLookup()
        {
            carStateLookup.Clear();

            if (SaveData == null || SaveData.collection == null || SaveData.collection.cars == null)
                return;

            List<CarOwnedState> cars = SaveData.collection.cars;
            for (int i = 0; i < cars.Count; i++)
            {
                CarOwnedState state = cars[i];
                if (state == null || string.IsNullOrEmpty(state.carId) || carStateLookup.ContainsKey(state.carId))
                    continue;

                carStateLookup.Add(state.carId, state);
            }
        }

        private bool SyncMissingDatabaseEntries()
        {
            if (database == null || database.cars == null)
                return false;

            bool changed = false;

            for (int i = 0; i < database.cars.Count; i++)
            {
                CarDefinition definition = database.cars[i];
                if (definition == null || string.IsNullOrEmpty(definition.carId) || carStateLookup.ContainsKey(definition.carId))
                    continue;

                CarOwnedState state = new CarOwnedState(definition.carId, definition.initiallyOwned);
                SaveData.collection.cars.Add(state);
                carStateLookup.Add(definition.carId, state);
                changed = true;
            }

            return changed;
        }

        private bool RepairCarStates()
        {
            if (database == null)
                return false;

            bool changed = false;
            List<CarOwnedState> cars = SaveData.collection.cars;

            for (int i = 0; i < cars.Count; i++)
            {
                CarOwnedState state = cars[i];
                if (state == null || string.IsNullOrEmpty(state.carId))
                    continue;

                CarDefinition definition = database.GetCarDefinition(state.carId);
                if (definition == null)
                    continue;

                int clampedLevel = Mathf.Clamp(state.upgradeLevel, 0, Mathf.Max(0, definition.maxUpgradeLevel));
                if (state.upgradeLevel == clampedLevel)
                    continue;

                state.upgradeLevel = clampedLevel;
                changed = true;
            }

            return changed;
        }

        private bool RepairSelectedCar()
        {
            string selectedCarId = SaveData.playerProfile.selectedCarId;
            CarOwnedState selectedState = FindCarState(selectedCarId);

            if (selectedState != null && selectedState.unlocked &&
                database != null && database.GetCarDefinition(selectedCarId) != null)
            {
                return false;
            }

            string replacementId = string.Empty;

            if (database != null && database.cars != null)
            {
                for (int i = 0; i < database.cars.Count; i++)
                {
                    CarDefinition definition = database.cars[i];
                    if (definition == null || string.IsNullOrEmpty(definition.carId))
                        continue;

                    CarOwnedState state = FindCarState(definition.carId);
                    if (state != null && state.unlocked)
                    {
                        replacementId = definition.carId;
                        break;
                    }
                }
            }

            if (selectedCarId == replacementId)
                return false;

            SaveData.playerProfile.selectedCarId = replacementId;
            return true;
        }

        private CarCurrencyBalance FindBalance(CarCurrencyType type)
        {
            if (SaveData == null || SaveData.wallet == null || SaveData.wallet.balances == null)
                return null;

            List<CarCurrencyBalance> balances = SaveData.wallet.balances;
            for (int i = 0; i < balances.Count; i++)
            {
                CarCurrencyBalance balance = balances[i];
                if (balance != null && balance.type == type)
                    return balance;
            }

            return null;
        }

        private CarCurrencyBalance GetOrCreateBalance(CarCurrencyType type)
        {
            if (SaveData == null || SaveData.wallet == null)
                return null;

            if (SaveData.wallet.balances == null)
                SaveData.wallet.balances = new List<CarCurrencyBalance>();

            CarCurrencyBalance balance = FindBalance(type);
            if (balance != null)
                return balance;

            balance = new CarCurrencyBalance(type);
            SaveData.wallet.balances.Add(balance);
            return balance;
        }

        private void DeleteLegacyDinoSave()
        {
            if (ES3.KeyExists(LegacyDinoSaveKey))
                ES3.DeleteKey(LegacyDinoSaveKey);
        }

        private void NotifyAllChanged()
        {
            NotifyCurrencyChanged();
            NotifySelectedCarChanged();
            NotifyCarStateChanged();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause && isDirty)
                SaveNow();
        }

        private void OnApplicationQuit()
        {
            if (isDirty)
                SaveNow();
        }
    }
}
