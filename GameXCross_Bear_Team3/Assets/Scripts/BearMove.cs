using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;  

[RequireComponent(typeof(NavMeshAgent))]
public class BearMove : MonoBehaviour
{
    private NavMeshAgent agent;
    private  Collider targetCollider;

    private float radius;
    private Vector3 centerPoint;

    private bool isInitialized = false;

    //private const float RESPAWN_BUFFER = 2.0f;
    private const float TARGET_MULTIPLIER = 1.5f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (!isInitialized) return;

        if(targetCollider == null)
        {
            if(!agent.isStopped) agent.isStopped = true;
            return;
        }

        Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);

        if(Vector3.SqrMagnitude(agent.destination - closestPoint) > 1.0f)
        {
            agent.SetDestination(closestPoint);
        }

        if(!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.pathPending || agent.velocity.sqrMagnitude == 0f)
            {
                RespawnAndMove();
            }
        }
    }

    public void Initialize(float speed, float rad, Vector3 center, Collider target)
    {
        this.radius = rad;
        this.centerPoint = center;
        this.targetCollider = target;

        agent.speed = speed;
        agent.updatePosition = true;
        agent.updateRotation = true;

        // NavMesh上に配置
        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if(NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogError($"クマ({name})をNavMesh上に出現できませんでした。");
                return;
            }
        }

        this.isInitialized = true;

        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        if(targetCollider != null)
        {
            agent.SetDestination(targetCollider.ClosestPoint(transform.position));
        }

    }

    private void RespawnAndMove()
    {
        for (int i = 0; i < 10; i++)
        {
            // ランダムな出現位置を計算
            float randomAngle = Random.Range(0f, 2f * Mathf.PI);
            float x = centerPoint.x + radius * Mathf.Cos(randomAngle);
            float z = centerPoint.z + radius * Mathf.Sin(randomAngle);
            float y = centerPoint.y;

            Vector3 spawnPosition = new Vector3(x, y, z);

            NavMeshHit hit;

            if (NavMesh.SamplePosition(spawnPosition, out hit, 10.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);

                transform.localScale = Vector3.zero;
                transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

                if(targetCollider != null)
                {
                    agent.SetDestination(targetCollider.ClosestPoint(transform.position));
                }
                return;
            }
        }
        Debug.LogWarning($"クマ({name})の再出現に失敗しました。" );
    }
}
