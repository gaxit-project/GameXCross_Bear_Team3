using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PlayModeTools
{
    private const string MENU_NAME = "Tools/Always Play From First Scene";
    private const string SETTING_KEY = "PlayFromFirstScene_Enabled";

    [MenuItem(MENU_NAME)]
    private static void ToggleAction()
    {
        bool isEnabled = GetEnabled();

        EditorPrefs.SetBool(SETTING_KEY, !isEnabled);

        Menu.SetChecked(MENU_NAME, !isEnabled);
    }

    [MenuItem(MENU_NAME, true)]
    private static bool ValidateAction()
    {
        Menu.SetChecked(MENU_NAME, GetEnabled());
        return true;
    }

    private static bool GetEnabled()
    {
        return EditorPrefs.GetBool(SETTING_KEY, false);
    }

    [InitializeOnLoadMethod]
    private static void RegisterPlayModeChanged()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if(state == PlayModeStateChange.ExitingEditMode)
        {
            if (GetEnabled())
            {
                if(EditorBuildSettings.scenes.Length == 0)
                {
                    Debug.LogWarning("Build Settingsにシーンが登録されていません。「File > Build Settings」からシーンを追加してください。");
                    return;
                }

                var firstScene = EditorBuildSettings.scenes[0];

                if(firstScene != null && !string.IsNullOrEmpty(firstScene.path))
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
}
