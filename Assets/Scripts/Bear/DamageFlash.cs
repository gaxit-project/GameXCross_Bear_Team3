using System.Collections;
using System.Collections.Generic; // Listを使うために必要
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    // マテリアルと、その元の色をセットで覚えておくためのクラス
    private class MaterialData
    {
        public Material material;
        public Color originalColor;
    }

    private List<MaterialData> _materialDataList = new List<MaterialData>();
    private Coroutine _flashCoroutine;

    void Start()
    {
        // 自分と子供にある全てのレンダラーを取得
        var renderers = GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            // r.materials (複数形) を使うと、そのモデルの全てのマテリアルが取得できる
            foreach (var mat in r.materials)
            {
                // リストに「マテリアル」と「元の色」をセットで登録
                _materialDataList.Add(new MaterialData
                {
                    material = mat,
                    originalColor = mat.color
                });
            }
        }
    }

    public void Flash()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            ResetColor();
        }
        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        foreach (var data in _materialDataList)
        {
            if (data.material == null) continue;

            // URP用とStandard用、両方のプロパティ名を試す
            if (data.material.HasProperty("_BaseColor"))
                data.material.SetColor("_BaseColor", flashColor);
            else if (data.material.HasProperty("_Color"))
                data.material.SetColor("_Color", flashColor);
        }

        yield return new WaitForSeconds(flashDuration);

        ResetColor();
        _flashCoroutine = null;
    }

    private void ResetColor()
    {
        foreach (var data in _materialDataList)
        {
            if (data.material != null)
            {
                if (data.material.HasProperty("_BaseColor"))
                    data.material.SetColor("_BaseColor", data.originalColor);
                else if (data.material.HasProperty("_Color"))
                    data.material.SetColor("_Color", data.originalColor);
            }
        }
    }
}