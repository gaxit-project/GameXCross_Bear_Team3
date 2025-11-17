using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("カメラ移動設定")]
    [SerializeField] private float moveSpeed = 5.0f;

    [Header("カメラ回転設定")]
    [SerializeField] private float lookSensitivity = 100.0f;
    [SerializeField] private float pitchMax = 85.0f;
    [SerializeField] private float pitchMin = -85.0f;
    [SerializeField] private bool invertY = false;

    [Header("コンポーネント")]
    [SerializeField] private Transform pivotTransform;

    [Header("操作設定")]
    public bool enableRotation = true;

    [Header("Input Actions")]
    private CameraControls cameraControls;
    private Vector2 moveInput;
    private Vector2 lookInput;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    private void Awake()
    {
        cameraControls = new CameraControls();

        UpdateCursorState();
    }

    private void OnEnable()
    {
        cameraControls.Map.Move.performed += OnMovePerformed;
        cameraControls.Map.Move.canceled += OnMoveCanceled;

        cameraControls.Map.Look.performed += OnLookPerformed;
        cameraControls.Map.Look.canceled += OnLookCanceled;

        cameraControls.Enable();
    }

    private void OnDisable()
    {
        cameraControls.Disable();

        cameraControls.Map.Move.performed -= OnMovePerformed;
        cameraControls.Map.Move.canceled -= OnMoveCanceled;

        cameraControls.Map.Look.performed -= OnLookPerformed;
        cameraControls.Map.Look.canceled -= OnLookCanceled;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (enableRotation)
        {

            // 視点回転の処理
            float lookX = lookInput.x * lookSensitivity * Time.deltaTime;
            float lookY = lookInput.y * lookSensitivity * Time.deltaTime;

            // 左右反転
            if (invertY) lookY = -lookY;

            // 水平方向の入力
            yaw += lookX;

            // 垂直方向の入力
            pitch -= lookY;

            // ピッチの制限
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            transform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);

            if (pivotTransform != null)
            {
                //pivotTransform.localRotation = Quaternion.Euler(pitch, 0.0f, 0.0f);
            }
            else
            {
                Debug.LogWarning("Pivot Transform が設定されていません", this);

            }
        }

        // 視点移動の処理
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);

        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);
    }

    public void SetRotationEnabled(bool isEnable)
    {
        enableRotation = isEnable;
        UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        if (enableRotation)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
