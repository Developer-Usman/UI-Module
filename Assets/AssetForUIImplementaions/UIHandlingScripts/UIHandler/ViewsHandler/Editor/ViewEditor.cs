#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(View))]
[CanEditMultipleObjects]
public class ViewEditor : Editor
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

        DrawViewButtons();
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
                "Selected Views use different CanvasGroups. Inline editing disabled.",
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

    private void DrawViewButtons()
    {
        EditorGUILayout.Space(6f);

        if (GUILayout.Button("Assign View ID", GUILayout.Height(28f)))
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object obj in targets)
            {
                View view = (View)obj;
                Undo.RecordObject(view, "Assign View ID");
                view.AssignViewIDFromName();
                EditorUtility.SetDirty(view);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            }

            serializedObject.Update();
            GUI.FocusControl(null);
        }

        if (GUILayout.Button("Assign Canvas Group", GUILayout.Height(28f)))
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object obj in targets)
            {
                View view = (View)obj;
                Undo.RecordObject(view, "Assign Canvas Group");
                view.AssignCanvasGroup();
                EditorUtility.SetDirty(view);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            }

            serializedObject.Update();
            GUI.FocusControl(null);
        }

        if (GUILayout.Button("Toggle View State", GUILayout.Height(28f)))
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object obj in targets)
            {
                View view = (View)obj;
                SerializedObject so = new SerializedObject(view);
                CanvasGroup group = so.FindProperty("canvasGroup").objectReferenceValue as CanvasGroup;
                if (group == null)
                    continue;

                Undo.RecordObject(group, "Toggle View State");
                Undo.RecordObject(view, "Toggle View State");
                view.ToggleViewState();
                EditorUtility.SetDirty(group);
                EditorUtility.SetDirty(view);
                PrefabUtility.RecordPrefabInstancePropertyModifications(group);
                PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            }

            serializedObject.Update();
            GUI.FocusControl(null);
            Repaint();
        }

        EditorGUILayout.Space(6f);
    }
}

[CustomEditor(typeof(ViewHandler))]
[CanEditMultipleObjects]
public class ViewHandlerEditor : Editor
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

        DrawHandlerButtons();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHandlerButtons()
    {
        EditorGUILayout.Space(6f);

        if (GUILayout.Button("Assign Views", GUILayout.Height(28f)))
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object obj in targets)
            {
                ViewHandler handler = (ViewHandler)obj;
                Undo.RecordObject(handler, "Assign Views");
                handler.AssignViews();
                EditorUtility.SetDirty(handler);
                PrefabUtility.RecordPrefabInstancePropertyModifications(handler);
            }

            serializedObject.Update();
            GUI.FocusControl(null);
        }

        if (GUILayout.Button("Clear Views", GUILayout.Height(28f)))
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object obj in targets)
            {
                ViewHandler handler = (ViewHandler)obj;
                Undo.RecordObject(handler, "Clear Views");
                handler.ClearViews();
                EditorUtility.SetDirty(handler);
                PrefabUtility.RecordPrefabInstancePropertyModifications(handler);
            }

            serializedObject.Update();
            GUI.FocusControl(null);
        }

        EditorGUILayout.Space(6f);
    }
}
#endif
