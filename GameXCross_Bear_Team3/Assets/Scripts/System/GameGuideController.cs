using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

/// <summary>
/// Startボタン押下後に4ページのGAME GUIDEを表示するコントローラー。
/// 左スティック/十字キーの左右でページ切替、Yボタンでスキップ、
/// 最終ページで「ゲームを始める」ボタンを表示する。
/// </summary>
public class GameGuideController : MonoBehaviour
{
    [Header("ガイドUI")]
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private GameObject[] pages;
    [SerializeField] private GameObject startButton;
    [SerializeField] private TextMeshProUGUI skipText;

    [Header("点滅アニメーション設定")]
    [SerializeField] private float blinkDuration = 0.8f;
    [SerializeField] private float blinkMinAlpha = 0.2f;

    [Header("参照")]
    [SerializeField] private ButtonController buttonController;

    [Header("ガイド表示中に非表示にするオブジェクト")]
    [Tooltip("タイトルメニューのパネル（Start/Setting/Help/Quit等）を設定")]
    [SerializeField] private GameObject[] hideOnGuide;

    [Header("SE設定")]
    [SerializeField] private string pageChangeSE = "cursor";
    [SerializeField] private string skipSE = "deside";

    private int _currentPage = 0;
    private bool _isActive = false;
    private Vector2 _lastInput = Vector2.zero;
    private Tween _skipTextBlink;

    /// <summary>
    /// ガイドを表示する（StartボタンのOnClickから呼び出される）
    /// </summary>
    public void ShowGuide()
    {
        if (guidePanel == null || pages == null || pages.Length == 0) return;

        _currentPage = 0;
        _isActive = true;
        _lastInput = Vector2.zero;

        guidePanel.SetActive(true);

        // スキップテキストの点滅アニメーション開始
        StartSkipTextBlink();

        // 裏のメニューパネルを非表示にする（入力が透過しないように）
        if (hideOnGuide != null)
        {
            foreach (var obj in hideOnGuide)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        // EventSystemの選択をクリア（裏のボタンにフォーカスが残らないように）
        EventSystem.current.SetSelectedGameObject(null);

        // StartGameButtonは最初は非表示
        if (startButton != null)
        {
            startButton.SetActive(false);
        }

        UpdateDisplay();

        // SE再生
        if (SEmanager.Instance != null)
        {
            SEmanager.Instance.Play("deside");
        }
    }

    void Update()
    {
        if (!_isActive) return;

        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        // Yボタンでスキップ → 即座にMainシーンへ遷移
        if (gamepad.yButton.wasPressedThisFrame)
        {
            SkipGuide();
            return;
        }

        // 左スティックまたは十字キーの入力を取得
        Vector2 input = gamepad.leftStick.ReadValue();
        if (input.magnitude < 0.5f) input = gamepad.dpad.ReadValue();

        // 右入力 → 次のページ
        if (input.x > 0.5f && _lastInput.x <= 0.5f)
        {
            ChangePage(1);
        }
        // 左入力 → 前のページ
        else if (input.x < -0.5f && _lastInput.x >= -0.5f)
        {
            ChangePage(-1);
        }

        _lastInput = input;
    }

    private void ChangePage(int direction)
    {
        int maxPage = pages.Length - 1;
        int nextPage = Mathf.Clamp(_currentPage + direction, 0, maxPage);

        if (nextPage != _currentPage)
        {
            _currentPage = nextPage;

            // ページ切替SE
            if (SEmanager.Instance != null)
            {
                SEmanager.Instance.Play(pageChangeSE);
            }

            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        // 全ページを非表示にしてから現在のページのみ表示
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == _currentPage);
            }
        }

        // 最終ページの場合のみ「ゲームを始める」ボタンを表示
        bool isLastPage = (_currentPage == pages.Length - 1);
        if (startButton != null)
        {
            startButton.SetActive(isLastPage);

            // 最終ページでボタンにフォーカスを当てる
            if (isLastPage)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(startButton);
            }
        }
    }

    /// <summary>
    /// Yボタンスキップ処理
    /// </summary>
    private void SkipGuide()
    {
        _isActive = false;

        // アニメーション停止
        _skipTextBlink?.Kill();
        _skipTextBlink = null;

        // スキップSE
        if (SEmanager.Instance != null)
        {
            SEmanager.Instance.Play(skipSE);
        }

        // ガイドパネルは表示したままMainシーンへ遷移
        // （非表示にするとタイトル背景が一瞬見えてしまうため）
        if (buttonController != null)
        {
            buttonController.SwithToMain();
        }
    }

    // ========== 点滅アニメーション ==========

    private void StartSkipTextBlink()
    {
        if (skipText == null) return;

        // アルファを完全不透明に初期化
        Color c = skipText.color;
        c.a = 1f;
        skipText.color = c;

        _skipTextBlink?.Kill();
        _skipTextBlink = skipText.DOFade(blinkMinAlpha, blinkDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void OnDestroy()
    {
        _skipTextBlink?.Kill();
        _skipTextBlink = null;
    }
}
