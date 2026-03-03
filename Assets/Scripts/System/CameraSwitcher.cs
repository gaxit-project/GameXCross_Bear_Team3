using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UniRx;
using TMPro;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineCamera[] cameras;
    [SerializeField] private CinemachineCamera photoCamera;

    [Header("撮影用カメラ設定")]
    [SerializeField] private bool isPhotoMode = false;
    [SerializeField] private float photoMoveSpeed = 10f;
    [SerializeField] private float photoVerticalSpeed = 5f;
    [SerializeField] private float photoRotateSpeed = 0.1f; // 回転感度
    [SerializeField] private Canvas photoCanvas; // 非表示にするUI
    private bool isCapturing = false; // 時間停止・UI非表示中か
    private float photoYaw = 0f;
    private float photoPitch = 0f;

    [Header("切り替えキー設定")]
    [SerializeField]
    private Key[] switchKeys = new Key[]
    {
        Key.Digit1,
        Key.Digit2,
        Key.Digit3,
        Key.Digit4
    };

    private int currentCameraIndex = 0;
    private bool lastIsPhotoMode = false;

    private void Start()
    {
        // 配列が空でないかチェック
        if (cameras == null || cameras.Length == 0) return;

        // 初期化：最初のカメラだけ優先度を高くする
        SwitchCamera(0);
        lastIsPhotoMode = isPhotoMode;

        // 毎フレーム入力を監視
        Observable.EveryUpdate()
            .Subscribe(_ => 
            {
                CheckPhotoModeToggle();
                if (isPhotoMode)
                {
                    CheckCaptureInput();
                    HandlePhotoCameraMovement();
                }
                else
                {
                    CheckInput();
                }
            })
            .AddTo(this);
    }

    private void CheckInput()
    {

        if (GameManager.Instance != null && GameManager.Instance.DebugMode)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                for (int i = 0; i < switchKeys.Length; i++)
                {
                    if (i >= cameras.Length) break;

                    if (keyboard[switchKeys[i]].wasPressedThisFrame)
                    {
                        SwitchCamera(i);
                    }
                }
            }
        }

        var gamepad = Gamepad.current;
        if(gamepad != null)
        {
            if (gamepad.leftTrigger.wasPressedThisFrame)
            {
                CycleNextCamera();
            }
            else if (gamepad.rightTrigger.wasPressedThisFrame)
            {
                CyclePreviousCamera();
            }
        }
    }

    private void CycleNextCamera()
    {
        int nextIndex = (currentCameraIndex + 1) % cameras.Length;
        SwitchCamera(nextIndex);
    }

    private void CheckPhotoModeToggle()
    {
        if (isPhotoMode != lastIsPhotoMode)
        {
            ApplyCameraPriorities();
            
            if (isPhotoMode)
            {
                // フォトモード開始時にカメラの現在の回転を同期
                if (photoCamera != null)
                {
                    Vector3 rotation = photoCamera.transform.eulerAngles;
                    photoYaw = rotation.y;
                    photoPitch = rotation.x;
                    // ピッチ角を -180 〜 180 の範囲に正規化
                    if (photoPitch > 180) photoPitch -= 360;
                }
            }
            else
            {
                // フォトモード終了時は強制的にキャプチャ（時間停止等）を解除
                if (isCapturing)
                {
                    SetCaptureMode(false);
                }
            }
            
            lastIsPhotoMode = isPhotoMode;
            Debug.Log($"フォトモード: {isPhotoMode}");
        }
    }

    private void CheckCaptureInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            SetCaptureMode(!isCapturing);
        }
    }

    private void SetCaptureMode(bool active)
    {
        isCapturing = active;
        Time.timeScale = active ? 0f : 1f;
        
        if (photoCanvas != null)
        {
            photoCanvas.enabled = !active;
        }
    }

    private void HandlePhotoCameraMovement()
    {
        if (photoCamera == null) return;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null) return;

        // --- 回転処理 ---
        if (mouse != null)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            photoYaw += mouseDelta.x * photoRotateSpeed;
            photoPitch -= mouseDelta.y * photoRotateSpeed;
            photoPitch = Mathf.Clamp(photoPitch, -85f, 85f); // 上下の回転制限

            photoCamera.transform.rotation = Quaternion.Euler(photoPitch, photoYaw, 0f);
        }

        // --- 移動処理 ---
        Vector3 moveInput = Vector3.zero;
        if (keyboard.wKey.isPressed) moveInput.z += 1;
        if (keyboard.sKey.isPressed) moveInput.z -= 1;
        if (keyboard.aKey.isPressed) moveInput.x -= 1;
        if (keyboard.dKey.isPressed) moveInput.x += 1;

        float verticalInput = 0;
        if (keyboard.qKey.isPressed) verticalInput -= 1;
        if (keyboard.eKey.isPressed) verticalInput += 1;

        // 水平移動 (カメラの向きに基づく)
        // 時間停止中でも移動できるよう unscaledDeltaTime を使用
        float deltaTime = Time.unscaledDeltaTime;

        Vector3 horizontalMove = (photoCamera.transform.forward * moveInput.z + photoCamera.transform.right * moveInput.x);
        horizontalMove.y = 0; // 水平移動に限定
        if (horizontalMove.magnitude > 0.1f)
        {
            photoCamera.transform.position += horizontalMove.normalized * photoMoveSpeed * deltaTime;
        }

        // 垂直移動
        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            photoCamera.transform.position += Vector3.up * verticalInput * photoVerticalSpeed * deltaTime;
        }
    }

    private void CyclePreviousCamera()
    {
        int nextIndex = (currentCameraIndex + 3) % cameras.Length;
        SwitchCamera(nextIndex);
    }

    private void SwitchCamera(int activeIndex)
    {
        currentCameraIndex = activeIndex;
        ApplyCameraPriorities();
        Debug.Log($"カメラ {activeIndex + 1} に切り替えました");
    }

    private void ApplyCameraPriorities()
    {
        // フォトモードが有効な場合はフォトカメラを最優先にする
        if (isPhotoMode && photoCamera != null)
        {
            photoCamera.Priority = 30;
            foreach (var cam in cameras)
            {
                if (cam != null) cam.Priority = 10;
            }
            return;
        }

        // 通常モード
        if (photoCamera != null) photoCamera.Priority = 5;

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null) continue;

            if (i == currentCameraIndex)
            {
                cameras[i].Priority = 20; // 選ばれたカメラ
            }
            else
            {
                cameras[i].Priority = 10; // それ以外
            }
        }
    }
}