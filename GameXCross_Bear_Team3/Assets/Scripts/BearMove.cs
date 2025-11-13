using UnityEngine;

public class BearMove : MonoBehaviour
{
    [Header("ŒF‚Ì‘¬“x")]
    public float moveSpeed = 5.0f;

    [Header("oŒ»Ý’è")]
    public float radius = 25.0f;
    public Vector3 centerPoint = Vector3.zero;

    private Vector3 moveDirection;

    void Start()
    {
        ReapawnAndSetDirection();
    }

    void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        if(moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        Vector3 posOnPlane = new Vector3(transform.position.x, centerPoint.y, transform.position.z);
        float currentDistance = Vector3.Distance(posOnPlane, centerPoint);

        if (currentDistance > radius + 0.1f) ReapawnAndSetDirection();

    }

    private void ReapawnAndSetDirection()
    {
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);

        float x = centerPoint.x + radius * Mathf.Cos(randomAngle);
        float z = centerPoint.z + radius * Mathf.Sin(randomAngle);

        float y = centerPoint.y;

        Vector3 spawnPosition = new Vector3(x, y, z);

        transform.position = spawnPosition;

        Vector3 centerOnPlane = new Vector3(centerPoint.x, y, centerPoint.z);
        Vector3 directionToCenter = (centerOnPlane - spawnPosition).normalized;

        moveDirection = directionToCenter;
    }
}
