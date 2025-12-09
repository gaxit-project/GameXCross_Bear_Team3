using UnityEngine;
using System.Collections.Generic;

public class TreeGenerator : MonoBehaviour
{
    [Header("必須設定")]
    [Tooltip("木のプレハブ")]
    public GameObject treePrefab;

    [Tooltip("山も地面も含むフィールドのオブジェクト（Cube）")]
    public GameObject fieldObject; 

    [Header("高さフィルター（重要）")]
    [Tooltip("これより低い場所（砂地）には木を生やしません。シーンビューの赤い板を目安に調整してください。")]
    public float minSpawnHeight = -1.5f;

    [Header("配置設定")]
    [Tooltip("生成する木の最大数")]
    public int maxTreeCount = 50;

    [Tooltip("木同士の最低間隔（重なり防止）")]
    public float minDistance = 1.5f;

    [Tooltip("縦方向（Y）のスケール倍率の範囲")]
    public Vector2 scaleYRange = new Vector2(0.8f, 1.5f);

    // 生成した木を管理するリスト
    [HideInInspector] 
    public List<GameObject> spawnedTrees = new List<GameObject>();

    public void GenerateTrees()
    {
        if (treePrefab == null || fieldObject == null)
        {
            Debug.LogError("【エラー】Tree Prefab または Field Object が未設定です！");
            return;
        }

        Collider fieldCollider = fieldObject.GetComponent<Collider>();
        if (fieldCollider == null)
        {
            Debug.LogError("【エラー】フィールドにColliderがついていません！");
            return;
        }

        ClearTrees(); // リセット

        Bounds bounds = fieldCollider.bounds;
        int placedCount = 0;
        int attempts = 0;

        // 指定数配置できるまでループ
        while (placedCount < maxTreeCount && attempts < maxTreeCount * 20)
        {
            attempts++;

            // 1. 範囲内のランダムな座標(X, Z)を決定
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 rayOrigin = new Vector3(randomX, bounds.max.y + 10f, randomZ);

            // 2. 上空から真下にレイキャスト
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 100f))
            {
                // 3. 当たったのがフィールドで、かつ「高さが砂地より上」か確認
                if (hit.collider.gameObject == fieldObject && hit.point.y >= minSpawnHeight)
                {
                    Vector3 spawnPos = hit.point;

                    // 4. 重なりチェック
                    if (CanPlace(spawnPos))
                    {
                        PlaceTree(spawnPos);
                        placedCount++;
                    }
                }
            }
        }
        Debug.Log($"生成完了: {placedCount} 本の木を配置しました。");
    }

    bool CanPlace(Vector3 position)
    {
        foreach (var tree in spawnedTrees)
        {
            if (tree == null) continue;
            if (Vector3.Distance(tree.transform.position, position) < minDistance)
            {
                return false; 
            }
        }
        return true;
    }

    void PlaceTree(Vector3 position)
    {
        // 【修正ポイント】プレハブ自体の回転（Z=180など）をそのまま使用する
        GameObject newTree = Instantiate(treePrefab, position, treePrefab.transform.rotation);
        
        newTree.transform.parent = this.transform;

        // スケール変更（Y軸のみランダム、XZはプレハブのまま維持）
        float originalX = newTree.transform.localScale.x;
        float originalZ = newTree.transform.localScale.z;
        float randomY = Random.Range(scaleYRange.x, scaleYRange.y);
        
        newTree.transform.localScale = new Vector3(originalX, randomY, originalZ);

        spawnedTrees.Add(newTree);
    }

    public void ClearTrees()
    {
        foreach (var tree in spawnedTrees)
        {
            if (tree != null) DestroyImmediate(tree);
        }
        spawnedTrees.Clear();
        
        var children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        children.ForEach(child => DestroyImmediate(child));
    }

    // 高さの基準線を可視化（GizmosボタンがONのときだけ見える）
    private void OnDrawGizmosSelected()
    {
        if (fieldObject != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f); // 赤色（半透明）
            Bounds b = fieldObject.GetComponent<Collider>().bounds;
            // 高さ（Y）だけ設定値に固定して表示
            Vector3 center = new Vector3(b.center.x, minSpawnHeight, b.center.z);
            Vector3 size = new Vector3(b.size.x, 0.1f, b.size.z);
            Gizmos.DrawCube(center, size);
        }
    }
}