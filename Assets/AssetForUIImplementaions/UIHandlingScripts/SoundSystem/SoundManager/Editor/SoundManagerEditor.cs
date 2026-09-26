#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundManager))]
[CanEditMultipleObjects]
public class SoundManagerEditor : Editor
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

            if (property.name == "saveFileName")
                continue;

            EditorGUILayout.PropertyField(property, true);
        }

        DrawSaveFileSection();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSaveFileSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Save File", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("saveFileName"), new GUIContent("File Name"));

        SoundManager manager = (SoundManager)target;
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.TextField("Save Path", manager.SaveDirectory);

        bool exists = File.Exists(manager.SaveFilePath);
        EditorGUILayout.HelpBox(
            exists ? "JSON save file exists on disk." : "No JSON save file found yet.",
            exists ? MessageType.Info : MessageType.None);

        EditorGUILayout.Space(6f);

        using (new EditorGUI.DisabledScope(!exists && !Application.isPlaying))
        {
            if (!GUILayout.Button("Clear / Reset Save", GUILayout.Height(28f)))
                return;
        }

        if (!EditorUtility.DisplayDialog(
                "Clear Sound Settings",
                $"Delete '{manager.SaveFilePath}' and reset sound settings to defaults?",
                "Clear",
                "Cancel"))
            return;

        foreach (Object obj in targets)
        {
            SoundManager soundManager = (SoundManager)obj;
            Undo.RecordObject(soundManager, "Clear Sound Settings");
            soundManager.ClearSavedSettings();
            EditorUtility.SetDirty(soundManager);
        }

        GUI.FocusControl(null);
        Repaint();
    }
}
#endif
