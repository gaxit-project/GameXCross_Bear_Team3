using DG.Tweening;
using System;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
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

    public bool IsParalyzed => _isTrapped;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();

        _agent.stoppingDistance = attackRange - 0.5f;
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

    private void Die()
    {
        if (_isDead) return; // 二重呼び出し防止

        _isDead = true;
        _agent.enabled = false;
        StopAttacking();
        if (animator && enableAnimation) animator.SetTrigger("Die");

        GetComponent<Collider>().enabled = false;

        // GameManagerに死亡を報告
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReportEnemyDefeated();

            // 倒した時の報酬を加算（即時加算か、AddPendingRewardかはGameManagerの仕様に合わせてください）
            GameManager.Instance.AddPendingReward(defeatReward);
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

    /// <summary>
    /// 電気柵などによる麻痺
    /// </summary>
    /// <param name="duration">麻痺時間（秒）</param>
    /// <param name="damage">受けるダメージ</param>
    public void ApplyParalysis(float duration, float damage)
    {
        if (_isDead) return;

        // 既に動けない状態ならダメージだけ受ける（連続麻痺防止）
        if (_isTrapped)
        {
            TakeDamage(damage, null);
            return;
        }

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

    public void OnTrapped(GameObject trap)
    {
        if (_isTrapped || _isDead) return;

        _isTrapped = true;
        _assignedTrap = trap; // どの檻に捕まったか記録
        _agent.enabled = false;
        StopAttacking();

        if (animator && enableAnimation) animator.speed = 0;

        transform.position = trap.transform.position;
        transform.rotation = Quaternion.identity;

        Debug.Log($"{name}が罠にかかりました。");
        transform.DOShakeScale(0.5f, 0.5f);

        if (GameManager.Instance != null)
        {
            // 1. 敵の数を減らしてフェーズ進行を進める
            GameManager.Instance.ReportEnemyDefeated();

            // 2. 捕獲報酬を登録する
            GameManager.Instance.AddPendingReward(captureReward);
        }

        Debug.Log("熊を捕獲しました！");
    }
    public GameObject GetAssignedTrap() => _assignedTrap;

    // アニメーションイベントから呼ばれる攻撃処理
    public void OnAttackHit()
    {
        // ハンターへの攻撃
        if (_targetHunter != null && _targetHunter.gameObject.activeSelf && !_targetHunter.IsDead())
        {
            // 1. 高さ(Y軸)を無視した距離計算
            Vector3 bearPos = transform.position;
            Vector3 hunterPos = _targetHunter.transform.position;
            bearPos.y = 0;
            hunterPos.y = 0;

            //Vector3 attackPosition = (detectionPoint != null) ? detectionPoint.position : transform.position;
            //float dist = Vector3.Distance(attackPosition, _targetHunter.transform.position);
            float dist = Vector3.Distance(bearPos, hunterPos);
            // 距離の誤差許容（攻撃範囲より少し広めに設定）
            if (dist <= attackRange + 5.0f)
            {
                Debug.Log($"熊({gameObject.name}) -> ハンター({_targetHunter.name}) : {attackDamage} ダメージ");
                _targetHunter.TakeDamage(attackDamage);
            }

            return; // ハンター優先
        }

        // 家への攻撃判定
        if (_targetHouse != null && !_targetHouse.IsDestroyed)
        {
            // 1. 自分の周囲に球体の判定を発生させる
            float detectionSize = attackRange + 2.0f; // 判定サイズを広げる
            Vector3 checkCenter = transform.position + transform.forward * 3.0f; // 少し前方を起点にする

            // 2. 範囲内のコライダーをすべて取得
            Collider[] hitColliders = Physics.OverlapSphere(checkCenter, detectionSize);

            // 3. 狙っている家がその範囲に含まれているか確認
            bool isHit = hitColliders.Any(c => c == _targetHouse.HouseCollider);

            if (isHit)
            {
                Debug.Log($"熊: 家への広域攻撃ヒット (判定半径: {detectionSize})");
                _targetHouse.TakeDamage(attackDamage);
            }
            else
            {
                // 4. 万が一外れた場合でも、非常に近い場合は強制ヒット
                float distToHouse = Vector3.Distance(transform.position, _targetHouse.HouseCollider.ClosestPoint(transform.position));
                if (distToHouse <= attackRange + 3.0f)
                {
                    Debug.Log("熊: 近接補正により家へヒット");
                    _targetHouse.TakeDamage(attackDamage);
                }
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

        if (animator && enableAnimation) animator.SetBool("IsMoving", dist > attackRange - 1.0f);

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

        Vector3 attackPosition = (detectionPoint != null) ? detectionPoint.position : transform.position;
        // 距離判定は水平面で行う（高さ差で遠く判定されないようにする）
        Vector3 destination = _targetHouse.HouseCollider.ClosestPoint(attackPosition);
        destination.y = attackPosition.y;
        float dist = Vector3.Distance(attackPosition, destination);

        if (animator && enableAnimation) animator.SetBool("IsMoving", dist > 5.0f);

        // 実際の攻撃判定と同じレンジで止まる
        //float stopThreshold = Mathf.Max(attackRange, 1.0f);

        // 攻撃開始距離を少し広げて、壁に密着しすぎる前に足を止める
        float stopThreshold = attackRange + 2.0f; // 余裕を持たせる

        if (dist <= stopThreshold)
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
            /*bool isAttackingAndInRange = (_attackStream != null && dist <= attackRange);
            if (isAttackingAndInRange)
            {
                StopAttacking();
                if (_agent.isStopped) _agent.isStopped = false;
                if (Vector3.Distance(_agent.destination, destination) > 1.0f)
                {
                    _agent.SetDestination(destination);
                }
            }*/

            // 回転制御を戻して追跡する
            StopAttacking();

            if (_agent.isStopped) _agent.isStopped = false;

            _agent.updateRotation = true; // 移動方向を向くようにする

            // 目的地が大きく変わった場合のみパスを更新（負荷軽減）
            if (Vector3.Distance(_agent.destination, destination) > 1.0f)
            {
                _agent.SetDestination(destination);
            }

            if (animator && enableAnimation) animator.SetBool("IsMoving", true);
        }
    }

    private void FindNextTarget()
    {
        StopAttacking();
        
        // まずハンターを検出する（優先度が高いため）
        DetectNearestHunter();
        
        // ハンターが見つかった場合は、家へのターゲットを解除
        if (_targetHunter != null)
        {
            _targetHouse = null;
            return;
        }

        // ハンターが見つからない場合、家を探す
        _targetHunter = null; // ハンターゲット解除

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

    private void StartAttacking()
    {
        if (_attackStream != null) return;

        _attackStream = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(attackInterval))
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
