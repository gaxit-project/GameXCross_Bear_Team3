using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using System;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class BearController : MonoBehaviour
{
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackInterval = 1.0f;

    private NavMeshAgent _agent;
    private HouseHealth _targetHouse; // 狙っている家

    // 状態管理
    private IDisposable _attackStream;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public void Initialize(float speed)
    {
        _agent.speed = speed;

        // 出現アニメーション
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        FindNextTarget();
        ObserveState();
    }

    private void ObserveState()
    {
        // 移動中の制御
        this.UpdateAsObservable()
            .Where(_ => _targetHouse != null && !_targetHouse.IsDestroyed)
            .Subscribe(_ =>
            {
                Vector3 destination = _targetHouse.HouseCollider.ClosestPoint(transform.position);

                // ターゲットへの距離をチェック
                float dist = Vector3.Distance(transform.position, destination);

                // 攻撃範囲内なら停止、遠ければ移動
                if (dist <= 2.0f) // 少し余裕を持たせる
                {
                    if (!_agent.isStopped) _agent.isStopped = true;
                    StartAttacking();
                }
                else
                {
                    if (_agent.isStopped) _agent.isStopped = false;
                    _agent.SetDestination(destination);
                    StopAttacking();
                }
            })
            .AddTo(this);

        // 家が破壊された場合
        this.UpdateAsObservable()
            .Where(_ => _targetHouse == null || _targetHouse.IsDestroyed)
            .ThrottleFirst(TimeSpan.FromSeconds(1.0f)) // 連続実行を防ぐ(1秒間隔を開ける)
            .Subscribe(_ =>
            {
                StopAttacking();
                FindNextTarget();
            })
            .AddTo(this);
    }

    /// <summary>
    /// 次のターゲットを探す
    /// </summary>
    private void FindNextTarget()
    {
        StopAttacking();

        var houses = GameObject.FindGameObjectsWithTag("House");
        Debug.Log($"タグ'House'がついたオブジェクト数：{houses.Length}");

        if (houses.Length == 0)
        {
            Debug.LogError("家が見つかりません！家に'House'タグがついているか確認してください。");
            return;
        }

        var validTargets = houses
            .Select(h => h.GetComponent<HouseHealth>())
            .Where(h => h != null && !h.IsDestroyed)
            .ToList();
        Debug.Log($"HouseHealth スクリプトがついている生存中の家: {validTargets.Count}軒");

        if (validTargets.Count == 0)
        {
            Debug.LogError("タグはありますが 'HouseHealth' スクリプトがついている家が0軒です。");
            return;
        }

        // 一番近い破壊されていない家を探す
        _targetHouse = validTargets
            .OrderBy(h => Vector3.Distance(transform.position, h.transform.position))
            .FirstOrDefault();

        if (_targetHouse != null)
        {
            Debug.Log($"ターゲット決定: {_targetHouse.name} への移動を開始します。");
            _agent.isStopped = false;
            _agent.SetDestination(_targetHouse.transform.position);
        }
    }

    private void StartAttacking()
    {
        if (_attackStream != null) return; // 攻撃中の場合は何もしない

        // 一定間隔で攻撃する
        _attackStream = Observable.Interval(TimeSpan.FromSeconds(attackInterval))
            .Subscribe(_ =>
            {
                if(_targetHouse == null || _targetHouse.IsDestroyed)
                {
                    StopAttacking();
                    return;
                }

                Vector3 hitPosint = _targetHouse.HouseCollider.ClosestPoint(transform.position);
                float dist = Vector3.Distance(transform.position, hitPosint);

                if (dist > 3.0) return;

                Debug.Log($"攻撃実行中: 対象={_targetHouse.name}, 距離={dist:F2}m (許容範囲:3.0m)");

                // 攻撃アクション
                transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
                _targetHouse.TakeDamage(attackDamage);
            })
            .AddTo(this);
    }

    private void StopAttacking()
    {
        if(_attackStream != null)
        {
            _attackStream.Dispose();
            _attackStream = null;
        }
    }
}