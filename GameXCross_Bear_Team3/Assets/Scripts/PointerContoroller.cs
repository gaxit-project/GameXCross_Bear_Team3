using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PointerContoroller : MonoBehaviour
{
    [Header("ˆÚ“®‘¬“x")]
    [SerializeField]private float Speed = 1;
    private Vector2 input;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 MoveDirection = new Vector3(input.x, 0, input.y);
        transform.Translate(MoveDirection * Speed * Time.deltaTime);
    }

    public void OnPerformed(InputAction.CallbackContext context)
    {
        input = context.ReadValue<Vector2>();
    }
}
