using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using System;
using System.Linq;
using Unity.Cinemachine;

[RequireComponent(typeof(NavMeshAgent))]
public class BearController : MonoBehaviour
{
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackInterval = 1.0f;

    // 判定基準点
    [SerializeField] private Transform detectionPoint;

    [SerializeField] private bool enableAnimation = true;

    [SerializeField] private Animator animator;

    private NavMeshAgent _agent;
    private HouseHealth _targetHouse; // 狙っている家

    private Rigidbody _rb;

    // 状態管理
    private IDisposable _attackStream;
    private bool _isTrapped = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        _agent.stoppingDistance = 0f;

        if (detectionPoint == null) detectionPoint = transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (!enableAnimation && animator != null) animator.enabled = false;
    }

    public void Initialize(float speed)
    {
        _agent.enabled = true;
        _agent.speed = speed;
        _isTrapped = false;

        Vector3 targetScale = transform.localScale;

        // 出現アニメーション
        transform.localScale = Vector3.one * 0.1f;
        transform.DOScale(targetScale, 0.5f).SetEase(Ease.OutBack);

        FindNextTarget();
        ObserveState();
        ObserveCollision();
    }

    /// <summary>
    /// 罠にかかった時の処理
    /// </summary>
    public void OnTrapped(Vector3 trapCenterPosition)
    {
        if (_isTrapped) return;

        _isTrapped = true;
        _agent.enabled = false;
        StopAttacking();

        if (animator && enableAnimation) animator.speed = 0;

        Vector3 finalPosition = new Vector3(
            trapCenterPosition.x,
            trapCenterPosition.y,
            trapCenterPosition.z
        );

        transform.position = trapCenterPosition;
        transform.rotation = Quaternion.identity;

        Debug.Log($"{name}が罠にかかった。");

        transform.DOShakeScale(0.5f, 0.5f);
    }

    public void OnAttackHit()
    {
        if (_targetHouse == null || _targetHouse.IsDestroyed) return;

        Vector3 hitPoint = _targetHouse.HouseCollider.ClosestPoint(detectionPoint.position);
        if (Vector3.Distance(detectionPoint.position, hitPoint) > 7.0f) return;

        Debug.Log("攻撃ヒット");

        _targetHouse.TakeDamage(attackDamage);

        //transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
    }

    private void ObserveCollision()
    {
        this.OnTriggerEnterAsObservable()
            .Subscribe(other =>
            {
                if (other.TryGetComponent<Fence>(out var fence))
                {
                    Debug.Log("フェンスを破壊しました");
                    fence.FenceBreak();

                    transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
                }
            })
            .AddTo(this);
    }

    private void ObserveState()
    {
        // 移動中の制御
        this.UpdateAsObservable()
            .Where(_ => _targetHouse != null && !_targetHouse.IsDestroyed && !_isTrapped)
            .Subscribe(_ =>
            {
                Vector3 destination = _targetHouse.HouseCollider.ClosestPoint(transform.position);

                // ターゲットへの距離をチェック
                float dist = Vector3.Distance(detectionPoint.position, _targetHouse.HouseCollider.ClosestPoint(detectionPoint.position)); ;

                if (animator && enableAnimation) animator.SetBool("IsMoving", dist > 5.0f);

                // 攻撃範囲内なら停止、遠ければ移動
                if (dist <= 5.0f) // 少し余裕を持たせる
                {
                    if (!_agent.isStopped) _agent.isStopped = true;
                    StartAttacking();
                }
                else
                {
                    StopAttacking();
                    if (_agent.isStopped) _agent.isStopped = false;

                    if(Vector3.Distance(_agent.destination, destination) > 1.0f)
                    {
                        _agent.SetDestination(destination);
                    }
                    
                }
            })
            .AddTo(this);

        // 家が破壊された場合
        this.UpdateAsObservable()
            .Where(_ => _targetHouse == null || _targetHouse.IsDestroyed && !_isTrapped)
            .ThrottleFirst(TimeSpan.FromSeconds(1.0f)) // 連続実行を防ぐ(1秒間隔を開ける)
            .Subscribe(_ =>
            {
                StopAttacking();
                if (animator && enableAnimation) animator.SetBool("IsMoving", false);
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
            .OrderBy(h => Vector3.Distance(detectionPoint.position, h.HouseCollider.ClosestPoint(detectionPoint.position)))
            .FirstOrDefault();

        if (_targetHouse != null)
        {
            Debug.Log($"ターゲット決定: {_targetHouse.name} への移動を開始します。");
            _agent.isStopped = false;
            Vector3 targetPos = _targetHouse.HouseCollider.ClosestPoint(transform.position);
            _agent.SetDestination(targetPos);
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

                Vector3 hitPosint = _targetHouse.HouseCollider.ClosestPoint(detectionPoint.position);
                float dist = Vector3.Distance(detectionPoint.position, hitPosint);

                if (dist > 6.0f) return;

                if (animator != null && enableAnimation) animator.SetTrigger("Attack");

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