#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnButtonClick))]
[CanEditMultipleObjects]
public class OnButtonClickEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(property);
                continue;
            }

            EditorGUILayout.PropertyField(property, true);
        }

        DrawAssignButton();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAssignButton()
    {
        EditorGUILayout.Space(6f);

        if (!GUILayout.Button("Assign Button & Audio Source", GUILayout.Height(28f)))
            return;

        serializedObject.ApplyModifiedProperties();

        foreach (Object obj in targets)
        {
            OnButtonClick click = (OnButtonClick)obj;
            Undo.RecordObject(click, "Assign Button & Audio Source");
            click.AssignRefs();
            EditorUtility.SetDirty(click);
            PrefabUtility.RecordPrefabInstancePropertyModifications(click);
        }

        serializedObject.Update();
        GUI.FocusControl(null);
    }
}
#endif
