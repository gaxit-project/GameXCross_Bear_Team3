using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using System;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class HunterController : MonoBehaviour
{
    [Header("ハンター設定")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("攻撃設定")]
    [SerializeField] private float attackRange = 15.0f;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float detectionRadius = 20.0f;

    [Header("パトロール設定")]
    [SerializeField] private float patrolRadius = 20f;
    [SerializeField] private float waitTimeAtPatrolPoint = 3.0f; // パトロール地点での待機時間

    [Header("コンポーネント")]
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("デバッグ")]
    [SerializeField] private bool isDebugMode = false; // アニメーションなしのデバッグモード

    private NavMeshAgent _agent;
    private Vector3 _spawnPosition;
    private BearController _targetBear;
    private float _currentHealth;
    private bool _isDead = false;
    private float _patrolWaitTimer = 0f;

    private IDisposable _attackStream;
    private IDisposable _patrolStream;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _currentHealth = maxHealth;
        _spawnPosition = transform.position;

        if (!isDebugMode && animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        StartPatrol();
        ObserveSurroundings();
    }

    private void ObserveSurroundings()
    {
        // 定期的に周囲を探索（ターゲットが見つかるまで）
        Observable.Interval(TimeSpan.FromSeconds(0.5f))
            .Where(_ => !_isDead && _targetBear == null)
            .Subscribe(_ =>
            {
                DetectNearestBear();
            })
            .AddTo(this);

        // 毎フレーム更新
        this.UpdateAsObservable()
            .Where(_ => !_isDead && _targetBear != null)
            .Subscribe(_ =>
            {
                // ターゲットが無効（死亡/破壊）ならパトロールに戻る
                if (_targetBear == null || !_targetBear.isActiveAndEnabled)
                {
                    ReturnToPatrol();
                    return;
                }

                float dist = Vector3.Distance(transform.position, _targetBear.transform.position);

                // 攻撃範囲内なら攻撃
                if (dist <= attackRange)
                {
                    if (!_agent.isStopped) _agent.isStopped = true;
                    transform.LookAt(_targetBear.transform);

                    if (!isDebugMode && animator) animator.SetBool("IsMoving", false);

                    if (_attackStream == null) StartShooting();
                }
                // 範囲外なら追跡
                else
                {
                    if (_agent.isStopped) _agent.isStopped = false;
                    _agent.SetDestination(_targetBear.transform.position);

                    if (!isDebugMode && animator) animator.SetBool("IsMoving", true);

                    StopShooting(); // 射撃停止
                }
            })
            .AddTo(this);
    }

    private void DetectNearestBear()
    {
        // タグ "Bear" のオブジェクトを検索し、BearControllerコンポーネントを取得
        var bears = Physics.OverlapSphere(transform.position, detectionRadius)
            .Select(c => c.GetComponent<BearController>())
            .Where(b => b != null && b.isActiveAndEnabled) // 有効なターゲットのみ
            .OrderBy(b => Vector3.Distance(transform.position, b.transform.position))
            .FirstOrDefault();

        if (bears != null)
        {
            _targetBear = bears;
            StopPatrol();
            Debug.Log("ハンター: クマを発見！攻撃を開始します。");
        }
    }

    private void StartShooting()
    {
        if (_attackStream != null) return;

        _attackStream = Observable.Interval(TimeSpan.FromSeconds(attackInterval))
            .Subscribe(_ =>
            {
                if (_targetBear != null && _targetBear.isActiveAndEnabled)
                {
                    Shoot();
                }
            })
            .AddTo(this);
    }

    private void Shoot()
    {
        if (!isDebugMode && animator) animator.SetTrigger("Attack");
        if (muzzleFlash) muzzleFlash.Play();

        // クマにダメージを与える（attackerとして自分を渡す）
        _targetBear.TakeDamage(damage, this);

        // 攻撃演出（反動）
        transform.DOPunchRotation(new Vector3(-2f, 0, 0), 0.1f);
    }

    private void StopShooting()
    {
        _attackStream?.Dispose();
        _attackStream = null;
    }

    private void StartPatrol()
    {
        if (_patrolStream != null) return;

        _agent.isStopped = false;
        MoveToRandomPoint();
        _patrolWaitTimer = waitTimeAtPatrolPoint;

        _patrolStream = this.UpdateAsObservable()
            .Where(_ => !_isDead && _targetBear == null)
            .Subscribe(_ =>
            {
                if (!isDebugMode && animator) animator.SetBool("IsMoving", _agent.velocity.magnitude > 0.1f);

                if (!_agent.isOnNavMesh || !_agent.isActiveAndEnabled) return;
                
                if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                {
                    // 到着後は設定時間待ってから次のポイントへ
                    if (_patrolWaitTimer > 0f)
                    {
                        _patrolWaitTimer -= Time.deltaTime;
                        return;
                    }

                    MoveToRandomPoint();
                    _patrolWaitTimer = waitTimeAtPatrolPoint;
                }
            })
            .AddTo(this);
    }

    private void MoveToRandomPoint()
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * patrolRadius;
        randomDirection += _spawnPosition;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
        {
            _agent.SetDestination(hit.position);
        }
    }

    private void StopPatrol()
    {
        _patrolStream?.Dispose();
        _patrolStream = null;
    }

    private void ReturnToPatrol()
    {
        Debug.Log("ハンター: ターゲットロスト。パトロールに戻ります。");
        _targetBear = null;
        StopShooting();
        StartPatrol();
    }

    /// <summary>
    /// ダメージを受けた時の処理
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        _currentHealth -= damage;
        transform.DOShakeScale(0.2f, 0.1f); // ダメージ演出

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 死亡しているかどうかを返す
    /// </summary>
    public bool IsDead()
    {
        return _isDead;
    }

    private void Die()
    {
        _isDead = true;
        _agent.enabled = false;
        StopShooting();
        StopPatrol();

        if (!isDebugMode && animator) animator.SetTrigger("Die");

        Debug.Log("ハンター: 死亡しました。");

        GetComponent<Collider>().enabled = false;

        // 横に倒れて消えるアニメーション
        Vector3 currentRotation = transform.eulerAngles;
        Vector3 fallRotation = new Vector3(currentRotation.x + 90f, currentRotation.y, currentRotation.z);
        
        Sequence deathSequence = DOTween.Sequence();
        // 横に倒れる（0.5秒）
        deathSequence.Append(transform.DORotate(fallRotation, 0.5f).SetEase(Ease.OutQuad));
        // 少し沈む（0.3秒）
        deathSequence.Join(transform.DOMoveY(transform.position.y - 0.5f, 0.5f).SetEase(Ease.InQuad));
        // スケールを0にして消える（0.3秒）
        deathSequence.Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InQuad));
        // アニメーション完了後に削除
        deathSequence.OnComplete(() => Destroy(gameObject));
    }
}
