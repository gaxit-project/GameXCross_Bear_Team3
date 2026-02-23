using UnityEngine;
using TMPro;

/// <summary>
/// リザルト画面のUI表示を担当するクラス
/// KillCountManagerからデータを読み取り表示する
/// </summary>
public class ResultUI : MonoBehaviour
{
    [Header("各日のテキスト - 駆除数")]
    [SerializeField] private TextMeshProUGUI day1KillText;
    [SerializeField] private TextMeshProUGUI day2KillText;
    [SerializeField] private TextMeshProUGUI day3KillText;

    [Header("各日のテキスト - 捕獲数")]
    [SerializeField] private TextMeshProUGUI day1CaptureText;
    [SerializeField] private TextMeshProUGUI day2CaptureText;
    [SerializeField] private TextMeshProUGUI day3CaptureText;

    [Header("各日のテキスト - 被害件数")]
    [SerializeField] private TextMeshProUGUI day1DamageText;
    [SerializeField] private TextMeshProUGUI day2DamageText;
    [SerializeField] private TextMeshProUGUI day3DamageText;

    [Header("合計テキスト")]
    [SerializeField] private TextMeshProUGUI totalKillText;
    [SerializeField] private TextMeshProUGUI totalCaptureText;
    [SerializeField] private TextMeshProUGUI totalDamageText;

    [Header("評価")]
    [SerializeField] private TextMeshProUGUI rankText;

    private void Start()
    {
        DisplayResults();
    }

    /// <summary>
    /// KillCountManagerからデータを取得して表示
    /// </summary>
    private void DisplayResults()
    {
        var manager = KillCountManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("KillCountManagerが見つかりません。データを表示できません。");
            return;
        }

        // 1日目のデータ
        SetText(day1KillText, manager.GetKillCount(1));
        SetText(day1CaptureText, manager.GetCaptureCount(1));
        SetText(day1DamageText, manager.GetDamageCount(1));

        // 2日目のデータ
        SetText(day2KillText, manager.GetKillCount(2));
        SetText(day2CaptureText, manager.GetCaptureCount(2));
        SetText(day2DamageText, manager.GetDamageCount(2));

        // 3日目のデータ
        SetText(day3KillText, manager.GetKillCount(3));
        SetText(day3CaptureText, manager.GetCaptureCount(3));
        SetText(day3DamageText, manager.GetDamageCount(3));

        // 合計データ
        SetText(totalKillText, manager.GetTotalKills());
        SetText(totalCaptureText, manager.GetTotalCaptures());
        SetText(totalDamageText, manager.GetTotalDamages());

        // 評価ランク
        if (rankText != null)
        {
            rankText.text = manager.GetRank();
        }

        Debug.Log($"リザルト表示完了 - 評価: {manager.GetRank()}");
    }

    /// <summary>
    /// テキストを安全に設定するヘルパーメソッド
    /// </summary>
    private void SetText(TextMeshProUGUI textComponent, int value)
    {
        if (textComponent != null)
        {
            textComponent.text = value.ToString();
        }
    }
}
