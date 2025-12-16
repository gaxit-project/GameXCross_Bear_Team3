using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class BuildingGhost : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    [Header("判定調整")]
    [SerializeField] private float sizeScale = 0.9f;

    private BoxCollider boxCollider;

    // 【変更点1】単体ではなく、配列（複数）で保持する
    private MeshRenderer[] meshRenderers;

    private bool isPlaceable = true;

    public bool IsPlaceable => isPlaceable;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

        // 【変更点2】自分自身を含む、すべての子オブジェクトのMeshRendererを取得する
        meshRenderers = GetComponentsInChildren<MeshRenderer>();

        boxCollider.isTrigger = true;
    }

    private void Update()
    {
        CheckOverlap();
        UpdateVisual();
    }

    private void CheckOverlap()
    {
        // 判定ボックスの中心とサイズ
        Vector3 center = transform.position + boxCollider.center;
        Vector3 halfExtents = (boxCollider.size * 0.5f) * sizeScale;
        Quaternion orientation = transform.rotation;

        // 指定範囲内のすべてのコライダーを取得
        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, orientation);

        bool overlapFound = false;

        foreach (var hit in hitColliders)
        {
            // 【修正ポイント】
            // 以前：if (hit.gameObject == gameObject) continue;
            // これだと「自分自身」しか無視できず、「自分の子」には反応してしまう

            // 今回：自分、または自分の子要素（ヒエラルキーの下にあるもの）なら無視する
            if (hit.transform.IsChildOf(transform)) continue;

            // 2. 地面タグは無視
            if (hit.CompareTag(groundTag)) continue;

            // ここに来たということは「自分たち」でも「地面」でもない何かに当たっている
            overlapFound = true;

            // デバッグ用：何が邪魔しているかコンソールに表示
            Debug.Log($"邪魔なオブジェクト: {hit.name}");

            break;
        }

        isPlaceable = !overlapFound;
    }

    private void UpdateVisual()
    {
        // 【変更点3】レンダラーが一つもない場合は何もしない
        if (meshRenderers == null || meshRenderers.Length == 0) return;

        // 適用するマテリアルを先に決定
        Material targetMaterial = isPlaceable ? validMaterial : invalidMaterial;

        // 【変更点4】取得した全てのレンダラーに対してループ処理でマテリアルを適用
        foreach (var renderer in meshRenderers)
        {
            if (renderer == null) continue;

            // もし「1つのオブジェクトに複数のマテリアル（例：ドアとノブ）」がついている場合、
            // その全てを置き換えるには sharedMaterials 配列ごと入れ替える必要があります。
            // 単純なモデルなら renderer.sharedMaterial = targetMaterial; だけでOKです。

            // --- 念の為、全マテリアルスロットを上書きする丁寧な実装 ---
            Material[] mats = renderer.sharedMaterials;
            bool needsUpdate = false;

            // マテリアル配列の中身をチェックして書き換え
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != targetMaterial)
                {
                    mats[i] = targetMaterial;
                    needsUpdate = true;
                }
            }

            // 変更が必要な場合のみ適用（負荷対策）
            if (needsUpdate)
            {
                renderer.sharedMaterials = mats;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (boxCollider == null) return;

        Gizmos.color = isPlaceable ? Color.green : Color.red;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;

        Gizmos.DrawWireCube(boxCollider.center, boxCollider.size * sizeScale);
    }
}