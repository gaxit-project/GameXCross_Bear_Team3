using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class SceneLauncher : EditorWindow
{
    [MenuItem("Tools/Scene Launcher")]
    public static void ShowWindow()
    {
        GetWindow<SceneLauncher>("Launcher");
    }

    private void OnGUI()
    {
        GUILayout.Label("Quick Scene Open", EditorStyles.boldLabel);

        foreach(var scene in EditorBuildSettings.scenes)
        {
            if(scene.enabled)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scene.path);

                if(GUILayout.Button(sceneName))
                {
                    if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scene.path);
                    }
                }
            }
        }

        GUILayout.Space(10);
        if(GUILayout.Button("Open Build Settings"))
        {
            GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }
    }
}
