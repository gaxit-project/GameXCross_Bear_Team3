using UnityEngine;
using TMPro;
using UniRx;
using DG.Tweening;

public class PhaseInfoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI flashText;

    private Tween flashBlinkTween;

    void Start()
    {
        // UIが見つからない場合は自動取得
        if (phaseText == null || timerText == null)
        {
            FindUIComponents();
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("【エラー】GameManagerが見つかりません！HierarchyにGameManagerがあるか確認してください。");
            return;
        }

        // フェーズ名の表示更新
        GameManager.Instance.CurrentState
            .Subscribe(state =>
            {
                flashBlinkTween?.Kill();
                if (flashBlinkTween != null) flashText.alpha = 1f;
                
                switch (state)
                {
                    case GameState.Setup:
                        if (phaseText != null)
                        {
                            phaseText.text = "準備フェーズ";
                            phaseText.color = Color.white; // 白くする
                            if (timerText != null) timerText.gameObject.SetActive(true);
                        }

                        if (flashText != null)
                            {
                                flashText.text = "Yでスキップ";
                                flashBlinkTween = flashText.DOFade(0.0f, 0.8f)
                                    .SetLoops(-1, LoopType.Yoyo)
                                    .SetEase(Ease.InOutSine);
                            }
                        break;
                    case GameState.Battle:
                        phaseText.text = "襲撃開始！！";
                        phaseText.color = Color.red; // 赤くする
                        if (timerText != null) timerText.gameObject.SetActive(false); // タイマーを消す
                        break;
                    case GameState.Result:
                        phaseText.text = "";
                        break;
                }
            })
            .AddTo(this);

        // 残り時間の表示更新
        GameManager.Instance.TimeRemaining
            .Subscribe(time =>
            {
                if (timerText == null) return;
                timerText.text = $"残り時間: {time:F0}";
            })
            .AddTo(this);

        // ウェーブを表示
        GameManager.Instance.CurrentWave
            .Subscribe(wave =>
            {
                Debug.Log($"UI更新: Wave {wave}");
            })
            .AddTo(this);
    }

    /// <summary>
    /// UIコンポーネントを自動取得
    /// </summary>
    private void FindUIComponents()
    {
        Debug.LogWarning("PhaseInfoUI: UIコンポーネントが見つかりません。自動取得を試みます。");

        // 同じCanvasの子要素から検索
        if (phaseText == null)
        {
            phaseText = GetComponentInChildren<TextMeshProUGUI>();
            if (phaseText != null)
            {
                Debug.Log($"phaseText を自動取得しました: {phaseText.gameObject.name}");
            }
        }

        if (timerText == null)
        {
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>();
            if (allTexts.Length > 1)
            {
                timerText = allTexts[1]; // 2番目のテキストを取得
                Debug.Log($"timerText を自動取得しました: {timerText.gameObject.name}");
            }
            else if (allTexts.Length == 1)
            {
                // 1つしかない場合は警告のみ
                Debug.LogWarning("PhaseInfoUI: timerTextが見つかりません。");
            }
        }
    }
}