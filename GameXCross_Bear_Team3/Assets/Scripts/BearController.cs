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
    [SerializeField] private float detectionRadius = 15.0f; // ハンター検出範囲

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

        // 横に倒れて消えるアニメーション
        Vector3 currentRotation = transform.eulerAngles;
        // Z軸を回転させて横に倒れる（左または右に倒れる）
        Vector3 fallRotation = new Vector3(currentRotation.x, currentRotation.y, currentRotation.z + 90f);
        
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
        if (_targetHunter != null && _targetHunter.gameObject.activeSelf && !_targetHunter.IsDead())
        {
            Vector3 attackPosition = (detectionPoint != null) ? detectionPoint.position : transform.position;
            float dist = Vector3.Distance(attackPosition, _targetHunter.transform.position);
            // 距離の誤差許容（攻撃範囲より少し広めに設定）
            if (dist <= attackRange + 2.0f)
            {
                Debug.Log($"熊: ハンターへ攻撃ヒット (距離: {dist:F2}, 攻撃範囲: {attackRange})");
                _targetHunter.TakeDamage(attackDamage);
            }
            else
            {
                Debug.Log($"熊: ハンターが攻撃範囲外 (距離: {dist:F2}, 攻撃範囲: {attackRange})");
            }
            return; // ハンター優先
        }

        // 家への攻撃
        if (_targetHouse != null && !_targetHouse.IsDestroyed && _targetHouse.HouseCollider != null)
        {
            Vector3 attackPosition = (detectionPoint != null) ? detectionPoint.position : transform.position;
            
            // 方法1: ClosestPointを使った距離判定
            Vector3 hitPoint = _targetHouse.HouseCollider.ClosestPoint(attackPosition);
            float distToClosest = Vector3.Distance(attackPosition, hitPoint);
            
            // 方法2: Raycastを使って熊の正面方向から家に当たっているか確認
            bool hitByRaycast = false;
            Vector3 attackDirection = transform.forward;
            RaycastHit hit;
            if (Physics.Raycast(attackPosition, attackDirection, out hit, attackRange + 2.0f))
            {
                if (hit.collider == _targetHouse.HouseCollider)
                {
                    hitByRaycast = true;
                }
            }
            
            // 方法3: 家のコライダーの境界ボックスとの距離判定（より寛容）
            Bounds houseBounds = _targetHouse.HouseCollider.bounds;
            float distToBounds = Vector3.Distance(attackPosition, houseBounds.ClosestPoint(attackPosition));
            
            // いずれかの条件を満たせば攻撃が当たったと判定
            if (distToClosest <= attackRange + 3.0f || hitByRaycast || distToBounds <= attackRange + 2.0f)
            {
                Debug.Log($"熊: 家へ攻撃ヒット (ClosestPoint距離: {distToClosest:F2}, Bounds距離: {distToBounds:F2}, Raycast: {hitByRaycast})");
                _targetHouse.TakeDamage(attackDamage);
            }
            else
            {
                Debug.Log($"熊: 家が攻撃範囲外 (ClosestPoint距離: {distToClosest:F2}, Bounds距離: {distToBounds:F2}, 攻撃範囲: {attackRange})");
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
        float stopThreshold = Mathf.Max(attackRange, 1.0f);

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
