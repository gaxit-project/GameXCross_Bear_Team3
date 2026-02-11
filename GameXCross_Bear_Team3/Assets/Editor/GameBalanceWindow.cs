/* #if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class GameBalanceWindow : EditorWindow
{
    private GameBalanceData balanceData;
    private const string AssetPath = "Assets/Resources/GameBalanceData.asset";

    // スクロール位置を記憶する変数
    private Vector2 _scrollPosition = Vector2.zero;

    [MenuItem("Game/Balance Manager")]
    public static void ShowWindow()
    {
        GetWindow<GameBalanceWindow>("Balance Manager");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void OnGUI()
    {
        // --- 1. 固定ヘッダー部分 ---
        GUILayout.Label("ゲームバランス一元管理", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (balanceData == null)
        {
            EditorGUILayout.HelpBox("データファイルが見つかりません。", MessageType.Warning);
            if (GUILayout.Button("データファイルを作成する"))
            {
                CreateData();
            }
        }
        else
        {
            // --- 2. スクロール領域の開始 ---
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.BeginVertical("box");

            SerializedObject so = new SerializedObject(balanceData);
            so.Update();

            SerializedProperty prop = so.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                // "m_Script"（スクリプト参照プロパティ）は表示しない
                if (prop.name == "m_Script") { enterChildren = false; continue; }

                EditorGUILayout.PropertyField(prop, true);
                enterChildren = false;
            }

            so.ApplyModifiedProperties();

            EditorGUILayout.EndVertical();

            // --- 3. スクロール領域の終了 ---
            EditorGUILayout.EndScrollView();

            // --- 4. 固定フッター部分（保存ボタン） ---
            EditorGUILayout.Space();
            GUI.backgroundColor = new Color(0.7f, 1.0f, 0.7f);
            if (GUILayout.Button("設定を保存 (Force Save)", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(balanceData);
                AssetDatabase.SaveAssets();
                Debug.Log("バランスデータを保存しました。");
            }
            GUI.backgroundColor = Color.white; // 色を戻す
        }
    }

    private void LoadData()
    {
        balanceData = AssetDatabase.LoadAssetAtPath<GameBalanceData>(AssetPath);
    }

    private void CreateData()
    {
        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(AssetPath)))
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AssetPath));
        }

        balanceData = CreateInstance<GameBalanceData>();
        AssetDatabase.CreateAsset(balanceData, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[GameBalanceWindow] データを作成しました: {AssetPath}");
    }
}
#endif */