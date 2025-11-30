using UnityEngine;
using UniRx;
using DG.Tweening;
using System;
using NUnit.Framework.Constraints;

[RequireComponent(typeof(Collider))]
public class@HouseHealth : MonoBehaviour
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
        var seq = DOTween.Sequence();
        seq.Append(transform.DOShakePosition(1.0f, 0.5f));
        seq.Append(transform.DOMoveY(-5.0f, 2.0f).SetEase(Ease.InBack));
        seq.Join(transform.DOScale(Vector3.zero, 2.0f));
        seq.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

}