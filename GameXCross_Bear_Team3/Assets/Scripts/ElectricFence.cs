using UnityEngine;
using UniRx;
using UniRx.Triggers;
using DG.Tweening;
using System;

public class ElectricFence : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float damage = 10f;        // ダメージ量
    [SerializeField] private float paralysisDuration = 3.0f; // 麻痺時間
    [SerializeField] private float reloadTime = 5.0f;   // 再度通電するまでのクールダウン

    private bool _isReady = true; // 通電可能か

    private void Start()
    {
        this.OnTriggerEnterAsObservable()
            .Where(_ => _isReady) // 準備完了している場合のみ
            .Subscribe(other =>
            {
                // BearControllerを取得
                if (other.TryGetComponent<BearController>(out var bear))
                {
                    OnBearContact(bear);
                }
            })
            .AddTo(this);
    }

    private void OnBearContact(BearController bear)
    {
        if (bear == null) return;

        // 熊に麻痺とダメージを与える
        bear.ApplyParalysis(paralysisDuration, damage);

        Debug.Log("電気柵: 放電しました！クールダウンに入ります。");

        // クールダウン開始
        EnterCooldown();
    }

    private void EnterCooldown()
    {
        _isReady = false;

        // 指定時間後に復帰 (UniRx)
        Observable.Timer(TimeSpan.FromSeconds(reloadTime))
            .Subscribe(_ =>
            {
                Reactivate();
            })
            .AddTo(this);
    }

    private void Reactivate()
    {
        _isReady = true;
    }
}