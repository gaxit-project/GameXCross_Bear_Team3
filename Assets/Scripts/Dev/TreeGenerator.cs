using UnityEngine;
using System.Collections.Generic;

public class TreeGenerator : MonoBehaviour
{
    [Header("必須設定")]
    [Tooltip("手順1で作った『CorrectTree』プレハブをセット")]
    public GameObject treePrefab;

    [Tooltip("木を生やす地面のオブジェクト")]
    public GameObject fieldObject;

    [Header("配置ルール")]
    [Tooltip("生成する本数")]
    public int maxTreeCount = 50;

    [Tooltip("木同士の間隔")]
    public float minDistance = 1.5f;

    [Tooltip("地面より少し低い値にしておく（例: -100）")]
    public float minSpawnHeight = -100f;

    [Header("サイズ調整（倍率）")]
    [Tooltip("高さ（Y）を元の何倍にするか（例：0.8倍 ～ 1.5倍）")]
    public Vector2 scaleYMultiplier = new Vector2(0.8f, 1.5f);

    [HideInInspector]
    public List<GameObject> spawnedTrees = new List<GameObject>();

    public void GenerateTrees()
    {
        if (treePrefab == null || fieldObject == null)
        {
            Debug.LogError("Tree Prefab または Field Object が設定されていません！");
            return;
        }

        Collider col = fieldObject.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("地面（Field Object）にColliderがありません！");
            return;
        }

        ClearTrees();

        Bounds bounds = col.bounds;
        int count = 0;
        int attempts = 0;

        // 指定数配置できるまでループ（無限ループ防止付き）
        while (count < maxTreeCount && attempts < maxTreeCount * 20)
        {
            attempts++;

            // 範囲内からランダムに選ぶ
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 startPos = new Vector3(x, bounds.max.y + 10f, z);

            if (Physics.Raycast(startPos, Vector3.down, out RaycastHit hit, 200f))
            {
                // 指定した地面に当たったか、かつ高さ制限をクリアしているか
                if (hit.collider.gameObject == fieldObject && hit.point.y >= minSpawnHeight)
                {
                    if (CanPlace(hit.point))
                    {
                        PlaceTree(hit.point);
                        count++;
                    }
                }
            }
        }
        Debug.Log($"生成完了: {count} 本の木を配置しました。");
    }

    bool CanPlace(Vector3 pos)
    {
        foreach (var t in spawnedTrees)
        {
            if (t == null) continue;
            // 木同士が近すぎないかチェック
            if (Vector3.Distance(t.transform.position, pos) < minDistance) return false;
        }
        return true;
    }

    void PlaceTree(Vector3 pos)
    {
        // 1. プレハブを生成（回転はプレハブのものを維持）
        GameObject obj = Instantiate(treePrefab, pos, treePrefab.transform.rotation);
        
        // 管理しやすいように、一旦このManagerの子にする
        obj.transform.parent = transform;

        // 2. 【ここが修正ポイント】
        // 元のプレハブのスケール（大きさ）を取得
        Vector3 originalScale = obj.transform.localScale;

        // Y軸（高さ）にかけるランダムな倍率を決める
        float randomMultiplier = Random.Range(scaleYMultiplier.x, scaleYMultiplier.y);

        // 元のXとZはそのまま使い、Yだけ倍率をかけて設定し直す
        obj.transform.localScale = new Vector3(
            originalScale.x, 
            originalScale.y * randomMultiplier, 
            originalScale.z
        );

        spawnedTrees.Add(obj);
    }

    public void ClearTrees()
    {
        foreach (var t in spawnedTrees)
        {
            if (t != null) DestroyImmediate(t);
        }
        spawnedTrees.Clear();

        // リストから漏れた子オブジェクトも念のため全削除
        var list = new List<GameObject>();
        foreach(Transform child in transform) list.Add(child.gameObject);
        list.ForEach(DestroyImmediate);
    }
}