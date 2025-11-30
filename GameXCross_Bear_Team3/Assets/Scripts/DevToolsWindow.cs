using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

[InitializeOnLoad]
public class DevToolsWindow : EditorWindow
{
    private const string KEY_PLAY_FROM_FIRST = "DevTools_PlayFromFirstScene";

    [MenuItem("Tools/Game Dev Tools")]
    public static void ShowWindow()
    {
        GetWindow<DevToolsWindow>("Dev Tools");
    }

    static DevToolsWindow()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }


    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (EditorPrefs.GetBool(KEY_PLAY_FROM_FIRST, false))
            {
                if (EditorBuildSettings.scenes.Length == 0)
                {
                    Debug.LogWarning("Build Settingsにシーンが登録されていません。「File > Build Settings」からシーンを追加してください。");
                    return;
                }

                var firstScene = EditorBuildSettings.scenes[0];
                if (firstScene != null && !string.IsNullOrEmpty(firstScene.path))
                {
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(firstScene.path);
                    EditorSceneManager.playModeStartScene = sceneAsset;
                }
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Quick Scene Open", EditorStyles.boldLabel);

        bool isEnabled = EditorPrefs.GetBool(KEY_PLAY_FROM_FIRST, false);
        bool newStatus = GUILayout.Toggle(isEnabled, "Always Play From First Scene");

        if (isEnabled != newStatus) EditorPrefs.SetBool(KEY_PLAY_FROM_FIRST, newStatus);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // 区切り線
        GUILayout.Space(10);

        GUILayout.Label("Scene Launcher", EditorStyles.boldLabel);

        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scene.path);

                if (GUILayout.Button(sceneName))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scene.path);
                    }
                }
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Open Build Settings"))
        {
            GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(10);
    }
}