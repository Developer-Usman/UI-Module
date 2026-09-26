#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RP_DataSavingSystem.Editor
{
    [CustomEditor(typeof(CarDefinition))]
    public sealed class CarDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CarDefinition car = (CarDefinition)target;

            if (string.IsNullOrWhiteSpace(car.carId))
                EditorGUILayout.HelpBox("Car ID is required.", MessageType.Error);

            if (car.year < 0 || car.maxUpgradeLevel < 0 || car.coinPrice < 0 ||
                car.silverPrice < 0 || car.diamondPrice < 0)
            {
                EditorGUILayout.HelpBox("Year, prices, and max upgrade level cannot be negative.", MessageType.Error);
            }

            if (car.topSpeed < 0f || car.power < 0f || car.zeroToHundred < 0f ||
                car.handling < 0f || car.scoring < 0f)
            {
                EditorGUILayout.HelpBox("Car stats cannot be negative.", MessageType.Error);
            }
        }
    }
}
#endif
