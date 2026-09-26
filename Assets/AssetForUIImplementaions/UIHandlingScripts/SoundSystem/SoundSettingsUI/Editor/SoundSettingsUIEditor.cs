#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundSettingsUI))]
[CanEditMultipleObjects]
public class SoundSettingsUIEditor : Editor
{
    private SerializedProperty useMusicToggle;
    private SerializedProperty useMusicSlider;
    private SerializedProperty useSfxToggle;
    private SerializedProperty useSfxSlider;
    private SerializedProperty showPercentageText;
    private SerializedProperty linkToggleAndSlider;
    private SerializedProperty restoreOnToggleOn;
    private SerializedProperty lerpDuration;
    private SerializedProperty uiAudioSource;
    private SerializedProperty toggleClip;
    private SerializedProperty sliderReleaseClip;
    private SerializedProperty musicToggle;
    private SerializedProperty musicSlider;
    private SerializedProperty musicPercentText;
    private SerializedProperty sfxToggle;
    private SerializedProperty sfxSlider;
    private SerializedProperty sfxPercentText;

    private void OnEnable()
    {
        useMusicToggle = serializedObject.FindProperty("useMusicToggle");
        useMusicSlider = serializedObject.FindProperty("useMusicSlider");
        useSfxToggle = serializedObject.FindProperty("useSfxToggle");
        useSfxSlider = serializedObject.FindProperty("useSfxSlider");
        showPercentageText = serializedObject.FindProperty("showPercentageText");
        linkToggleAndSlider = serializedObject.FindProperty("linkToggleAndSlider");
        restoreOnToggleOn = serializedObject.FindProperty("restoreOnToggleOn");
        lerpDuration = serializedObject.FindProperty("lerpDuration");
        uiAudioSource = serializedObject.FindProperty("uiAudioSource");
        toggleClip = serializedObject.FindProperty("toggleClip");
        sliderReleaseClip = serializedObject.FindProperty("sliderReleaseClip");
        musicToggle = serializedObject.FindProperty("musicToggle");
        musicSlider = serializedObject.FindProperty("musicSlider");
        musicPercentText = serializedObject.FindProperty("musicPercentText");
        sfxToggle = serializedObject.FindProperty("sfxToggle");
        sfxSlider = serializedObject.FindProperty("sfxSlider");
        sfxPercentText = serializedObject.FindProperty("sfxPercentText");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        EditorGUILayout.LabelField("Features", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useMusicToggle);
        EditorGUILayout.PropertyField(useMusicSlider);
        EditorGUILayout.PropertyField(useSfxToggle);
        EditorGUILayout.PropertyField(useSfxSlider);
        EditorGUILayout.PropertyField(showPercentageText);

        bool anyToggle = useMusicToggle.boolValue || useSfxToggle.boolValue;
        bool anySlider = useMusicSlider.boolValue || useSfxSlider.boolValue;
        bool canLink = (useMusicToggle.boolValue && useMusicSlider.boolValue)
                       || (useSfxToggle.boolValue && useSfxSlider.boolValue);

        if (canLink)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Link", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(linkToggleAndSlider);
            if (linkToggleAndSlider.boolValue)
            {
                EditorGUILayout.PropertyField(restoreOnToggleOn);
                EditorGUILayout.PropertyField(lerpDuration);
            }
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("UI Audio", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(uiAudioSource);
        EditorGUILayout.PropertyField(toggleClip);
        EditorGUILayout.PropertyField(sliderReleaseClip);

        if (useMusicToggle.boolValue || useMusicSlider.boolValue)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Music", EditorStyles.boldLabel);
            if (useMusicToggle.boolValue)
                EditorGUILayout.PropertyField(musicToggle);
            if (useMusicSlider.boolValue)
                EditorGUILayout.PropertyField(musicSlider);
            if (useMusicSlider.boolValue && showPercentageText.boolValue)
                EditorGUILayout.PropertyField(musicPercentText, new GUIContent("Music Percent Text"));
        }

        if (useSfxToggle.boolValue || useSfxSlider.boolValue)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("SFX", EditorStyles.boldLabel);
            if (useSfxToggle.boolValue)
                EditorGUILayout.PropertyField(sfxToggle);
            if (useSfxSlider.boolValue)
                EditorGUILayout.PropertyField(sfxSlider);
            if (useSfxSlider.boolValue && showPercentageText.boolValue)
                EditorGUILayout.PropertyField(sfxPercentText, new GUIContent("SFX Percent Text"));
        }

        if (!anyToggle && !anySlider)
            EditorGUILayout.HelpBox("Enable at least one toggle or slider feature.", MessageType.Info);

        DrawAssignAudioSourceButton();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        EditorGUILayout.Space(4f);
    }

    private void DrawAssignAudioSourceButton()
    {
        EditorGUILayout.Space(6f);

        if (!GUILayout.Button("Assign UI Audio Source", GUILayout.Height(28f)))
            return;

        serializedObject.ApplyModifiedProperties();

        foreach (Object obj in targets)
        {
            SoundSettingsUI settingsUi = (SoundSettingsUI)obj;
            Undo.RecordObject(settingsUi, "Assign UI Audio Source");
            settingsUi.AssignUiAudioSource();
            EditorUtility.SetDirty(settingsUi);
            PrefabUtility.RecordPrefabInstancePropertyModifications(settingsUi);
        }

        serializedObject.Update();
        GUI.FocusControl(null);
    }
}
#endif
