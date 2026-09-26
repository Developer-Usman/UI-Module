#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TabView))]
[CanEditMultipleObjects]
public class TabViewEditor : Editor
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

            if (property.name == "canvasGroup")
                DrawCanvasGroupInline();
        }

        DrawTabViewButtons();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCanvasGroupInline()
    {
        CanvasGroup sharedGroup = null;
        bool hasNull = false;
        bool mismatched = false;

        for (int i = 0; i < targets.Length; i++)
        {
            SerializedObject so = new SerializedObject(targets[i]);
            CanvasGroup group = so.FindProperty("canvasGroup").objectReferenceValue as CanvasGroup;

            if (group == null)
            {
                hasNull = true;
                continue;
            }

            if (sharedGroup == null)
            {
                sharedGroup = group;
                continue;
            }

            if (sharedGroup != group)
            {
                mismatched = true;
                break;
            }
        }

        if (mismatched)
        {
            EditorGUILayout.HelpBox(
                "Selected TabViews use different CanvasGroups. Inline editing disabled.",
                MessageType.Info);
            return;
        }

        if (sharedGroup == null)
        {
            if (hasNull)
                EditorGUILayout.HelpBox("Assign a Canvas Group to edit its properties.", MessageType.Info);
            return;
        }

        SerializedObject canvasGroupObject = new SerializedObject(sharedGroup);
        canvasGroupObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(canvasGroupObject.FindProperty("m_Alpha"), new GUIContent("Alpha"));
        EditorGUILayout.PropertyField(canvasGroupObject.FindProperty("m_Interactable"), new GUIContent("Interactable"));
        EditorGUILayout.PropertyField(canvasGroupObject.FindProperty("m_BlocksRaycasts"), new GUIContent("Block Raycasts"));
        EditorGUILayout.PropertyField(canvasGroupObject.FindProperty("m_IgnoreParentGroups"), new GUIContent("Ignore Parent Groups"));
        if (EditorGUI.EndChangeCheck())
        {
            canvasGroupObject.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(sharedGroup);
        }
    }

    private void DrawTabViewButtons()
    {
        EditorGUILayout.Space(6f);

        if (GUILayout.Button("Assign Canvas Group", GUILayout.Height(28f)))
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object obj in targets)
            {
                TabView tabView = (TabView)obj;
                Undo.RecordObject(tabView, "Assign Canvas Group");
                tabView.AssignCanvasGroup();
                EditorUtility.SetDirty(tabView);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tabView);
            }

            serializedObject.Update();
            GUI.FocusControl(null);
        }

        if (GUILayout.Button("Toggle View State", GUILayout.Height(28f)))
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object obj in targets)
            {
                TabView tabView = (TabView)obj;
                SerializedObject so = new SerializedObject(tabView);
                CanvasGroup group = so.FindProperty("canvasGroup").objectReferenceValue as CanvasGroup;
                if (group == null)
                    continue;

                Undo.RecordObject(group, "Toggle View State");
                Undo.RecordObject(tabView, "Toggle View State");
                tabView.ToggleViewState();
                EditorUtility.SetDirty(group);
                EditorUtility.SetDirty(tabView);
                PrefabUtility.RecordPrefabInstancePropertyModifications(group);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tabView);
            }

            serializedObject.Update();
            GUI.FocusControl(null);
            Repaint();
        }

        EditorGUILayout.Space(6f);
    }
}

[CustomEditor(typeof(TabController))]
[CanEditMultipleObjects]
public class TabControllerEditor : Editor
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

        DrawAssignAudioSourceButton();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAssignAudioSourceButton()
    {
        EditorGUILayout.Space(6f);

        if (GUILayout.Button("Assign Tabs Audio Source", GUILayout.Height(28f)))
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object obj in targets)
            {
                TabController tabController = (TabController)obj;
                Undo.RecordObject(tabController, "Assign Tabs Audio Source");
                tabController.AssignTabsAudioSource();
                EditorUtility.SetDirty(tabController);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tabController);
            }

            serializedObject.Update();
            GUI.FocusControl(null);
        }

        EditorGUILayout.Space(6f);
    }
}
#endif
