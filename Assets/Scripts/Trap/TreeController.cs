using UnityEngine;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;

public class TreeController : MonoBehaviour
{
    [Header("揺れの設定")]
    [SerializeField] private float duration = 0.5f;     // 揺れる時間
    [SerializeField] private float strength = 5f;       // 揺れる強さ（角度）
    [SerializeField] private int vibrato = 10;          // 震える速さ

    [SerializeField] private Vector3 shakeAxis = new Vector3(1f, 0f, 1f);

    private bool _isShaking = false;

    private void Start()
    {
        // 熊が当たった時に反応する
        this.OnCollisionEnterAsObservable()
            .Where(collision => !_isShaking && collision.gameObject.GetComponent<BearController>() != null)
            .Subscribe(_ =>
            {
                ShakeTree();
            })
            .AddTo(this);
    }

    private void ShakeTree()
    {
        _isShaking = true;

        Vector3 strengthVector = new Vector3(strength * shakeAxis.x, strength * shakeAxis.y, strength * shakeAxis.z);

        transform.DOShakeRotation(duration, strengthVector, vibrato, 90f)
            .SetLink(gameObject)
            .OnComplete(() => _isShaking = false);
    }
}