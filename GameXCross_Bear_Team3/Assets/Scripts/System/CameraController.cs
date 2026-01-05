using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("カメラ移動設定")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float verticalSpeed = 5.0f;

    [Header("カメラ回転設定")]
    [SerializeField] private float lookSensitivity = 100.0f;
    [SerializeField] private float pitchMax = 85.0f;
    [SerializeField] private float pitchMin = -85.0f;
    [SerializeField] private bool invertY = false;

    [Header("コンポーネント")]
    [SerializeField] private Transform pivotTransform;

    [Header("動作設定")]
    public bool enableRotation = true;
    [SerializeField] private bool isLocked = false; // 入力を完全に無効化する

    [Header("Input Actions")]
    private CameraControls cameraControls;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float elevateInput;

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

        cameraControls.Map.Elevate.performed += OnElevatePerformed;
        cameraControls.Map.Elevate.canceled += OnElevateCanceled;

        cameraControls.Enable();
    }

    private void OnDisable()
    {
        cameraControls.Map.Move.performed -= OnMovePerformed;
        cameraControls.Map.Move.canceled -= OnMoveCanceled;

        cameraControls.Map.Look.performed -= OnLookPerformed;
        cameraControls.Map.Look.canceled -= OnLookCanceled;

        cameraControls.Map.Elevate.performed -= OnElevatePerformed;
        cameraControls.Map.Elevate.canceled -= OnElevateCanceled;

        cameraControls.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if (isLocked) return;
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        if (isLocked)
        {
            lookInput = Vector2.zero;
            return;
        }

        if (enableRotation)
        {
            lookInput = context.ReadValue<Vector2>();
        }
        else
        {
            lookInput = Vector2.zero;
        }
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }

    private void OnElevatePerformed(InputAction.CallbackContext context)
    {
        if (isLocked) return;
        elevateInput = context.ReadValue<float>();
    }

    private void OnElevateCanceled(InputAction.CallbackContext context)
    {
        elevateInput = 0.0f;
    }

    void Start()
    {
        yaw = transform.eulerAngles.y;

        if(pivotTransform != null)
        {
            float currentPitch = pivotTransform.localEulerAngles.x;

            if (currentPitch > 180) currentPitch -= 360;
            pitch = currentPitch;
        }
    }

    void Update()
    {
        if (isLocked)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            elevateInput = 0.0f;
            return;
        }

        // enableRotationがfalseの場合は、入力もリセットしておく
        if (!enableRotation)
        {
            lookInput = Vector2.zero;
        }

        if (enableRotation)
        {
            // カメラ回転処理
            float lookX = lookInput.x * lookSensitivity * Time.deltaTime;
            float lookY = lookInput.y * lookSensitivity * Time.deltaTime;

            // 水平方向の回転
            yaw += lookX;

            transform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);

            if (invertY) lookY = -lookY;

            pitch -= lookY;

            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            if(pivotTransform != null)
            {
                pivotTransform.localRotation = Quaternion.Euler(pitch, 0.0f, 0.0f);
            }
            else
            {
                Debug.LogWarning("Pivot Transform が未設定です。Inspectorでカメラ本体またはピボットを割り当ててください。", this);
            }
        }

        // カメラ移動処理
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);

        float verticalMove = elevateInput * verticalSpeed * Time.deltaTime;
        transform.Translate(Vector3.up * verticalMove, Space.World);
    }

    public void SetRotationEnabled(bool isEnable)
    {
        enableRotation = isEnable;
        // 回転を無効化する場合は、現在の入力をリセット
        if (!isEnable)
        {
            lookInput = Vector2.zero;
        }
        UpdateCursorState();
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (locked)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            elevateInput = 0.0f;
        }
        UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

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

    private void OnValidate()
    {
        if(Application.isPlaying)
        {
            UpdateCursorState();
        }
    }
}
