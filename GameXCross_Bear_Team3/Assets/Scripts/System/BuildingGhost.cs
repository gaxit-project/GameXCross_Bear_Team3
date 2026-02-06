using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class BuildingGhost : MonoBehaviour
{
    [Header("�ݒ�")]
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;
    [SerializeField] private Material constructionMaterial;
    [SerializeField] private PointerController p; // �R���g���[���[�̖��O�͂��̂܂܂ɂ��Ă��܂�

    [Header("���蒲��")]
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

    public void SwitchToConstructionMode()
    {
        // マテリアルが設定されていなければ、機能だけ止めて終了（プレハブ本来の色になる）
        if (constructionMaterial == null)
        {
            this.enabled = false; 
            return;
        }

        // 全てのレンダラーを「建設中マテリアル」に塗り替える
        foreach (var renderer in meshRenderers)
        {
            if (renderer == null) continue;

            Material[] newMats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                newMats[i] = constructionMaterial;
            }
            renderer.sharedMaterials = newMats;
        }

        // 最後にこのスクリプトを停止（Updateで緑/赤に戻されるのを防ぐ）
        this.enabled = false; 
    }

    private void CheckOverlap()
    {
        // �y�C��1�z���S�_�̌v�Z
        // boxCollider.center�̓��[�J�����W�Ȃ̂ŁATransformPoint�Ń��[���h���W�ɕϊ����܂��B
        // ����ɂ��A�I�u�W�F�N�g����]���Ă��Ă����������S�ʒu���v�Z����܂��B
        Vector3 center = transform.TransformPoint(boxCollider.center);

        // �y�C��2�z�T�C�Y�̌v�Z
        // boxCollider.size�͌��̃T�C�Y�Ȃ̂ŁA�I�u�W�F�N�g�̃X�P�[��(lossyScale)���|�����킹�܂��B
        // ����ŃI�u�W�F�N�g���g��k�����Ă��Ă�����{�b�N�X���Ǐ]���܂��B
        Vector3 worldSize = Vector3.Scale(boxCollider.size, transform.lossyScale);
        Vector3 halfExtents = (worldSize * 0.5f) * sizeScale;

        Quaternion orientation = transform.rotation;

        // �w��͈͓��̂��ׂẴR���C�_�[���擾
        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, orientation);

        bool overlapFound = false;

        foreach (var hit in hitColliders)
        {
            // �����A�܂��͎����̎q�v�f�Ȃ疳��
            if (hit.transform.IsChildOf(transform)) continue;

            // �n�ʃ^�O�͖���
            if (hit.CompareTag(groundTag)) continue;

            overlapFound = true;
            // Debug.Log($"�Փ�: {hit.name}"); 
            break;
        }

        isPlaceable = !overlapFound;
    }

    private void UpdateVisual()
    {
        if (meshRenderers == null || meshRenderers.Length == 0) return;

        Material targetMaterial = isPlaceable ? validMaterial : invalidMaterial;

        // p���A�^�b�`����Ă��Ȃ��ꍇ��null�`�F�b�N��ǉ����Ă����ƈ��S�ł�
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

    // �y�C��3�zGizmos���v�Z�������킹��
    // Physics.OverlapBox�ƑS�������v�Z�ŕ`�悵�Ȃ��ƁA�����ڂƔ��肪�Y���Č������킩��Ȃ��Ȃ�܂�
    private void OnDrawGizmos()
    {
        if (boxCollider == null) return;

        Gizmos.color = isPlaceable ? Color.green : Color.red;

        // Physics�̌v�Z�Ɠ������W�b�N��p��
        Vector3 center = transform.TransformPoint(boxCollider.center);
        Vector3 worldSize = Vector3.Scale(boxCollider.size, transform.lossyScale);
        Vector3 size = worldSize * sizeScale;

        // ��]��������ԂŃL���[�u��`��
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, transform.rotation, size);
        Gizmos.matrix = rotationMatrix;

        // Matrix�ł��łɈʒu�E��]�E�T�C�Y��K�p���Ă���̂ŁA
        // DrawWireCube�ɂ͌��_���S�E�T�C�Y1�̗����̂�n����OK
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}