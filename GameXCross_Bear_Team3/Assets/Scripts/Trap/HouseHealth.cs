using UnityEngine;
using UniRx;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class HouseHealth : MonoBehaviour
{
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private GameObject ruinsPrefab; // 崩壊後に配置するプリハブ
    [SerializeField] private float shakeIntensity = 0.3f; // 揺れの強さ
    [SerializeField] private float shakeDuration = 1.5f; // 揺れの時間
    [SerializeField] private float ruinsYPosition = 3f; // 廃墟を配置するY座標
    
    public FloatReactiveProperty CurrentHp { get; private set; }

    public bool IsDestroyed => CurrentHp.Value <= 0f;

    public Collider HouseCollider { get; private set; }

    private void Awake()
    {
        CurrentHp = new FloatReactiveProperty(maxHp);

        HouseCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        // GameManagerに家を登録
        GameManager.Instance.RegisterHouse(this);
    }

    public void TakeDamage(float amount)
    {
        if(IsDestroyed) return;

        CurrentHp.Value -= amount;

        transform.DOShakePosition(0.2f, 0.5f);

        if (IsDestroyed)
        {
            Collapse();
        }
    }

    private void Collapse()
    {
        // GameManagerに家の破壊を報告（ターゲットから除外）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReportHouseDestroyed(this);
        }

        transform.DOKill();
        transform.SetParent(null);

        var seq = DOTween.Sequence();
        // seq.Append(transform.DOShakePosition(1.0f, 0.5f));
        // seq.Append(transform.DOMoveY(-5.0f, 2.0f).SetRelative(true).SetEase(Ease.InBack));
        // seq.Join(transform.DOScale(Vector3.zero, 2.0f));

        // ステップ1: ガタガタと震えながら縮小
        seq.Append(
            transform.DOShakePosition(shakeDuration, new Vector3(shakeIntensity, 0, shakeIntensity), 20, 90f, snapping: true, fadeOut: false)
            .SetEase(Ease.InQuad)
        );
        // ステップ2: 同時にスケールを0＆下に落ちる
        // seq.Join(transform.DOScale(Vector3.zero, shakeDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DOMoveY(-50.0f, shakeDuration).SetEase(Ease.InBack));
        seq.OnComplete(() =>
        {
            // 崩壊後のオブジェクトを配置
            if (ruinsPrefab != null)
            {
                // 指定したY座標に廃墟を配置
                Vector3 ruinsPosition = new Vector3(transform.position.x, ruinsYPosition, transform.position.z);
                var ruins = Instantiate(ruinsPrefab, ruinsPosition, transform.rotation);
                
                // 廃墟にコライダーを追加（障害物として機能）
                if (!ruins.GetComponent<Collider>())
                {
                    ruins.AddComponent<BoxCollider>();
                }
                
                // 廃墟にはHouseHealthコンポーネントを付けない（熊が狙わない）
                ruins.tag = "Untagged";
            }
            
            Destroy(gameObject);
        });
    }
}