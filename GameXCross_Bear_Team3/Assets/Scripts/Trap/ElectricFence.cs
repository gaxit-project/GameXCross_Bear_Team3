using UnityEngine;
using UniRx;
using UniRx.Triggers;
using DG.Tweening;

public class ElectricFence : MonoBehaviour
{
    [Header("設定")]
    private float damage = 10f;        // ダメージ量
    private float paralysisDuration = 3.0f; // 麻痺時間
    private Collider _collider;

    private bool _isBroken = false;


    private void Start()
    {
        _collider = GetComponent<Collider>();

        if(GameManager.Instance != null && GameManager.Instance.Balance != null)
        {
            var balance = GameManager.Instance.Balance;
            this.damage = balance.fenceDamage;
            this.paralysisDuration = balance.fenceParalysisDuration;
        }

        this.OnTriggerEnterAsObservable()
            .Select(other => other.GetComponent<TrapTarget>())
            .Where(target => target != null && !_isBroken)
            .Subscribe(target =>
            {
                Debug.Log("電気柵: 感電作動！");
                target.ApplyStun(paralysisDuration, damage);
                BreakFence();
            });
    }

    //private void OnBearTouch(BearController bear)
    //{
    //    if (_isBroken) return;
    //    _isBroken = true; // 二重発動防止

    //    // 1. 熊への作用（麻痺 ＆ ダメージ）
    //    bear.ApplyParalysis(paralysisDuration, damage);

    //    // 2. 柵の破壊処理
    //    BreakFence();
    //}

    private void BreakFence()
    {
        // コライダーを無効化（これ以上当たり判定が発生しないように）
        if (_collider != null) _collider.enabled = false;

        var seq = DOTween.Sequence();
        seq.Append(transform.DOShakeRotation(0.5f, 30f));
        seq.Join(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));

        // アニメーション完了後にGameObjectを削除
        seq.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}