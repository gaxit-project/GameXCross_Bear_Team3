using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PointerContoroller : MonoBehaviour
{
    [Header("à⁄ìÆë¨ìx")]
    [SerializeField] private float Speed = 1;
    private Vector2 input;
    [SerializeField] public GameObject ScrollUI;
    [SerializeField] public GameObject pointer;
    [Header("åªç›ëIëÇµÇƒÇ¢ÇÈê›íuï®")]
    [SerializeField] public GameObject obj;

    void Start()
    {
        pointer.SetActive(false);
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

    public void OnCansel(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            ScrollUI.SetActive(true);
            pointer.SetActive(false);
        }
    }

    public void OnPut(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Instantiate(obj, pointer.transform.position, pointer.transform.rotation);
        }
    }
}
