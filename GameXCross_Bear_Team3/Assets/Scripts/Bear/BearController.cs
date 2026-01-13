using DG.Tweening;
using System;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class BearController : MonoBehaviour, TrapTarget
{
    [Header("ステータス")]
    [SerializeField] private float maxHealth = 100f; // HP初期値
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private float attackRange = 5.0f;
    [SerializeField] private float detectionRadius = 15.0f; // ハンター検出範囲
    [SerializeField] private int captureReward = 30000; // 捕獲時の報酬
    [SerializeField] private int defeatReward = 30000;  // 倒した時の報酬

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
    private GameObject _assignedTrap; // 捕まった檻を保存する変数
    private bool _isTrapped = false;
    private bool _isDead = false;
    private bool _isRewardProcessed = false; // 報酬支払い済みフラグ

    // public bool IsDead => _currentHealth <= 0; // 死亡判定プロパティ

    public bool IsDead => _isDead;

    public bool IsParalyzed => _isTrapped; // 麻痺・行動不能状態であるかを判定する

    private void Awake()
    {
        Debug.Log("[Bear Awake] START");
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        // Rigidbodyをkinematicに設定（NavMeshAgentとの競合を防ぐ）
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            Debug.Log($"[Bear Awake] Rigidbody configured: isKinematic={_rb.isKinematic}");
        }

        _agent.stoppingDistance = attackRange - 0.5f;
        _currentHealth = maxHealth; // 初期化

        if (detectionPoint == null) detectionPoint = transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // NavMeshAgentとアニメーターの競合を防ぐ
        if (animator != null)
        {
            animator.applyRootMotion = false; // Root Motionを無効化
            animator.updateMode = AnimatorUpdateMode.Normal;
            Debug.Log($"[Bear Awake] Animator configured: applyRootMotion={animator.applyRootMotion}, updateMode={animator.updateMode}, enabled={animator.enabled}");
            if (!enableAnimation) animator.enabled = false;
        }

        // NavMeshAgentの設定
        _agent.updatePosition = true;  // NavMeshAgentが位置を制御
        _agent.updateRotation = true;  // NavMeshAgentが回転を制御
        
        Debug.Log($"[Bear Awake] END - Agent enabled: {_agent.enabled}, isOnNavMesh: {_agent.isOnNavMesh}");
    }

    // 初期状態
    public void Initialize(float speed)
    {
        _agent.enabled = true;
        _agent.speed = speed;
        _agent.updateRotation = true;
        _isTrapped = false;
        _isDead = false; // 初期化
        _currentHealth = maxHealth; // 初期化

        Debug.Log($"[Bear Initialize] Agent: enabled={_agent.enabled}, speed={_agent.speed}, isOnNavMesh={_agent.isOnNavMesh}");

        Vector3 targetScale = transform.localScale;

        // 出現アニメーション（NavMeshAgent 位置制御確認中 - 問題が解決したら有効化）
        // transform.localScale = Vector3.one * 0.1f;
        // transform.DOScale(targetScale, 0.5f).SetEase(Ease.OutBack);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemy();
        }

        FindNextTarget();
        ObserveState();
        ObserveCollision();
    }

    /// ハンターからダメージを受けた際の処理
    public void TakeDamage(float damage, HunterController attacker)
    {
        if (_isDead || _isTrapped) return;

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

    /// <summary>
    /// 電気柵などによる麻痺
    /// </summary>
    /// <param name="duration">麻痺時間（秒）</param>
    /// <param name="damage">受けるダメージ</param>
    public void ApplyStun(float duration, float damage)
    {
        if (_isDead || IsParalyzed) return;

        Debug.Log("熊: 感電しました！麻痺状態になります。");

        // 1. ダメージ処理
        TakeDamage(damage, null);
        if (_isDead) return;

        // 2. 状態の拘束
        _isTrapped = true;       // 行動ロジックを停止させるフラグ
        _agent.isStopped = true; // NavMesh Agent停止
        StopAttacking();         // 攻撃動作キャンセル

        // 3. 感電演出
        transform.DOShakePosition(0.5f, strength: 0.5f, vibrato: 30, randomness: 90)
                 .SetLink(gameObject);

        transform.DOShakeRotation(0.5f, strength: 30f, vibrato: 30, randomness: 90)
                 .SetLink(gameObject);

        // アニメーション
        if (animator != null && enableAnimation)
        {
            animator.SetTrigger("Damage"); // ダメージモーション等を再生
            animator.speed = 0; // 一時的にアニメーションを止める場合
        }

        // 4. 指定時間後に復帰
        Observable.Timer(TimeSpan.FromSeconds(duration))
            .Subscribe(_ =>
            {
                if (!_isDead && this != null)
                {
                    RecoverFromParalysis();
                }
            })
            .AddTo(this);
    }

    /// <summary>
    /// 麻痺からの復帰処理
    /// </summary>
    private void RecoverFromParalysis()
    {
        Debug.Log("熊: 麻痺から回復しました。");
        _isTrapped = false;

        if (animator != null && enableAnimation) animator.speed = 1.0f;

        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }

        // 次のターゲットを探す
        FindNextTarget();
    }

    // 罠にかかった際の拘束処理を実行し、捕獲状態へ移行
    public void Capture(GameObject trap)
    {
        if (_isDead || _isTrapped) return;

        _isTrapped = true;
        _assignedTrap = trap; // どの檻に捕まったか記録
        _agent.enabled = false;
        StopAttacking();

        if (animator && enableAnimation) animator.speed = 0;

        transform.position = trap.transform.position;
        transform.rotation = Quaternion.identity;

        Debug.Log($"{name}が罠にかかりました。");
        transform.DOShakeScale(0.5f, 0.5f);
        
        // 捕獲時点で報酬を確定させ、Dead状態にする（二重支払い防止）
        TryProcessReward();
        _isDead = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReportEnemyDefeated();
        }

        Debug.Log("熊を捕獲しました！");
    }

    // 自身を現在拘束している罠の参照を取得する
    public GameObject GetAssignedTrap() => _assignedTrap;

    // アニメーションイベントから呼ばれる攻撃処理
    public void OnAttackHit()
    {
        // 1. ハンター優先判定
        if (_targetHunter != null && _targetHunter.gameObject.activeSelf && !_targetHunter.IsDead())
        {
            float dist = Vector3.Distance(transform.position, _targetHunter.transform.position);
            if (dist <= attackRange + 1.0f) // 1.0fの猶予
            {
                _targetHunter.TakeDamage(attackDamage);
            }
            return;
        }

        // 2. 家への攻撃判定
        if (_targetHouse != null && !_targetHouse.IsDestroyed)
        {
            // 表面までの最短距離を再計算
            Vector3 bearPos = transform.position;
            Vector3 closestPoint = _targetHouse.HouseCollider.ClosestPoint(bearPos);
            float dist = Vector3.Distance(
                new Vector3(bearPos.x, 0, bearPos.z),
                new Vector3(closestPoint.x, 0, closestPoint.z)
            );

            // 停止距離(attackRange)よりも少し広い判定（遊び）を持たせる
            if (dist <= attackRange + 2.0f)
            {
                Debug.Log($"熊: 家への攻撃ヒット！ 距離: {dist:F2}");
                _targetHouse.TakeDamage(attackDamage);
            }
            else
            {
                Debug.LogWarning($"熊: 攻撃アニメーションが再生されましたが、家が遠すぎます。 距離: {dist:F2}");
            }
        }
    }

    // 物理的な接触を起点とした攻撃や罠のトリガーを常時監視
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

    // HPや経過時間などの内部状態を監視し、死亡や状態復帰のロジックを制御
    private void ObserveState()
    {
        Debug.Log("[Bear ObserveState] Setting up observers");
        
        // 定期的にハンターを探索（ターゲットがない、または家をターゲットしている場合のみ）
        Observable.Interval(TimeSpan.FromSeconds(0.5f))
            .Where(_ => !_isTrapped && !_isDead && _targetHunter == null)
            .Subscribe(_ =>
            {
                Debug.Log("[Bear] Searching for hunter...");
                DetectNearestHunter();
            })
            .AddTo(this);

        // 毎フレーム更新
        this.UpdateAsObservable()
            .Where(_ => !_isTrapped && !_isDead)
            .Subscribe(_ =>
            {
                // 優先度1: ハンターをターゲットしている場合
                if (_targetHunter != null)
                {
                    // ハンターが死んだ、または無効になったらターゲット解除して次を探す
                    if (_targetHunter == null || !_targetHunter.gameObject.activeSelf || _targetHunter.IsDead())
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

    /// 周囲のハンターを検出する
    private void DetectNearestHunter()
    {
        var hunters = Physics.OverlapSphere(detectionPoint.position, detectionRadius)
            .Select(c => c.GetComponent<HunterController>())
            .Where(h => h != null && h.isActiveAndEnabled) // 有効なターゲットのみ
            .OrderBy(h => Vector3.Distance(detectionPoint.position, h.transform.position))
            .FirstOrDefault();

        if (hunters != null)
        {
            _targetHunter = hunters;
            _targetHouse = null; // 家へのターゲットを解除
            StopAttacking();
            if (_agent.isActiveAndEnabled) _agent.isStopped = false;
            Debug.Log("熊: ハンターを発見！ターゲットをハンターに変更します。");
        }
    }

    // ハンターに対する挙動
    private void HandleHunterTarget()
    {
        if (_targetHunter == null || !_targetHunter.gameObject.activeSelf || _targetHunter.IsDead())
        {
            _targetHunter = null;
            FindNextTarget();
            return;
        }

        Vector3 attackPosition = (detectionPoint != null) ? detectionPoint.position : transform.position;
        float dist = Vector3.Distance(attackPosition, _targetHunter.transform.position);

        // 判定の遊びを作る
        float effectiveRange = (_attackStream != null) ? attackRange + 2.0f : attackRange;

        // アニメーション状態を更新（攻撃範囲外なら移動中）
        bool isMoving = dist > attackRange - 1.0f;
        if (animator && enableAnimation && animator.enabled)
        {
            animator.SetBool("IsMoving", isMoving);
        }

        // 攻撃範囲内
        if (dist <= effectiveRange)
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
        if (_targetHouse == null || _targetHouse.IsDestroyed || _targetHouse.HouseCollider == null)
        {
            FindNextTarget();
            return;
        }

        Vector3 bearPos = transform.position;
        Vector3 closestPointOnHouse = _targetHouse.HouseCollider.ClosestPoint(bearPos);

        // Y軸を無視して水平距離を出す（高さの差で空振りするのを防ぐ）
        float horizontalDist = Vector3.Distance(
            new Vector3(bearPos.x, 0, bearPos.z),
            new Vector3(closestPointOnHouse.x, 0, closestPointOnHouse.z)
        );

        // アニメーション状態を更新
        bool isMoving = horizontalDist > 5.0f;
        if (animator && enableAnimation && animator.enabled)
        {
            animator.SetBool("IsMoving", isMoving);
            Debug.Log($"[Bear] Animator.SetBool IsMoving: {isMoving}");
        }

        // 攻撃開始距離を少し広げて、壁に密着しすぎる前に足を止める
        float stopThreshold = attackRange + 2.0f; // 余裕を持たせる

        if (horizontalDist <= stopThreshold)
        {
            Debug.Log($"[Bear] Stop threshold reached: {horizontalDist:F2} <= {stopThreshold}");
            
            if (!_agent.isStopped)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
            _agent.updateRotation = false;

            // 家の中心方向を向く（高さは熊と同じに揃える）
            Vector3 houseCenter = _targetHouse.HouseCollider.bounds.center;
            Vector3 lookTarget = houseCenter;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget);

            StartAttacking();
        }
        else
        {
            Debug.Log($"[Bear] Moving to house: {horizontalDist:F2} > {stopThreshold}");
            // 回転制御を戻して追跡する
            StopAttacking();

            if (_agent.isStopped) _agent.isStopped = false;
            _agent.updateRotation = true;
            _agent.SetDestination(closestPointOnHouse);

            if (animator && enableAnimation && animator.enabled)
            {
                animator.SetBool("IsMoving", true);
            }
        }
    }

    // 最寄りの攻撃対象（家・ハンター）を探し、移動を開始
    private void FindNextTarget()
    {
        Debug.Log("[Bear FindNextTarget] START");
        StopAttacking();
        
        // まずハンターを検出する（優先度が高いため）
        DetectNearestHunter();
        
        // ハンターが見つかった場合は、家へのターゲットを解除
        if (_targetHunter != null)
        {
            Debug.Log($"[Bear] Hunter target found: {_targetHunter.name}");
            _targetHouse = null;
            return;
        }

        // ハンターが見つからない場合、家を探す
        _targetHunter = null; // ハンターゲット解除

        var houses = GameObject.FindGameObjectsWithTag("House");
        Debug.Log($"[Bear] Found {houses.Length} houses with tag 'House'");
        
        if (houses.Length == 0) return;

        var validTargets = houses
            .Select(h => h.GetComponent<HouseHealth>())
            .Where(h => h != null && !h.IsDestroyed)
            .ToList();

        Debug.Log($"[Bear] Valid house targets: {validTargets.Count}");
        
        if (validTargets.Count == 0) return;

        // 一番近い壊れていない家を探す
        Vector3 searchPosition = (detectionPoint != null) ? detectionPoint.position : transform.position;
        _targetHouse = validTargets
            .OrderBy(h => Vector3.Distance(searchPosition, h.HouseCollider.ClosestPoint(searchPosition)))
            .FirstOrDefault();

        if (_targetHouse != null)
        {
            Debug.Log($"[Bear] Target house selected: {_targetHouse.name}");
            
            if (!_agent.isOnNavMesh)
            {
                Debug.LogError($"[Bear] Agent is NOT on NavMesh! Position: {transform.position}");
                return;
            }
            
            _agent.isStopped = false;
            _agent.updateRotation = true; // 移動開始時に向きの固定を解除
            Vector3 targetPos = _targetHouse.HouseCollider.ClosestPoint(transform.position);
            
            Debug.Log($"[Bear] Setting destination to house at: {targetPos}");
            bool success = _agent.SetDestination(targetPos);
            Debug.Log($"[Bear] SetDestination result: {success}, hasPath: {_agent.hasPath}, pathPending: {_agent.pathPending}");
            
            if (animator != null && enableAnimation && animator.enabled)
            {
                animator.SetBool("IsMoving", true);
                Debug.Log("[Bear] Animator IsMoving set to TRUE");
            }
        }
        else
        {
            Debug.LogWarning("[Bear] No valid house target found");
        }
    }

    // 一定間隔で攻撃するループ
    private void StartAttacking()
    {
        if (_attackStream != null) return;

        _attackStream = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(attackInterval))
            .Subscribe(_ =>
            {
                // ハンター・家、どちらを狙っている場合でも、OnAttackHitで判定してダメージを与える
                // アニメーションがある場合はTriggerセット
                if (animator != null && enableAnimation && animator.enabled)
                {
                    animator.SetTrigger("Attack");
                }

                // 攻撃演出
                transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);

                // アニメーションイベントを使わない場合はここで直接呼ぶ
                OnAttackHit();
            })
            .AddTo(this);
    }

    // 実行中の攻撃ループの停止・リソースの解放
    private void StopAttacking()
    {
        if (_attackStream != null)
        {
            _attackStream.Dispose();
            _attackStream = null;
        }
    }

    /// 状態に応じた報酬を確定し、GameManagerに報告する
    private void TryProcessReward()
    {
        if (_isRewardProcessed) return; // すでに処理済みなら何もしない

        int reward = 0;
        if (_isDead) reward = defeatReward;
        else if (IsParalyzed) reward = captureReward;

        if (reward > 0)
        {
            _isRewardProcessed = true;
            GameManager.Instance.AddPendingReward(reward);
        }
    }

    // 死亡状態へ移行、AI停止、演出再生、自身を破棄
    private void Die()
    {
        if (_isDead) return; // 二重呼び出し防止

        _isDead = true;
        _agent.enabled = false;
        StopAttacking();
        if (animator && enableAnimation) animator.SetTrigger("Die");

        GetComponent<Collider>().enabled = false;

        TryProcessReward();

        // GameManagerに死亡を報告
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReportEnemyDefeated();
            Debug.Log($"熊を討伐！ {defeatReward}円 獲得");
        }

        // アニメーション処理
        Vector3 currentRotation = transform.eulerAngles;
        Vector3 fallRotation = new Vector3(currentRotation.x, currentRotation.y, currentRotation.z + 90f);

        Sequence deathSequence = DOTween.Sequence();
        deathSequence.Append(transform.DORotate(fallRotation, 0.5f).SetEase(Ease.OutQuad));
        deathSequence.Join(transform.DOMoveY(transform.position.y - 0.5f, 0.5f).SetEase(Ease.InQuad));
        deathSequence.Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InQuad));
        deathSequence.OnComplete(() => Destroy(gameObject));
    }
}
