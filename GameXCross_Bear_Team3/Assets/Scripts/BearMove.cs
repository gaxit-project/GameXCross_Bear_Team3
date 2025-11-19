using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BearMove : MonoBehaviour
{
    private NavMeshAgent agent;

    private float radius;
    private Vector3 centerPoint;

    private bool isInitialized = false;

    private const float RESPAWN_BUFFER = 2.0f;
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

        if (agent.hasPath)
        {
            Debug.DrawLine(transform.position, agent.destination, Color.red);
        }

        //transform.position += moveDirection * moveSpeed * Time.deltaTime;

        //if (moveDirection != Vector3.zero)
        //{
        //    transform.rotation = Quaternion.LookRotation(moveDirection);
        //}

        Vector3 posOnPlane = new Vector3(transform.position.x, centerPoint.y, transform.position.z);
        float currentDistance = Vector3.Distance(posOnPlane, centerPoint);

        if (currentDistance > radius + RESPAWN_BUFFER)
        {
            RespawnAndMove();
        }
    }

    public void Initialize(float speed, float rad, Vector3 center, Vector3 initDirection)
    {
        this.radius = rad;
        this.centerPoint = center;

        agent.speed = speed;
        agent.updatePosition = true;
        agent.updateRotation = true;

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
        SetDestinationToOppositeSide();

    }

    private void SetDestinationToOppositeSide()
    {
        Vector3 currentPos = transform.position;
        Vector3 directionToCenter = (centerPoint - currentPos).normalized;

        Vector3 targetPos = centerPoint + (directionToCenter * (radius * TARGET_MULTIPLIER));

        // NavMeshAgentに目的地を設定
        agent.SetDestination(targetPos);
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
                float dist = Vector3.Distance(new Vector3(hit.position.x, centerPoint.y, hit.position.z), centerPoint);
                if (dist < radius + 4.0f)
                {
                    agent.Warp(hit.position);
                    SetDestinationToOppositeSide();
                    return;
                }
            }
        }
        Debug.LogWarning($"クマ({name})の再出現に失敗しました。" );
    }
}
