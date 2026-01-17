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
    // 武器タイプ定義
    public enum WeaponType
    {
        None,      // 武器なし
        Rifle,     // ライフル
        Revolver   // リボルバー
    }

    [Header("ハンター設定")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("攻撃設定")]
    [SerializeField] private float attackRange = 15.0f;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float detectionRadius = 20.0f;

    [Header("武器設定")]
    [SerializeField] private WeaponType currentWeapon = WeaponType.None; // 初期状態は武器なし
    [SerializeField] private float rifleAttackInterval = 1.5f;
    [SerializeField] private float revolverAttackInterval = 0.8f;

    [Header("パトロール設定")]
    [SerializeField] private float patrolRadius = 20f;
    [SerializeField] private float waitTimeAtPatrolPoint = 3.0f; // パトロール地点での待機時間

    [Header("コンポーネント")]
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private LayerMask bearLayer;

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
    private IDisposable _observeStream; // 監視ストリームを追跡

    private WeaponType _equippedWeapon = WeaponType.None;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = attackRange * 0.2f;

        // ★ NavMeshAgent に移動・回転を任せる
        _agent.updatePosition = true;
        _agent.updateRotation = true;

        _currentHealth = maxHealth;
        _spawnPosition = transform.position;
        _equippedWeapon = currentWeapon; // 初期武器を設定

        if (!isDebugMode && animator == null) animator = GetComponentInChildren<Animator>();

        Debug.Log($"ハンター({gameObject.name}): Awake完了");
    }

    private void Start()
    {
        Debug.Log($"ハンター({gameObject.name}): Start開始");
        Initialize();
    }

    /// <summary>
    /// ハンターを初期化して動作を開始する
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"ハンター({gameObject.name}): Initialize開始 - CurrentState = {GameManager.Instance?.CurrentState.Value}");

        // エージェントが無効化されている場合は有効化
        if (_agent != null)
        {
            _agent.enabled = true;
        }

        // 初期武器を装備
        EquipWeapon(WeaponType.Rifle);

        // GameManagerの状態を監視し、フェーズに合わせて挙動を制御
        if (GameManager.Instance != null)
        {
            // 現在の状態を確認
            GameState currentState = GameManager.Instance.CurrentState.Value;

            // Setup フェーズなら待機状態にする
            if (currentState == GameState.Setup)
            {
                Debug.Log("ハンター: 準備フェーズのため待機します。");
                StopMovement();
            }
            // Battleフェーズなら即座に行動開始
            else if (currentState == GameState.Battle)
            {
                Debug.Log("ハンター: バトルフェーズ中に配置されました。行動を開始します。");
                if (_agent != null && !_agent.enabled)
                {
                    _agent.enabled = true;
                }
                // 監視を開始
                ObserveSurroundings();
            }

            // 状態変更を監視
            GameManager.Instance.CurrentState
                .Subscribe(state =>
                {
                    Debug.Log($"ハンター({gameObject.name}): GameState変更 -> {state}");
                    
                    if (state == GameState.Setup)
                    {
                        Debug.Log("ハンター: 準備フェーズ。待機します。");
                        ReturnToWait(); // ターゲットを解除し、パスをクリアして停止
                        // 監視ストリームをクリーンアップ
                        CleanupObserveStream();
                    }
                    else if (state == GameState.Battle)
                    {
                        Debug.Log("ハンター: バトルフェーズ開始。行動を開始します。");
                        // バトル開始時にエージェントを明示的に有効化
                        if (_agent != null && !_agent.enabled)
                        {
                            _agent.enabled = true;
                        }
                        // 監視が始まっていなければ開始
                        if (_observeStream == null)
                        {
                            ObserveSurroundings();
                        }
                    }
                })
                .AddTo(this);
        }
        else
        {
            Debug.LogWarning("ハンター: GameManagerが見つかりません！");
        }
    }

    /// <summary>
    /// 監視ストリームをクリーンアップする
    /// </summary>
    private void CleanupObserveStream()
    {
        if (_observeStream != null)
        {
            _observeStream.Dispose();
            _observeStream = null;
            Debug.Log($"ハンター({gameObject.name}): 監視ストリームをクリーンアップしました。");
        }
    }

    private void ObserveSurroundings()
    {
        // 既に監視が始まっている場合は重複を防ぐ
        if (_observeStream != null)
        {
            Debug.LogWarning("ハンター: ObserveSurroundings は既に実行中です。");
            return;
        }

        Debug.Log($"ハンター({gameObject.name}): ObserveSurroundings 開始");

        CompositeDisposable disposables = new CompositeDisposable();

        // 準備フェーズ中は強制停止ロック
        this.UpdateAsObservable()
        .Where(_ => GameManager.Instance != null && GameManager.Instance.CurrentState.Value == GameState.Setup)
        .Subscribe(_ =>
        {
            if (_agent.hasPath || _agent.velocity.sqrMagnitude > 0.01f)
            {
                StopMovement(); // 準備中は毎フレーム停止を保証
            }
        })
        .AddTo(disposables);

        // 定期的に周囲を探索（ターゲットが見つかるまで）
        Observable.Interval(TimeSpan.FromSeconds(0.5f))
            .Where(_ => !_isDead && _targetBear == null)
            .Where(_ => GameManager.Instance != null && GameManager.Instance.CurrentState.Value == GameState.Battle)
            .Subscribe(_ =>
            {
                DetectNearestBear();
            })
            .AddTo(disposables);

        // 毎フレーム更新
        this.UpdateAsObservable()
            .Where(_ => !_isDead)
            .Where(_ => GameManager.Instance != null && GameManager.Instance.CurrentState.Value == GameState.Battle)
            .Subscribe(_ =>
            {
                // ターゲットが有効な場合の処理
                if (_targetBear != null && _targetBear.isActiveAndEnabled && !_targetBear.IsDead)
                {
                    float dist = Vector3.Distance(transform.position, _targetBear.transform.position);

                    // 攻撃範囲内なら攻撃（構え＋停止）
                    if (dist <= attackRange)
                    {
                        StopMovement();
                        transform.LookAt(new Vector3(_targetBear.transform.position.x, transform.position.y, _targetBear.transform.position.z));

                        if (!isDebugMode && animator)
                        {
                            animator.SetBool("IsRifle", _equippedWeapon == WeaponType.Rifle);
                            animator.SetBool("IsWalking", false);
                        }

                        // ★ 武器がある場合のみ射撃開始
                        if (_equippedWeapon != WeaponType.None && _attackStream == null)
                        {
                            StartShooting();
                        }

                        // クマが極めて近い場合のみ後退（条件を厳しくする）
                        // 3.0f 以下の時だけ後退
                        if (dist < 3.0f)  // attackRange * 0.3f から 3.0f に変更
                        {
                            Vector3 awayDirection = (transform.position - _targetBear.transform.position).normalized;
                            Vector3 backupTarget = transform.position + awayDirection * 2.0f;  // 後退距離を減らす
                            _agent.isStopped = false;
                            _agent.SetDestination(backupTarget);
                        }
                    }
                    // 範囲外なら追跡（歩く＋構え解除）
                    else
                    {
                        _agent.isStopped = false;
                        Vector3 directionToBear = (_targetBear.transform.position - transform.position).normalized;
                        Vector3 stoppingPoint = _targetBear.transform.position - directionToBear * (attackRange * 0.8f);

                        _agent.SetDestination(stoppingPoint);

                        if (!isDebugMode && animator)
                        {
                            animator.SetBool("IsRifle", false);
                            animator.SetBool("IsWalking", _agent.velocity.magnitude > 0.1f);
                        }

                        StopShooting();
                    }
                }
                // ターゲットが無効（死亡/破壊）なら待機状態に
                else if (_targetBear != null && (_targetBear.IsDead || !_targetBear.isActiveAndEnabled))
                {
                    ReturnToWait();
                }
                else
                {
                    // ターゲットなし：アイドル
                    if (!isDebugMode && animator)
                    {
                        animator.SetBool("IsWalking", false);
                        animator.SetBool("IsRifle", false);
                    }
                }
            })
            .AddTo(disposables);

        // CompositeDisposableを_observeStreamとして保持
        _observeStream = disposables;
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
            StartShooting();  // クマ発見時に即座に攻撃開始
            Debug.Log("ハンター: クマを発見！攻撃を開始します。");
        }
    }

    private void StartShooting()
    {
        if (_attackStream != null || _equippedWeapon == WeaponType.None) return;

        // ★ 武器に応じて射撃間隔を変更
        float interval = _equippedWeapon == WeaponType.Rifle ? rifleAttackInterval : revolverAttackInterval;

        _attackStream = Observable.Interval(TimeSpan.FromSeconds(interval))
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
        if (!isDebugMode && animator) animator.SetTrigger("Fire");
        if (muzzleFlash) muzzleFlash.Play();

        if (_targetBear != null)
        {
            Debug.Log($"ハンター({gameObject.name}) -> 熊({_targetBear.name}) : {damage} ダメージ");
            _targetBear.TakeDamage(damage, this);
        }

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
            .Where(_ => GameManager.Instance != null && GameManager.Instance.CurrentState.Value == GameState.Battle)
            .Subscribe(_ =>
            {
                // 修正: Animatorのパラメータ名をIsWalkingに統一
                if (!isDebugMode && animator) animator.SetBool("IsWalking", _agent.velocity.magnitude > 0.1f);

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

    private void ReturnToWait()
    {
        Debug.Log("ハンター: ターゲットロスト。その場で待機します。");
        _targetBear = null;
        StopShooting();
        StopPatrol();

        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        if (!isDebugMode && animator)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRifle", false);
        }
    }

    private void StopMovement()
    {
        Debug.Log($"ハンター({gameObject.name}): StopMovement呼び出し");
        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }
        if (!isDebugMode && animator)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRifle", false);
        }
    }

    /// <summary>
    /// ダメージを受けた時の処理
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        _currentHealth -= damage;
        transform.DOShakeScale(0.2f, 0.1f); // ダメージ演出
        Debug.Log($"ハンター: ダメージを受けた！ 残りHP: {_currentHealth}");

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

        PublicOpinionManager.Instance.POchanging(-0.5f);

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

    /// <summary>
    /// 武器を切り替える
    /// </summary>
    public void EquipWeapon(WeaponType weaponType)
    {
        _equippedWeapon = weaponType;
        Debug.Log($"ハンター: {weaponType}に切り替えました。");

        // 既に射撃中なら射撃間隔を更新
        if (_attackStream != null && _targetBear != null)
        {
            StopShooting();
            StartShooting();
        }
    }

    /// <summary>
    /// 現在装備している武器を取得
    /// </summary>
    public WeaponType GetEquippedWeapon()
    {
        return _equippedWeapon;
    }
}
