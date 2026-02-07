using DG.Tweening;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections;
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
    [SerializeField] private float houseAttackRange = 15.0f;
    [SerializeField] private float hunterAttackRange = 15.0f;
    [SerializeField] private float detectionRadius = 20.0f;
    [SerializeField] private int captureReward = 30000; // 捕獲時の報酬
    [SerializeField] private int defeatReward = 30000;  // 倒した時の報酬

    [Header("参照")]
    [SerializeField] private Transform detectionPoint;
    [SerializeField] private bool enableAnimation = true;
    [SerializeField] private Animator animator;
    [SerializeField] private DamageFlash DamageFlash;

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

    private float _lastAttackTime = -999f; // 最後に攻撃した時間

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        _agent.stoppingDistance = houseAttackRange;
        _currentHealth = maxHealth; // 初期化

        if (detectionPoint == null) detectionPoint = transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (!enableAnimation && animator != null) animator.enabled = false;
    }

    // 初期状態
    public void Initialize()
    {
        var balance = GameManager.Instance.Balance;
        this.maxHealth = balance.bearMaxHealth;
        this.attackDamage = balance.bearAttackDamage;
        this.attackInterval = balance.bearAttackInterval;
        this.houseAttackRange = balance.bearHouseAttackRange;
        this.hunterAttackRange = balance.bearHunterAttackRange;
        this.detectionRadius = balance.bearDetectionRadius;
        this.captureReward = balance.bearCaptureReward;
        this.defeatReward = balance.bearDefeatReward;

        _currentHealth = maxHealth;

        // NavMeshAgent が手前で止まらないよう調整
        _agent.enabled = true;
        _agent.speed = balance.bearMoveSpeed;
        _agent.stoppingDistance = 0f;                 // 攻撃対象へ食い込む
        _agent.autoBraking = false;                   // 手前で減速しない
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance; // 回避を弱める

        _currentHealth = maxHealth; // 初期化

        Vector3 targetScale = transform.localScale;

        // 出現アニメーション
        transform.localScale = Vector3.one * 0.1f;
        transform.DOScale(targetScale, 0.5f).SetEase(Ease.OutBack);

        // GameManagerに自分を登録する
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemy(this);
        }

        FindNextTarget();
        ObserveState();
        ObserveCollision();
    }

    /// ハンターからダメージを受けた際の処理
    public void TakeDamage(float damage, HunterController attacker)
    {
        DamageFlash.Flash();
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
        SEmanager.Instance.Play("bear");
        if (_isDead || IsParalyzed) return;

        Debug.Log("熊: 感電しました！麻痺状態になります。");

        animator.SetBool("IsMoving", false);

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
        /* if (animator != null && enableAnimation)
        {
            animator.SetTrigger("Damage"); // ダメージモーション等を再生
            animator.speed = 0; // 一時的にアニメーションを止める場合
            Debug.Log("熊: Damageアニメーション再生");
        }*/

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

        if (animator != null && enableAnimation)
        {
            animator.speed = 1.0f;
            animator.SetBool("IsMoving", true);
        }

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

        // 捕獲数をカウント
        KillCountManager.Instance?.Capturecounterplus();

        // 捕獲時点で報酬を確定させ、Dead状態にする（二重支払い防止）
        TryProcessReward();
        _isDead = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReportEnemyDefeated(this);
        }

        Debug.Log("熊を捕獲しました！");
        PublicOpinionManager.Instance.POchanging(0.5f);//世論値をプラス0.5する

    }

    // 自身を現在拘束している罠の参照を取得する
    public GameObject GetAssignedTrap() => _assignedTrap;

    // アニメーションイベントから呼ばれる攻撃処理
    public void OnAttackHit()
    {
        Debug.Log("OnAttackHit() が呼ばれました！");

        // 1. ハンター優先判定
        if (_targetHunter != null && _targetHunter.gameObject.activeSelf && !_targetHunter.IsDead())
        {
            float dist = Vector3.Distance(transform.position, _targetHunter.transform.position);

            if (dist <= hunterAttackRange)
            {
                Debug.Log($"熊: ハンターに {attackDamage} ダメージを与えた！");
                _targetHunter.TakeDamage(attackDamage);
            }
            else
            {
                Debug.LogWarning($"熊: ハンターが範囲外です。");
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
            if (dist <= houseAttackRange)
            {
                Debug.Log($"熊: 家への攻撃ヒット！");
                _targetHouse.TakeDamage(attackDamage);
            }
            else
            {
                Debug.LogWarning($"熊: 攻撃アニメーションが再生されましたが、家が遠すぎます。");
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
        // 定期的にハンターを探索（ターゲットがない、または家をターゲットしている場合のみ）
        Observable.Interval(TimeSpan.FromSeconds(0.5f))
            .Where(_ => !_isTrapped && !_isDead && _targetHunter == null)
            .Subscribe(_ =>
            {
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
                        Debug.Log("熊: ハンターが死亡/無効。ターゲット解除");
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

    /// <summary>
    /// 周囲のハンターを検出する
    /// </summary>
    private void DetectNearestHunter()
    {
        // 検知範囲内のすべてのハンターを取得し、最も近い1体を選択
        _targetHunter = Physics.OverlapSphere(transform.position, GameManager.Instance.Balance.bearDetectionRadius)
            .Select(c => c.GetComponent<HunterController>())
            .Where(h => h != null && h.isActiveAndEnabled && !h.IsDead())
            .OrderBy(h => Vector3.Distance(transform.position, h.transform.position))
            .FirstOrDefault();
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

        float dist = Vector3.Distance(transform.position, _targetHunter.transform.position);

        // 常に追従し続ける（攻撃中でも止めない）
        // _agent.isStopped = false;
        // _agent.SetDestination(_targetHunter.transform.position);

        if (animator && enableAnimation)
        {
            bool isMoving = dist > hunterAttackRange || _agent.velocity.sqrMagnitude > 0.1f;
            animator.SetBool("IsMoving", isMoving);
            //Debug.Log("熊: 移動アニメーション再生");
        }

        if (dist <= hunterAttackRange)
        {
            if(!_agent.isStopped)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }

            if (animator && enableAnimation)
            {
                animator.SetBool("IsMoving", false);
            }

            var lookTarget = _targetHunter.transform.position;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget);

            StartAttacking();
        }
        else
        {
            // 追跡モード
            StopAttacking();

            if(_agent.isStopped) _agent.isStopped = false;
            _agent.SetDestination(_targetHunter.transform.position);

            if (animator && enableAnimation)
            {
                bool isMoving = _agent.velocity.sqrMagnitude > 0.1f;
                animator.SetBool("IsMoving", isMoving);
            }
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

        // Y軸を無視して水平距離を出す
        float horizontalDist = Vector3.Distance(
            new Vector3(bearPos.x, 0, bearPos.z),
            new Vector3(closestPointOnHouse.x, 0, closestPointOnHouse.z)
        );

        if (animator && enableAnimation)
        {
            animator.SetBool("IsMoving", horizontalDist > houseAttackRange);
            // Debug.Log("熊: 移動アニメーション再生");
        }

        float stopThreshold = houseAttackRange;

        if (horizontalDist <= stopThreshold)
        {
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
            // 回転制御を戻して追跡する
            StopAttacking();

            if (_agent.isStopped) _agent.isStopped = false;
            _agent.updateRotation = true;
            _agent.SetDestination(closestPointOnHouse);

            if (animator && enableAnimation)
            {
                animator.SetBool("IsMoving", true);
                // Debug.Log("熊: 移動アニメーション再生");
            }    
        }
    }

    // 最寄りの攻撃対象（家・ハンター）を探し、移動を開始
    private void FindNextTarget()
    {
        StopAttacking();
        
        // まずハンターを検出する
        DetectNearestHunter();
        
        // ハンターが見つかった場合は、家へのターゲットを解除
        if (_targetHunter != null)
        {
            _targetHouse = null;
            return;
        }

        // ハンターが見つからない場合、家を探す
        _targetHunter = null; // ターゲット解除

        var houses = GameObject.FindGameObjectsWithTag("House");
        if (houses.Length == 0) return;

        var validTargets = houses
            .Select(h => h.GetComponent<HouseHealth>())
            .Where(h => h != null && !h.IsDestroyed)
            .ToList();

        if (validTargets.Count == 0) return;

        // 一番近い壊れていない家を探す
        Vector3 searchPosition = (detectionPoint != null) ? detectionPoint.position : transform.position;
        _targetHouse = validTargets
            .OrderBy(h => Vector3.Distance(searchPosition, h.HouseCollider.ClosestPoint(searchPosition)))
            .FirstOrDefault();

        if (_targetHouse != null)
        {
            _agent.isStopped = false;
            _agent.updateRotation = true; // 移動開始時に向きの固定を解除
            Vector3 targetPos = _targetHouse.HouseCollider.ClosestPoint(transform.position);
            _agent.SetDestination(targetPos);
        }
    }

    // 一定間隔で攻撃するループ
    private void StartAttacking()
    {
        if (_attackStream != null) return;

        _attackStream = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(attackInterval))
            .Subscribe(_ =>
            {
                if (animator != null && enableAnimation)
                {
                    if (Time.time < _lastAttackTime + attackInterval) return;
                    _lastAttackTime = Time.time;
                    animator.SetTrigger("Attack");
                    OnAttackHit();
                    Debug.Log("熊: 攻撃アニメーション再生");
                }
                else
                {
                    // アニメーションが無効な場合のみ直接実行
                    OnAttackHit();
                }

                // transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
            })
            .AddTo(this);
    }

    // 実行中の攻撃ループの停止・リソースの解放
    private void StopAttacking()
    {
        _agent.updateRotation = true;
        
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
        StartCoroutine(BearCry());
        _isDead = true;
        _agent.enabled = false;
        StopAttacking();
        if (animator && enableAnimation) 
        {
            // animator.SetTrigger("Die");
            Debug.Log("熊: 死亡アニメーション再生");
        }
        GetComponent<Collider>().enabled = false;

        TryProcessReward();

        // 駆除数をカウント
        KillCountManager.Instance?.KillCounterplus();

        // GameManagerに死亡を報告
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReportEnemyDefeated(this);
            Debug.Log($"熊を討伐！ {defeatReward}円 獲得");
        }

        PublicOpinionManager.Instance.POchanging(0.2f);//世論値を0.2上昇させる

        // アニメーション処理
        Vector3 currentRotation = transform.eulerAngles;
        Vector3 fallRotation = new Vector3(currentRotation.x, currentRotation.y, currentRotation.z + 90f);

        Sequence deathSequence = DOTween.Sequence();
        deathSequence.Append(transform.DORotate(fallRotation, 0.5f).SetEase(Ease.OutQuad));
        deathSequence.Join(transform.DOMoveY(transform.position.y - 0.5f, 0.5f).SetEase(Ease.InQuad));
        deathSequence.Append(transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InQuad));
        deathSequence.OnComplete(() => Destroy(gameObject));
    }

    private IEnumerator BearCry()
    {
        yield return new WaitForSeconds(0.3f);
        SEmanager.Instance.Play("bear");
    }
}
