using UnityEngine;
using UniRx;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class HouseHealth : MonoBehaviour
{
    [SerializeField] private float maxHp = 100f;
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
            // GameManagerに家の破壊を報告
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReportHouseDestroyed(this);
        }

        transform.DOKill();
        transform.SetParent(null);

        var seq = DOTween.Sequence();
        seq.Append(transform.DOShakePosition(1.0f, 0.5f));
        seq.Append(transform.DOMoveY(-5.0f, 2.0f).SetRelative(true).SetEase(Ease.InBack));
        seq.Join(transform.DOScale(Vector3.zero, 2.0f));
        seq.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

}