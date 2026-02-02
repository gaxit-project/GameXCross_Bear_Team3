using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class BuildingGhost : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;
    [SerializeField] private PointerController p; // コントローラーの名前はそのままにしています

    [Header("判定調整")]
    [SerializeField] private float sizeScale = 0.9f;

    private BoxCollider boxCollider;
    private MeshRenderer[] meshRenderers;
    private bool isPlaceable = true;

    public bool IsPlaceable => isPlaceable;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
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
        // 【修正1】中心点の計算
        // boxCollider.centerはローカル座標なので、TransformPointでワールド座標に変換します。
        // これにより、オブジェクトが回転していても正しい中心位置が計算されます。
        Vector3 center = transform.TransformPoint(boxCollider.center);

        // 【修正2】サイズの計算
        // boxCollider.sizeは元のサイズなので、オブジェクトのスケール(lossyScale)を掛け合わせます。
        // これでオブジェクトを拡大縮小していても判定ボックスが追従します。
        Vector3 worldSize = Vector3.Scale(boxCollider.size, transform.lossyScale);
        Vector3 halfExtents = (worldSize * 0.5f) * sizeScale;

        Quaternion orientation = transform.rotation;

        // 指定範囲内のすべてのコライダーを取得
        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, orientation);

        bool overlapFound = false;

        foreach (var hit in hitColliders)
        {
            // 自分、または自分の子要素なら無視
            if (hit.transform.IsChildOf(transform)) continue;

            // 地面タグは無視
            if (hit.CompareTag(groundTag)) continue;

            overlapFound = true;
            // Debug.Log($"衝突: {hit.name}"); 
            break;
        }

        isPlaceable = !overlapFound;
    }

    private void UpdateVisual()
    {
        if (meshRenderers == null || meshRenderers.Length == 0) return;

        Material targetMaterial = isPlaceable ? validMaterial : invalidMaterial;

        // pがアタッチされていない場合のnullチェックを追加しておくと安全です
        if (p != null) p.canput = isPlaceable;

        foreach (var renderer in meshRenderers)
        {
            if (renderer == null) continue;

            Material[] mats = renderer.sharedMaterials;
            bool needsUpdate = false;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != targetMaterial)
                {
                    mats[i] = targetMaterial;
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                renderer.sharedMaterials = mats;
            }
        }
    }

    // 【修正3】Gizmosも計算式を合わせる
    // Physics.OverlapBoxと全く同じ計算で描画しないと、見た目と判定がズレて原因がわからなくなります
    private void OnDrawGizmos()
    {
        if (boxCollider == null) return;

        Gizmos.color = isPlaceable ? Color.green : Color.red;

        // Physicsの計算と同じロジックを用意
        Vector3 center = transform.TransformPoint(boxCollider.center);
        Vector3 worldSize = Vector3.Scale(boxCollider.size, transform.lossyScale);
        Vector3 size = worldSize * sizeScale;

        // 回転させた状態でキューブを描画
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, transform.rotation, size);
        Gizmos.matrix = rotationMatrix;

        // Matrixですでに位置・回転・サイズを適用しているので、
        // DrawWireCubeには原点中心・サイズ1の立方体を渡せばOK
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}