using UnityEngine;
using UnityEngine.UI;

public class HouseHealth_Gauge : MonoBehaviour
{
    [SerializeField] private float gaugevalue=1;
    [SerializeField] private float gaugespeed;
    [SerializeField] private Image image;
    [SerializeField] public float health;
    [SerializeField] public float health_Max;

    public Transform target;
    public RectTransform uiRectTransform;
    public Camera mainCamera;

    [Header("スケール調整の設定")]
    public float baseDistance = 10f; // UIが通常の大きさ（Scale = 1）になる基準の距離
    public float minScale = 0.3f;    // 遠くに行ったときの最小サイズ
    public float maxScale = 2.0f;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        gaugevalue = Mathf.Lerp(gaugevalue,(float)(health/health_Max),gaugespeed*Time.deltaTime);
        image.fillAmount = gaugevalue;
        if(image.fillAmount <= 0.01)
        {
            Destroy(this);
        }
    }
    void LateUpdate()
    {
        if (target != null)
        {
            // --- 1. 座標の更新 ---
            Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);

            // 画面の前にいる場合のみ処理
            if (screenPos.z > 0)
            {
                uiRectTransform.position = screenPos;

                // --- 2. 距離によるスケールの更新 ---
                // カメラとターゲットの実際の距離を計算
                float distance = Vector3.Distance(target.position, mainCamera.transform.position);

                // 距離に応じてスケールを計算 (基準距離 ÷ 現在の距離)
                // ※遠くなるほど scale の値が小さくなります
                float scale = baseDistance / distance;

                // 大きくなりすぎたり、小さくなりすぎたりするのを防ぐ
                scale = Mathf.Clamp(scale, minScale, maxScale);

                // UIのスケールに適用
                uiRectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
