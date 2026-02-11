using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HowToPlayScroll : MonoBehaviour
{
    [Header("表示する画像")]
    [SerializeField] private GameObject page1;
    [SerializeField] private GameObject page2;

    // [Header("SE設定")]
    // [SerializeField] private string changeSeKey = "cursor";

    private int _currentPage = 0;
    private Vector2 _lastInput = Vector2.zero;

    void OnEnable()
    {
        // パネルが開いた時は1枚目を表示
        _currentPage = 0;
        UpdateDisplay();
    }

    void Update()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        // スティックまたは十字キーの入力を取得
        Vector2 input = gamepad.leftStick.ReadValue();
        if (input.magnitude < 0.5f) input = gamepad.dpad.ReadValue();

        // 左右入力の検知（押しっぱなし防止のため、前フレームと比較）
        if (input.x > 0.5f && _lastInput.x <= 0.5f)
        {
            ChangePage(1);
        }
        else if (input.x < -0.5f && _lastInput.x >= -0.5f)
        {
            ChangePage(-1);
        }

        _lastInput = input;
    }

    private void ChangePage(int direction)
    {
        int nextPage = Mathf.Clamp(_currentPage + direction, 0, 1);
        
        if (nextPage != _currentPage)
        {
            _currentPage = nextPage;
            // if (SEmanager.Instance != null) SEmanager.Instance.Play(changeSeKey);
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (page1 != null) page1.SetActive(_currentPage == 0);
        if (page2 != null) page2.SetActive(_currentPage == 1);
    }
}