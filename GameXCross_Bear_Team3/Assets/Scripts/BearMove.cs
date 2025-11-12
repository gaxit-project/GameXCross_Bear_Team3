using UnityEngine;

public class BearMove : MonoBehaviour
{
    public float moveSpeed = 5.0f;

    private float bearMove;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += -Vector3.forward * moveSpeed * Time.deltaTime;
    }
}
