using UnityEngine;
using TMPro;
using UniRx;

public class PhaseInfoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private TextMeshProUGUI timerText;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("【エラー】GameManagerが見つかりません！HierarchyにGameManagerがあるか確認してください。");
            return;
        }

        // フェーズ名の表示更新
        GameManager.Instance.CurrentState
            .Subscribe(state =>
            {
                switch (state)
                {
                    case GameState.Setup:
                        phaseText.text = "準備フェーズ";
                        phaseText.color = Color.white; // 白くする
                        timerText.gameObject.SetActive(true);
                        break;
                    case GameState.Battle:
                        phaseText.text = "襲撃開始！！";
                        phaseText.color = Color.red; // 赤くする
                        timerText.gameObject.SetActive(false); // タイマーを消す
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
}