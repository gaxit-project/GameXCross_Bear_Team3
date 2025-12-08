using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using System;
using System.Linq;
// using UnityEditor; // ビルド時にエラーになる可能性があるためコメントアウト

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class BearController : MonoBehaviour
{
    [Header("ステータス")]
    [SerializeField] private float maxHealth = 100f; // HP初期値
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private float attackRange = 5.0f;

    [Header("参照")]
    [SerializeField] private Transform detectionPoint;
    [SerializeField] private bool enableAnimation = true;
    [SerializeField] private Animator animator;

    private NavMeshAgent _agent;
    private Rigidbody _rb;

    // ターゲット
    private HouseHealth _targetHouse; // ターゲットの家
    private HunterController _targetHunter; // ターゲットのハンター
    private float _currentHealth;

    // 状態管理
    private IDisposable _attackStream;
    private bool _isTrapped = false;
    private bool _isDead = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        _agent.stoppingDistance = 0f;
        _currentHealth = maxHealth; // 初期化

        if (detectionPoint == null) detectionPoint = transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (!enableAnimation && animator != null) animator.enabled = false;
    }

    public void Initialize(float speed)
    {
        _agent.enabled = true;
        _agent.speed = speed;
        _agent.updateRotation = true;
        _isTrapped = false;
        _isDead = false; // 初期化
        _currentHealth = maxHealth; // 初期化

        Vector3 targetScale = transform.localScale;

        // 出現アニメーション
        transform.localScale = Vector3.one * 0.1f;
        transform.DOScale(targetScale, 0.5f).SetEase(Ease.OutBack);

        FindNextTarget();
        ObserveState();
        ObserveCollision();
    }

    /// <summary>
    /// ハンターからダメージを受けた際の処理
    /// </summary>
    public void TakeDamage(float damage, HunterController attacker)
    {
        if (_isDead) return;

        _currentHealth -= damage;
        transform.DOPunchScale(Vector3.one * -0.1f, 0.2f); // ダメージ演出

        if (_currentHealth <= 0)
        {
            Die();
            return;
        }

        // 罠にかかっておらず、かつターゲットが現在のハンターでない場合
        if (!_isTrapped && attacker != null && _targetHunter != attacker)
        {
            Debug.Log("熊: 攻撃を受けた！ターゲットをハンターに変更します。");

            _targetHunter = attacker; // ターゲットをハンターに変更
            _targetHouse = null;      // 家へのターゲットを解除

            StopAttacking();
            if (_agent.isActiveAndEnabled) _agent.isStopped = false;
        }
    }

    private void Die()
    {
        _isDead = true;
        _agent.enabled = false;
        StopAttacking();
        if (animator && enableAnimation) animator.SetTrigger("Die");
        Debug.Log("熊: 死亡しました。");

        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 3.0f);
    }

    public void OnTrapped(Vector3 trapCenterPosition)
    {
        if (_isTrapped || _isDead) return;

        _isTrapped = true;
        _agent.enabled = false;
        StopAttacking();

        if (animator && enableAnimation) animator.speed = 0;

        transform.position = trapCenterPosition;
        transform.rotation = Quaternion.identity;

        Debug.Log($"{name}が罠にかかりました。");
        transform.DOShakeScale(0.5f, 0.5f);
    }

    // アニメーションイベントから呼ばれる攻撃処理
    public void OnAttackHit()
    {
        // ハンターへの攻撃
        if (_targetHunter != null)
        {
            float dist = Vector3.Distance(detectionPoint.position, _targetHunter.transform.position);
            // 距離の誤差許容
            if (dist < attackRange + 2.0f)
            {
                Debug.Log("熊: ハンターへ攻撃ヒット");
                _targetHunter.TakeDamage(attackDamage);
            }
            return; // ハンター優先
        }

        // 家への攻撃
        if (_targetHouse != null && !_targetHouse.IsDestroyed)
        {
            Vector3 hitPoint = _targetHouse.HouseCollider.ClosestPoint(detectionPoint.position);
            float dist = Vector3.Distance(detectionPoint.position, hitPoint);

            if (dist < attackRange + 3.0f)
            {
                Debug.Log("熊: 家へ攻撃ヒット");
                _targetHouse.TakeDamage(attackDamage);
            }
        }
    }

    private void ObserveCollision()
    {
        this.OnTriggerEnterAsObservable()
            .Where(_ => !_isDead) // 死んでいない場合のみ
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
        this.UpdateAsObservable()
            .Where(_ => !_isTrapped && !_isDead)
            .Subscribe(_ =>
            {
                // 優先度1: ハンターをターゲットしている場合
                if (_targetHunter != null)
                {
                    // ハンターが死んだ、または無効になったらターゲット解除して次を探す
                    if (_targetHunter == null || !_targetHunter.gameObject.activeSelf)
                    {
                        _targetHunter = null;
                        FindNextTarget();
                        return;
                    }

                    HandleHunterTarget();
                }
                // 優先度2: 家をターゲットしている場合
                else if (_targetHouse != null && !_targetHouse.IsDestroyed)
                {
                    HandleHouseTarget();
                }
                // 優先度3: ターゲットがない場合
                else
                {
                    FindNextTarget();
                }
            })
            .AddTo(this);
    }

    // ハンターに対する挙動
    private void HandleHunterTarget()
    {
        float dist = Vector3.Distance(detectionPoint.position, _targetHunter.transform.position);

        if (animator && enableAnimation) animator.SetBool("IsMoving", dist > attackRange - 1.0f);

        // 攻撃範囲内
        if (dist <= attackRange)
        {
            if (!_agent.isStopped)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
            _agent.updateRotation = false;

            // ハンターの方を向く
            Vector3 lookTarget = _targetHunter.transform.position;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget);

            StartAttacking();
        }
        else
        {
            // 追跡
            StopAttacking();
            if (_agent.isStopped) _agent.isStopped = false;
            _agent.updateRotation = true;
            _agent.SetDestination(_targetHunter.transform.position);
        }
    }

    // 家に対する挙動
    private void HandleHouseTarget()
    {
        Vector3 destination = _targetHouse.HouseCollider.ClosestPoint(transform.position);
        float dist = Vector3.Distance(detectionPoint.position, destination);

        if (animator && enableAnimation) animator.SetBool("IsMoving", dist > 5.0f);

        float stopThreshold = attackRange - 1.5f;
        if (stopThreshold < 1.0f) stopThreshold = 1.0f;

        if (dist <= stopThreshold)
        {
            if (!_agent.isStopped)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
            _agent.updateRotation = false;

            Vector3 lookTarget = destination;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget);

            StartAttacking();
        }
        else
        {
            bool isAttackingAndInRange = (_attackStream != null && dist <= attackRange);
            if (isAttackingAndInRange)
            {
                StopAttacking();
                if (_agent.isStopped) _agent.isStopped = false;
                if (Vector3.Distance(_agent.destination, destination) > 1.0f)
                {
                    _agent.SetDestination(destination);
                }
            }
        }
    }

    private void FindNextTarget()
    {
        StopAttacking();
        _targetHunter = null; // ハンターゲット解除

        var houses = GameObject.FindGameObjectsWithTag("House");
        if (houses.Length == 0) return;

        var validTargets = houses
            .Select(h => h.GetComponent<HouseHealth>())
            .Where(h => h != null && !h.IsDestroyed)
            .ToList();

        if (validTargets.Count == 0) return;

        // 一番近い壊れていない家を探す
        _targetHouse = validTargets
            .OrderBy(h => Vector3.Distance(detectionPoint.position, h.HouseCollider.ClosestPoint(detectionPoint.position)))
            .FirstOrDefault();

        if (_targetHouse != null)
        {
            _agent.isStopped = false;
            Vector3 targetPos = _targetHouse.HouseCollider.ClosestPoint(transform.position);
            _agent.SetDestination(targetPos);
        }
    }

    private void StartAttacking()
    {
        if (_attackStream != null) return;

        _attackStream = Observable.Interval(TimeSpan.FromSeconds(attackInterval))
            .Subscribe(_ =>
            {
                // ハンター・家、どちらを狙っている場合でも、OnAttackHitで判定してダメージを与える
                // アニメーションがある場合はTriggerセット
                if (animator != null && enableAnimation) animator.SetTrigger("Attack");

                // 攻撃演出
                transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);

                // アニメーションイベントを使わない場合はここで直接呼ぶ
                OnAttackHit();
            })
            .AddTo(this);
    }

    private void StopAttacking()
    {
        if (_attackStream != null)
        {
            _attackStream.Dispose();
            _attackStream = null;
        }
    }
}
