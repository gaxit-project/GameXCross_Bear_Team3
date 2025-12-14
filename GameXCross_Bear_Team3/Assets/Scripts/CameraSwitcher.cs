using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UniRx;
using TMPro;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineCamera[] cameras;

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

    private void Start()
    {
        // 配列が空でないかチェック
        if (cameras == null || cameras.Length == 0) return;

        // 初期化：最初のカメラだけ優先度を高くする
        SwitchCamera(0);

        // 毎フレーム入力を監視
        Observable.EveryUpdate()
            .Subscribe(_ => CheckInput())
            .AddTo(this);
    }

    private void CheckInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int i = 0; i < switchKeys.Length; i++)
        {
            if (i >= cameras.Length) break;

            if (keyboard[switchKeys[i]].wasPressedThisFrame)
            {
                SwitchCamera(i);
            }
        }

        var gamepad = Gamepad.current;
        if(gamepad != null)
        {
            if (gamepad.buttonWest.wasPressedThisFrame)
            {
                CycleNextCamera();
            }
        }
    }

    private void CycleNextCamera()
    {
        int nextIndex = (currentCameraIndex + 1) % cameras.Length;
        SwitchCamera(nextIndex);
    }

    private void SwitchCamera(int activeIndex)
    {
        currentCameraIndex = activeIndex;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null) continue;

            if (i == activeIndex)
            {
                cameras[i].Priority = 20; // 選ばれたカメラ
            }
            else
            {
                cameras[i].Priority = 10; // それ以外
            }
        }
        Debug.Log($"カメラ {activeIndex + 1} に切り替えました");
    }
}