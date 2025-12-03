using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PointerContoroller : MonoBehaviour
{
    [Header("移動速度")]
    [SerializeField] private float Speed = 1;
    private Vector2 input;
    [SerializeField] public GameObject ScrollUI;
    [SerializeField] public GameObject pointer;
    [SerializeField] public money money;
    [Header("現在選択している設置物")]
    [SerializeField] public GameObject obj;
    [Header("その設置物の設置コスト")]
    [SerializeField] public int cost;

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
            if (money.moneycount > cost)
            {
                Instantiate(obj, pointer.transform.position, pointer.transform.rotation);
                money.moneycount -= cost;
            }
        }
    }
}
