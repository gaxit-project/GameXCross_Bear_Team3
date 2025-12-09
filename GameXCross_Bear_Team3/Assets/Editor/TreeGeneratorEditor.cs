using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TreeGenerator))]
public class TreeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // 変数などを表示

        TreeGenerator script = (TreeGenerator)target;

        GUILayout.Space(20);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("木を生成する (Generate)", GUILayout.Height(40)))
        {
            script.GenerateTrees();
        }

        GUILayout.Space(10);

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("木を削除する (Clear)", GUILayout.Height(30)))
        {
            script.ClearTrees();
        }
    }
}