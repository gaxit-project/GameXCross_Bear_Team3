using DG.Tweening;
using NUnit.Framework.Internal;
using UniRx;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HouseHealth : MonoBehaviour
{
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private GameObject ruinsPrefab; // 崩壊後に配置するプリハブ
    [SerializeField] private float shakeIntensity = 0.3f; // 揺れの強さ
    [SerializeField] private float shakeDuration = 1.5f; // 揺れの時間
    [SerializeField] private float ruinsYPosition = 3f; // 廃墟を配置するY座標

    [SerializeField] private GameObject DustStorm;
    [SerializeField] private GameObject HealthGauge;//参照元
    private HouseHealth_Gauge HouseHealth_Gauge_this;
    private GameObject HealthGauge_this;
    private int DamageCount=0;
    public FloatReactiveProperty CurrentHealth { get; private set; }

    public bool IsDestroyed => CurrentHealth.Value <= 0f;

    public Collider HouseCollider { get; private set; }
    private void Awake()
    {
        HouseCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        if(GameManager.Instance != null && GameManager.Instance.Balance != null)
        {
            this.maxHp = GameManager.Instance.Balance.houseMaxHp;
        }
        CurrentHealth = new FloatReactiveProperty(maxHp);
        // GameManagerに家を登録
        GameManager.Instance.RegisterHouse(this);
    }

    public void TakeDamage(float amount)
    {
        if(IsDestroyed) return;

        CurrentHealth.Value -= amount;

        if (DamageCount == 0)
        {
            HealthGauge_this = Instantiate(HealthGauge, this.transform.position, this.transform.rotation);//ゲージを生み出す
            HealthGauge_this.SetActive(true);
            HouseHealth_Gauge_this = HealthGauge_this.GetComponent<HouseHealth_Gauge>();//生み出したゲージからゲージについてのスクリプトを取得
            HouseHealth_Gauge_this.health_Max = maxHp;
            HouseHealth_Gauge_this.target = this.transform;
        }
        HouseHealth_Gauge_this.health = CurrentHealth.Value;//ゲージに今の体力を渡す。

        DamageCount++;//もう一度生成しないための制御

        transform.DOShakePosition(0.2f, 2.0f).SetDelay(0.8f);

        if (IsDestroyed)
        {
            Collapse();
        }
    }

    private void Collapse()
    {
        SEmanager.Instance.Play("collapse");

        GameObject Dust = Instantiate(DustStorm, transform.position, transform.rotation * Quaternion.Euler(-90, 0, 0));
        Dust.SetActive(true);
        var ps = Dust.GetComponent<ParticleSystem>();

        //①　先にカウントを増やす
        KillCountManager.Instance?.DamageCounterplus();
        
        //② GameManagerに家の破壊を報告（ターゲットから除外）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReportHouseDestroyed(this);
        }
        
        transform.DOKill();
        transform.SetParent(null);

        var seq = DOTween.Sequence().SetDelay(0.8f);
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
            if (Dust != null)
            {
                ps.Stop(); // まだ生きていれば、止まれと命令
            }
            Destroy(gameObject);
        });
    }
}