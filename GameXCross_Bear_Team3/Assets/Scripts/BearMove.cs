using UnityEngine;

public class BearMove : MonoBehaviour
{
    public float moveSpeed = 5.0f;

    private float bearMove;

    void Start()
    {
        
    }

    void Update()
    {
        transform.position += Vector3.back * moveSpeed * Time.deltaTime;
    }
}
