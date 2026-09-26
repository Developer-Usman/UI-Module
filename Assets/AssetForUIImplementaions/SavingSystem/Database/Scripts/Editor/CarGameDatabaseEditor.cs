#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RP_DataSavingSystem.Editor
{
    [CustomEditor(typeof(CarGameDatabase))]
    public sealed class CarGameDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Database"))
                ValidateDatabase((CarGameDatabase)target);
        }

        private static void ValidateDatabase(CarGameDatabase database)
        {
            if (database.cars == null)
            {
                Debug.LogError("Car database list is null.", database);
                return;
            }

            HashSet<string> ids = new HashSet<string>();
            bool valid = true;

            for (int i = 0; i < database.cars.Count; i++)
            {
                CarDefinition car = database.cars[i];
                if (car == null)
                {
                    Debug.LogError("Car database contains a null entry at index " + i + ".", database);
                    valid = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(car.carId))
                {
                    Debug.LogError("Car at index " + i + " has an empty ID.", car);
                    valid = false;
                    continue;
                }

                if (!ids.Add(car.carId))
                {
                    Debug.LogError("Duplicate car ID: " + car.carId, car);
                    valid = false;
                }
            }

            if (valid)
                Debug.Log("Car database validation passed.", database);
        }
    }
}
#endif
