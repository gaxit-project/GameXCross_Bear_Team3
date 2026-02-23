using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Fence : MonoBehaviour
{
    private bool _isBroken = false;

    public void FenceBreak()
    {
        if (_isBroken) return;
        _isBroken = true;

        GetComponent<Collider>().enabled = false;

        var seq = DOTween.Sequence();
        seq.Append(transform.DOShakeRotation(0.5f, 30f));
        seq.Join(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack));
        seq.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}
