using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("�J�����ړ��ݒ�")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float verticalSpeed = 5.0f;

    [Header("�J������]�ݒ�")]
    [SerializeField] private float lookSensitivity = 100.0f;
    [SerializeField] private float pitchMax = 85.0f;
    [SerializeField] private float pitchMin = -85.0f;
    [SerializeField] private bool invertY = false;

    [Header("�R���|�[�l���g")]
    [SerializeField] private Transform pivotTransform;

    [Header("����ݒ�")]
    public bool enableRotation = true;

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

    private void OnElevatePerformed(InputAction.CallbackContext context)
    {
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
        if (enableRotation)
        {

            // ���_��]�̏���
            float lookX = lookInput.x * lookSensitivity * Time.deltaTime;
            float lookY = lookInput.y * lookSensitivity * Time.deltaTime;

            // ���������̓���
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
                Debug.LogWarning("Pivot Transform ���ݒ肳��Ă��܂���BInspector�ŃJ�����܂��̓s�{�b�g����蓖�ĂĂ��������B", this);
            }
        }

        // ���_�ړ��̏���
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);

        float verticalMove = elevateInput * verticalSpeed * Time.deltaTime;
        transform.Translate(Vector3.up * verticalMove, Space.World);
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

    private void OnValidate()
    {
        if(Application.isPlaying)
        {
            UpdateCursorState();
        }
    }
}
