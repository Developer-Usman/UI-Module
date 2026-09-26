#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RP_DataSavingSystem.Editor
{
    [CustomEditor(typeof(CarSaveSystem))]
    public sealed class CarSaveSystemEditor : UnityEditor.Editor
    {
        private const int MaxDebugCurrency = 999999;

        private bool showSaveSummary = true;
        private bool showCurrencyTools = true;
        private bool showCarTools = true;
        private bool showSettings;
        private bool showAllCars;

        private CarCurrencyType currencyType;
        private int currencyAmount = 1000;
        private int selectedCarIndex;
        private int upgradeLevel;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CarSaveSystem saveSystem = (CarSaveSystem)target;

            EditorGUILayout.Space(10f);
            DrawSaveControls(saveSystem);

            if (saveSystem.SaveData == null)
            {
                EditorGUILayout.HelpBox(
                    Application.isPlaying
                        ? "SaveData is not initialized. Use Load / Create Save."
                        : "Enter Play Mode or use Load / Create Save to inspect editor save data.",
                    MessageType.Info);
                return;
            }

            DrawCurrencyTools(saveSystem);
            DrawCarTools(saveSystem);
            DrawSettings(saveSystem);
            DrawDebugView(saveSystem);

            if (GUI.changed)
                EditorUtility.SetDirty(saveSystem);
        }

        private void DrawSaveControls(CarSaveSystem saveSystem)
        {
            EditorGUILayout.LabelField("Save Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load / Create Save"))
                RunAction(saveSystem.LoadOrCreate);

            using (new EditorGUI.DisabledScope(saveSystem.SaveData == null))
            {
                if (GUILayout.Button("Save Now"))
                    RunAction(saveSystem.SaveNow);

                if (GUILayout.Button("Validate & Repair"))
                    RunAction(saveSystem.ValidateSaveData);
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(saveSystem.SaveData == null))
            {
                if (GUILayout.Button("Reload Save From Disk"))
                    RunAction(saveSystem.ReloadSave);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Full Save To Defaults") &&
                EditorUtility.DisplayDialog("Reset Full Save", "Reset wallet, settings, ownership, selection, and upgrades?", "Reset", "Cancel"))
            {
                RunAction(saveSystem.ResetToDefaultSave);
            }

            if (GUILayout.Button("Delete Save Key") &&
                EditorUtility.DisplayDialog("Delete Save", "Delete the CarSave key and recreate default in-memory data?", "Delete", "Cancel"))
            {
                RunAction(saveSystem.DeleteSaveFile);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCurrencyTools(CarSaveSystem saveSystem)
        {
            EditorGUILayout.Space(10f);
            showCurrencyTools = EditorGUILayout.Foldout(showCurrencyTools, "Wallet Actions", true);
            if (!showCurrencyTools)
                return;

            EditorGUI.indentLevel++;
            currencyType = (CarCurrencyType)EditorGUILayout.EnumPopup("Currency", currencyType);
            currencyAmount = Mathf.Max(0, EditorGUILayout.IntField("Amount", currencyAmount));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
                RunAction(() => saveSystem.AddCurrency(currencyType, currencyAmount));
            if (GUILayout.Button("Spend"))
                RunAction(() => saveSystem.SpendCurrency(currencyType, currencyAmount));
            if (GUILayout.Button("Set"))
                RunAction(() => saveSystem.SetCurrency(currencyType, currencyAmount));
            if (GUILayout.Button("Set 0"))
                RunAction(() => saveSystem.SetCurrency(currencyType, 0));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+1000 Coins"))
                RunAction(() => saveSystem.AddCurrency(CarCurrencyType.Coins, 1000));
            if (GUILayout.Button("+100 Silver"))
                RunAction(() => saveSystem.AddCurrency(CarCurrencyType.Silver, 100));
            if (GUILayout.Button("+25 Diamonds"))
                RunAction(() => saveSystem.AddCurrency(CarCurrencyType.Diamonds, 25));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Max All"))
            {
                saveSystem.SetCurrency(CarCurrencyType.Coins, MaxDebugCurrency);
                saveSystem.SetCurrency(CarCurrencyType.Silver, MaxDebugCurrency);
                saveSystem.SetCurrency(CarCurrencyType.Diamonds, MaxDebugCurrency);
                Repaint();
            }

            if (GUILayout.Button("Clear Wallet") &&
                EditorUtility.DisplayDialog("Clear Wallet", "Set every currency balance to zero?", "Clear", "Cancel"))
            {
                RunAction(saveSystem.ClearWallet);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        private void DrawCarTools(CarSaveSystem saveSystem)
        {
            EditorGUILayout.Space(10f);
            showCarTools = EditorGUILayout.Foldout(showCarTools, "Car Actions", true);
            if (!showCarTools)
                return;

            CarGameDatabase database = saveSystem.Database;
            List<CarDefinition> cars = database != null ? database.cars : null;

            EditorGUI.indentLevel++;
            if (cars == null || cars.Count == 0)
            {
                EditorGUILayout.HelpBox("The car database has no entries.", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            string[] carNames = new string[cars.Count];
            for (int i = 0; i < cars.Count; i++)
            {
                CarDefinition car = cars[i];
                carNames[i] = car != null
                    ? car.displayName + " (" + car.carId + ")"
                    : "Missing Car (Index " + i + ")";
            }

            selectedCarIndex = Mathf.Clamp(selectedCarIndex, 0, cars.Count - 1);
            selectedCarIndex = EditorGUILayout.Popup("Car", selectedCarIndex, carNames);

            CarDefinition selectedCar = cars[selectedCarIndex];
            if (selectedCar == null || string.IsNullOrEmpty(selectedCar.carId))
            {
                EditorGUILayout.HelpBox("Selected database entry is invalid.", MessageType.Error);
                EditorGUI.indentLevel--;
                return;
            }

            CarOwnedState state = saveSystem.FindCarState(selectedCar.carId);
            if (state == null)
            {
                EditorGUILayout.HelpBox("No save state found. Run Validate & Repair.", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.LabelField("Owned", state.unlocked.ToString());
            EditorGUILayout.LabelField("Upgrade", state.upgradeLevel + " / " + selectedCar.maxUpgradeLevel);
            EditorGUILayout.LabelField("Unlock Options", selectedCar.unlockOptions.ToString());

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock"))
                RunAction(() => saveSystem.UnlockCar(selectedCar.carId));
            if (GUILayout.Button("Lock"))
                RunAction(() => saveSystem.LockCar(selectedCar.carId));
            if (GUILayout.Button("Select"))
                RunAction(() => saveSystem.SetSelectedCar(selectedCar.carId));
            if (GUILayout.Button("Reset Car"))
                RunAction(() => saveSystem.ResetCarProgress(selectedCar.carId));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Buy With Coins"))
                RunAction(() => saveSystem.TryUnlockCarWithCurrency(selectedCar.carId, CarCurrencyType.Coins));
            if (GUILayout.Button("Buy With Silver"))
                RunAction(() => saveSystem.TryUnlockCarWithCurrency(selectedCar.carId, CarCurrencyType.Silver));
            if (GUILayout.Button("Buy With Diamonds"))
                RunAction(() => saveSystem.TryUnlockCarWithCurrency(selectedCar.carId, CarCurrencyType.Diamonds));
            EditorGUILayout.EndHorizontal();

            upgradeLevel = Mathf.Clamp(EditorGUILayout.IntField("New Upgrade Level", upgradeLevel), 0, selectedCar.maxUpgradeLevel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Upgrade"))
                RunAction(() => saveSystem.SetCarUpgradeLevel(selectedCar.carId, upgradeLevel));
            if (GUILayout.Button("Set Level 0"))
                RunAction(() => saveSystem.SetCarUpgradeLevel(selectedCar.carId, 0));
            if (GUILayout.Button("Set Max Level"))
                RunAction(() => saveSystem.SetCarUpgradeLevel(selectedCar.carId, selectedCar.maxUpgradeLevel));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock All Cars"))
                RunAction(saveSystem.UnlockAllCars);
            if (GUILayout.Button("Max Upgrade Owned"))
                RunAction(saveSystem.MaxUpgradeAllOwnedCars);
            if (GUILayout.Button("Reset Owned Upgrades"))
                RunAction(() => saveSystem.SetAllOwnedCarUpgradeLevels(0));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Lock All Cars") &&
                EditorUtility.DisplayDialog("Lock All Cars", "Lock every car and clear the selected car?", "Lock All", "Cancel"))
            {
                RunAction(saveSystem.LockAllCars);
            }

            if (GUILayout.Button("Reset Car Collection") &&
                EditorUtility.DisplayDialog("Reset Car Collection", "Reset ownership, selection, and upgrades without changing wallet or settings?", "Reset", "Cancel"))
            {
                RunAction(saveSystem.ResetCarCollection);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        private void DrawSettings(CarSaveSystem saveSystem)
        {
            EditorGUILayout.Space(10f);
            showSettings = EditorGUILayout.Foldout(showSettings, "Saved Settings", true);
            if (!showSettings || saveSystem.SaveData.settings == null)
                return;

            CarSettingsSave settings = saveSystem.SaveData.settings;
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            int frameRate = Mathf.Max(1, EditorGUILayout.IntField("Frame Rate", settings.frameRate));
            float masterVolume = EditorGUILayout.Slider("Master Volume", settings.masterVolume, 0f, 1f);
            float musicVolume = EditorGUILayout.Slider("Music Volume", settings.musicVolume, 0f, 1f);
            float sensitivity = Mathf.Max(0f, EditorGUILayout.FloatField("Sensitivity", settings.sensitivity));

            if (EditorGUI.EndChangeCheck())
            {
                settings.frameRate = frameRate;
                settings.masterVolume = masterVolume;
                settings.musicVolume = musicVolume;
                settings.sensitivity = sensitivity;
                saveSystem.MarkDirty();
            }

            if (GUILayout.Button("Reset Settings To Defaults"))
            {
                saveSystem.SaveData.settings = new CarSettingsSave();
                saveSystem.MarkDirty();
                Repaint();
            }
            EditorGUI.indentLevel--;
        }

        private void DrawDebugView(CarSaveSystem saveSystem)
        {
            CarSaveData data = saveSystem.SaveData;

            EditorGUILayout.Space(10f);
            showSaveSummary = EditorGUILayout.Foldout(showSaveSummary, "Save Summary", true);
            if (showSaveSummary)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Save Version", data.saveVersion.ToString());
                EditorGUILayout.LabelField("Selected Car", data.playerProfile.selectedCarId);
                EditorGUILayout.LabelField("Owned Cars", saveSystem.GetUnlockedCarCount().ToString());
                EditorGUILayout.LabelField("Total Save Entries", data.collection.cars.Count.ToString());
                EditorGUILayout.LabelField("Coins", saveSystem.GetBalance(CarCurrencyType.Coins).ToString());
                EditorGUILayout.LabelField("Silver", saveSystem.GetBalance(CarCurrencyType.Silver).ToString());
                EditorGUILayout.LabelField("Diamonds", saveSystem.GetBalance(CarCurrencyType.Diamonds).ToString());
                EditorGUI.indentLevel--;
            }

            showAllCars = EditorGUILayout.Foldout(showAllCars, "All Car States", true);
            if (!showAllCars)
                return;

            EditorGUI.indentLevel++;
            List<CarOwnedState> cars = data.collection.cars;
            for (int i = 0; i < cars.Count; i++)
            {
                CarOwnedState state = cars[i];
                if (state == null)
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Car ID", state.carId);
                EditorGUILayout.LabelField("Owned", state.unlocked.ToString());
                EditorGUILayout.LabelField("Upgrade Level", state.upgradeLevel.ToString());
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;
        }

        private void RunAction(System.Action action)
        {
            action?.Invoke();
            Repaint();
        }
    }
}
#endif
