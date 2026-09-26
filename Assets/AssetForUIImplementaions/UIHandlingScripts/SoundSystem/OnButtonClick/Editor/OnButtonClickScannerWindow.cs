#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OnButtonClickScannerWindow : EditorWindow
{
    private enum FilterMode
    {
        AllButtons = 0,
        HasOnButtonClick = 1,
        MissingOnButtonClick = 2,
        MissingAudioSource = 3,
        MissingClickClip = 4,
        Ok = 5
    }

    private sealed class Entry
    {
        public Button button;
        public string path;
        public bool hasScript;
        public bool hasAudioSourceComponent;
        public bool audioSourceFieldAssigned;
        public bool buttonFieldAssigned;
        public bool hasClickClip;
        public string clipName;
        public bool selected;
    }

    private readonly List<Entry> entries = new List<Entry>(64);
    private readonly List<Entry> filtered = new List<Entry>(64);

    private Vector2 scroll;
    private FilterMode filter = FilterMode.AllButtons;
    private bool includeInactive = true;
    private string search = string.Empty;
    private AudioClip masterClip;
    private Transform scanRoot;

    private int countAll;
    private int countWithScript;
    private int countWithoutScript;
    private int countMissingAudio;
    private int countMissingClip;
    private int countOk;

    [MenuItem("Tools/Sound System/OnButtonClick Scanner")]
    public static void Open()
    {
        OnButtonClickScannerWindow window = GetWindow<OnButtonClickScannerWindow>("OnButtonClick Scanner");
        window.minSize = new Vector2(780f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        Scan();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawSummary();
        DrawBulkTools();
        DrawFilters();
        DrawList();
    }

    #region Toolbar

    private void DrawToolbar()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Scan Target", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            scanRoot = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Root (optional)", "Leave empty to scan all open scenes."),
                scanRoot,
                typeof(Transform),
                true);

            includeInactive = GUILayout.Toggle(includeInactive, "Include Inactive", GUILayout.Width(120f));

            if (GUILayout.Button("Scan", GUILayout.Width(80f), GUILayout.Height(20f)))
                Scan();
        }

        EditorGUILayout.Space(4f);
        DrawSceneInfo();
    }

    private void DrawSceneInfo()
    {
        StringBuilder sb = new StringBuilder(128);
        sb.Append("Open scenes: ");

        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            if (i > 0)
                sb.Append(" | ");

            sb.Append(scene.name);
        }

        EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
    }

    #endregion

    #region Summary / Filters / Bulk

    private void DrawSummary()
    {
        EditorGUILayout.LabelField(
            $"Buttons: {countAll}  |  With Script: {countWithScript}  |  Without: {countWithoutScript}  |  Missing AudioSource: {countMissingAudio}  |  Missing Clip: {countMissingClip}  |  OK: {countOk}",
            EditorStyles.miniLabel);
    }

    private void DrawBulkTools()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Bulk Tools", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            masterClip = (AudioClip)EditorGUILayout.ObjectField("Master Click Clip", masterClip, typeof(AudioClip), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Assign Clip → Selected"))
                    AssignClipToSelection(overwrite: true);

                if (GUILayout.Button("Assign Clip → All Missing"))
                    AssignClipToMissing();

                if (GUILayout.Button("Assign Clip → All With Script"))
                    AssignClipToAllWithScript();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Assign AudioSource (Missing)"))
                    AssignMissingAudioSources();

                if (GUILayout.Button("Add OnButtonClick → Selected"))
                    AddScriptToSelection();

                if (GUILayout.Button("Add OnButtonClick → All Without"))
                    AddScriptToAllWithout();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All Filtered"))
                    SetSelectionOnFiltered(true);

                if (GUILayout.Button("Deselect All Filtered"))
                    SetSelectionOnFiltered(false);

                if (GUILayout.Button("Refresh"))
                    Scan();
            }
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorGUILayout.HelpBox("Bulk assign disabled in Play Mode.", MessageType.Warning);
    }

    private void DrawFilters()
    {
        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            filter = (FilterMode)EditorGUILayout.EnumPopup("Filter", filter);
            search = EditorGUILayout.TextField("Search", search);
            if (EditorGUI.EndChangeCheck())
                RebuildFiltered();
        }
    }

    #endregion

    #region List

    private void DrawList()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField($"Results ({filtered.Count})", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Sel", GUILayout.Width(28f));
            GUILayout.Label("Path", GUILayout.MinWidth(220f));
            GUILayout.Label("Script", GUILayout.Width(54f));
            GUILayout.Label("AudioSrc", GUILayout.Width(64f));
            GUILayout.Label("Clip", GUILayout.Width(140f));
            GUILayout.Label("Actions", GUILayout.Width(160f));
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < filtered.Count; i++)
        {
            Entry entry = filtered[i];
            if (entry.button == null)
                continue;

            using (new EditorGUILayout.HorizontalScope())
            {
                entry.selected = EditorGUILayout.Toggle(entry.selected, GUILayout.Width(28f));

                Color prev = GUI.color;
                GUI.color = GetRowColor(entry);
                EditorGUILayout.LabelField(entry.path, GUILayout.MinWidth(220f));
                GUI.color = prev;

                EditorGUILayout.LabelField(entry.hasScript ? "Yes" : "No", GUILayout.Width(54f));
                EditorGUILayout.LabelField(GetAudioSourceLabel(entry), GUILayout.Width(64f));
                EditorGUILayout.LabelField(entry.hasClickClip ? entry.clipName : "Missing", GUILayout.Width(140f));

                if (GUILayout.Button("Select", GUILayout.Width(52f)))
                {
                    Selection.activeObject = entry.button.gameObject;
                    EditorGUIUtility.PingObject(entry.button.gameObject);
                }

                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || masterClip == null))
                {
                    if (GUILayout.Button("Set Clip", GUILayout.Width(60f)))
                        AssignClip(entry, masterClip, overwrite: true);
                }

                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (!entry.hasScript)
                    {
                        if (GUILayout.Button("Add", GUILayout.Width(40f)))
                            AddScript(entry);
                    }
                    else if (!entry.hasAudioSourceComponent || !entry.audioSourceFieldAssigned || !entry.buttonFieldAssigned)
                    {
                        if (GUILayout.Button("Fix AS", GUILayout.Width(40f)))
                            AssignAudioSource(entry);
                    }
                    else
                    {
                        GUILayout.Space(44f);
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private static Color GetRowColor(Entry entry)
    {
        if (!entry.hasScript)
            return new Color(1f, 0.55f, 0.55f);

        if (!entry.hasAudioSourceComponent || !entry.audioSourceFieldAssigned || !entry.hasClickClip)
            return new Color(1f, 0.85f, 0.45f);

        return new Color(0.65f, 1f, 0.65f);
    }

    private static string GetAudioSourceLabel(Entry entry)
    {
        if (!entry.hasScript)
            return "-";

        if (!entry.hasAudioSourceComponent)
            return "No Comp";

        if (!entry.audioSourceFieldAssigned)
            return "Unwired";

        return "OK";
    }

    #endregion

    #region Scan

    private void Scan()
    {
        entries.Clear();

        Button[] buttons = FindButtons();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            Entry entry = BuildEntry(button);
            entries.Add(entry);
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
        RebuildCounts();
        RebuildFiltered();
        Repaint();
    }

    private Button[] FindButtons()
    {
        if (scanRoot != null)
            return scanRoot.GetComponentsInChildren<Button>(includeInactive);

#if UNITY_2023_1_OR_NEWER
        FindObjectsInactive inactive = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
        return FindObjectsByType<Button>(inactive, FindObjectsSortMode.None);
#else
        Button[] found = FindObjectsOfType<Button>(includeInactive);
        return found;
#endif
    }

    private static Entry BuildEntry(Button button)
    {
        OnButtonClick click = button.GetComponent<OnButtonClick>();
        AudioSource audioSourceComponent = button.GetComponent<AudioSource>();

        Entry entry = new Entry
        {
            button = button,
            path = GetHierarchyPath(button.transform),
            hasScript = click != null,
            hasAudioSourceComponent = audioSourceComponent != null,
            audioSourceFieldAssigned = false,
            buttonFieldAssigned = false,
            hasClickClip = false,
            clipName = string.Empty,
            selected = false
        };

        if (click == null)
            return entry;

        SerializedObject so = new SerializedObject(click);
        SerializedProperty buttonProp = so.FindProperty("button");
        SerializedProperty audioProp = so.FindProperty("audioSource");
        SerializedProperty clipProp = so.FindProperty("clickSound");

        entry.buttonFieldAssigned = buttonProp != null && buttonProp.objectReferenceValue != null;
        entry.audioSourceFieldAssigned = audioProp != null && audioProp.objectReferenceValue != null;

        AudioClip clip = clipProp != null ? clipProp.objectReferenceValue as AudioClip : null;
        entry.hasClickClip = clip != null;
        entry.clipName = clip != null ? clip.name : string.Empty;

        return entry;
    }

    private void RebuildCounts()
    {
        countAll = entries.Count;
        countWithScript = 0;
        countWithoutScript = 0;
        countMissingAudio = 0;
        countMissingClip = 0;
        countOk = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (!e.hasScript)
            {
                countWithoutScript++;
                continue;
            }

            countWithScript++;

            bool audioOk = e.hasAudioSourceComponent && e.audioSourceFieldAssigned;
            if (!audioOk)
                countMissingAudio++;

            if (!e.hasClickClip)
                countMissingClip++;

            if (audioOk && e.hasClickClip && e.buttonFieldAssigned)
                countOk++;
        }
    }

    private void RebuildFiltered()
    {
        filtered.Clear();
        string query = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (!PassesFilter(e))
                continue;

            if (query != null && e.path.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            filtered.Add(e);
        }
    }

    private bool PassesFilter(Entry e)
    {
        switch (filter)
        {
            case FilterMode.HasOnButtonClick:
                return e.hasScript;
            case FilterMode.MissingOnButtonClick:
                return !e.hasScript;
            case FilterMode.MissingAudioSource:
                return e.hasScript && (!e.hasAudioSourceComponent || !e.audioSourceFieldAssigned);
            case FilterMode.MissingClickClip:
                return e.hasScript && !e.hasClickClip;
            case FilterMode.Ok:
                return e.hasScript
                       && e.hasAudioSourceComponent
                       && e.audioSourceFieldAssigned
                       && e.buttonFieldAssigned
                       && e.hasClickClip;
            default:
                return true;
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder(64);
        Transform current = transform;
        while (current != null)
        {
            if (sb.Length > 0)
                sb.Insert(0, '/');

            sb.Insert(0, current.name);
            current = current.parent;
        }

        Scene scene = transform.gameObject.scene;
        if (scene.IsValid())
            sb.Insert(0, scene.name + "/");

        return sb.ToString();
    }

    #endregion

    #region Actions

    private void SetSelectionOnFiltered(bool value)
    {
        for (int i = 0; i < filtered.Count; i++)
            filtered[i].selected = value;

        Repaint();
    }

    private void AssignClipToSelection(bool overwrite)
    {
        if (!ValidateMasterClip())
            return;

        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (!entry.selected)
                continue;

            if (AssignClip(entry, masterClip, overwrite))
                count++;
        }

        FinishBulk($"Assigned click clip to {count} selected button(s).");
    }

    private void AssignClipToMissing()
    {
        if (!ValidateMasterClip())
            return;

        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (!entry.hasScript || entry.hasClickClip)
                continue;

            if (AssignClip(entry, masterClip, overwrite: true))
                count++;
        }

        FinishBulk($"Assigned click clip to {count} missing button(s).");
    }

    private void AssignClipToAllWithScript()
    {
        if (!ValidateMasterClip())
            return;

        if (!EditorUtility.DisplayDialog(
                "Overwrite Click Clips",
                "Assign master clip to EVERY button that has OnButtonClick? Existing clips will be overwritten.",
                "Assign All",
                "Cancel"))
            return;

        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (!entry.hasScript)
                continue;

            if (AssignClip(entry, masterClip, overwrite: true))
                count++;
        }

        FinishBulk($"Assigned click clip to {count} OnButtonClick button(s).");
    }

    private bool ValidateMasterClip()
    {
        if (masterClip != null)
            return true;

        EditorUtility.DisplayDialog("Master Clip Missing", "Assign a Master Click Clip first.", "OK");
        return false;
    }

    private bool AssignClip(Entry entry, AudioClip clip, bool overwrite)
    {
        if (entry == null || entry.button == null || clip == null)
            return false;

        OnButtonClick click = entry.button.GetComponent<OnButtonClick>();
        if (click == null)
        {
            if (!AddScript(entry))
                return false;

            click = entry.button.GetComponent<OnButtonClick>();
            if (click == null)
                return false;
        }

        SerializedObject so = new SerializedObject(click);
        SerializedProperty clipProp = so.FindProperty("clickSound");
        if (clipProp == null)
            return false;

        if (!overwrite && clipProp.objectReferenceValue != null)
            return false;

        Undo.RecordObject(click, "Assign OnButtonClick Clip");
        clipProp.objectReferenceValue = clip;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(click);
        PrefabUtility.RecordPrefabInstancePropertyModifications(click);
        MarkSceneDirty(entry.button.gameObject);
        return true;
    }

    private void AssignMissingAudioSources()
    {
        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (!entry.hasScript)
                continue;

            if (entry.hasAudioSourceComponent && entry.audioSourceFieldAssigned && entry.buttonFieldAssigned)
                continue;

            if (AssignAudioSource(entry))
                count++;
        }

        FinishBulk($"Fixed AudioSource/refs on {count} button(s).");
    }

    private bool AssignAudioSource(Entry entry)
    {
        if (entry == null || entry.button == null)
            return false;

        OnButtonClick click = entry.button.GetComponent<OnButtonClick>();
        if (click == null)
            return false;

        AudioSource audioSource = entry.button.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = Undo.AddComponent<AudioSource>(entry.button.gameObject);

        Undo.RecordObject(click, "Assign OnButtonClick AudioSource");
        click.AssignRefs();
        EditorUtility.SetDirty(click);
        PrefabUtility.RecordPrefabInstancePropertyModifications(click);
        MarkSceneDirty(entry.button.gameObject);
        return true;
    }

    private void AddScriptToSelection()
    {
        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (!entry.selected || entry.hasScript)
                continue;

            if (!AddScript(entry))
                continue;

            if (masterClip != null)
                AssignClip(entry, masterClip, overwrite: true);

            count++;
        }

        FinishBulk($"Added OnButtonClick to {count} selected button(s).");
    }

    private void AddScriptToAllWithout()
    {
        if (!EditorUtility.DisplayDialog(
                "Add OnButtonClick",
                "Add OnButtonClick to ALL buttons that are missing it?",
                "Add All",
                "Cancel"))
            return;

        int count = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry.hasScript)
                continue;

            if (!AddScript(entry))
                continue;

            if (masterClip != null)
                AssignClip(entry, masterClip, overwrite: true);

            count++;
        }

        FinishBulk($"Added OnButtonClick to {count} button(s).");
    }

    private bool AddScript(Entry entry)
    {
        if (entry == null || entry.button == null)
            return false;

        if (entry.button.GetComponent<OnButtonClick>() != null)
        {
            entry.hasScript = true;
            return true;
        }

        OnButtonClick click = Undo.AddComponent<OnButtonClick>(entry.button.gameObject);
        if (click == null)
            return false;

        click.AssignRefs();
        EditorUtility.SetDirty(click);
        PrefabUtility.RecordPrefabInstancePropertyModifications(click);
        MarkSceneDirty(entry.button.gameObject);

        entry.hasScript = true;
        entry.hasAudioSourceComponent = entry.button.GetComponent<AudioSource>() != null;
        entry.audioSourceFieldAssigned = true;
        entry.buttonFieldAssigned = true;
        return true;
    }

    private void FinishBulk(string message)
    {
        Scan();
        ShowNotification(new GUIContent(message));
    }

    private static void MarkSceneDirty(GameObject go)
    {
        if (go == null)
            return;

        Scene scene = go.scene;
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);
    }

    #endregion
}
#endif
