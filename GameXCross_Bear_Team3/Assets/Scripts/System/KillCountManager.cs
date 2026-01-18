<<<<<<< HEAD
using System;
using System.Linq;
=======
using TMPro;
>>>>>>> parent of 58fa75f (Merge branch 'develop' of https://github.com/gaxit-project/GameXCross_Bear_Team3 into develop)
using UnityEngine;

public class KillCountManager : MonoBehaviour
{
    public static KillCountManager Instance { get; private set; }

<<<<<<< HEAD
    [SerializeField] int killcount = 0;
    [SerializeField] int capturecount = 0;
    [SerializeField] int damagecount = 0;  // 被害件数（家の破壊数）

    [Header("データ")]
    [SerializeField] static int[] finalkill = new int[3];
    [SerializeField] static int[] finalcapture = new int[3];
    [SerializeField] static int[] finaldamage = new int[3];  // 被害件数（家の破壊数）
    [SerializeField] static int[] finalPO = new int[3];
=======
    [SerializeField] int killcount;
    [SerializeField] int victimcount;

    [SerializeField] TextMeshProUGUI killtext;
    [SerializeField] TextMeshProUGUI victimtext;
>>>>>>> parent of 58fa75f (Merge branch 'develop' of https://github.com/gaxit-project/GameXCross_Bear_Team3 into develop)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

<<<<<<< HEAD
    #region カウント操作
    /// <summary>
    /// 駆除数を1増やす
    /// </summary>
    public void KillCounterplus()
    {
        killcount++;
    }

    /// <summary>
    /// 被害件数（家の破壊数）を1増やす
    /// </summary>
    public void DamageCounterplus()
=======
    public void killCounterplus()
    {
        killcount++;
    }
    public void victimconterplus()
>>>>>>> parent of 58fa75f (Merge branch 'develop' of https://github.com/gaxit-project/GameXCross_Bear_Team3 into develop)
    {
        damagecount++;
    }
<<<<<<< HEAD

    /// <summary>
    /// 捕獲数を1増やす
    /// </summary>
    public void Capturecounterplus()
    {
        capturecount++;
    }
    #endregion

    #region データリセット
    /// <summary>
    /// 現カウントの値のみ初期化する。今までのデータは消えない。
    /// </summary>
    public void CountReset()
    {
        killcount = 0;
        damagecount = 0;
        capturecount = 0;
    }

    /// <summary>
    /// 1〜3日目の結果を消す。現カウントの値も初期化される。
    /// </summary>
    public void AlldataReset()
    {
        Array.Clear(finalkill, 0, finalkill.Length);
        Array.Clear(finalcapture, 0, finalcapture.Length);
        Array.Clear(finaldamage, 0, finaldamage.Length);
        Array.Clear(finalPO, 0, finalPO.Length);
        CountReset();
    }
    #endregion

    #region データ保存
    /// <summary>
    /// 現在のカウントを最終データとして格納する。引数には何日目に入るかを入れる。
    /// </summary>
    /// <param name="day">1〜3の日数</param>
    public void DataSet(int day)
    {
        if (day < 1 || day > 3) return;
        finalkill[day - 1] = killcount;
        finalcapture[day - 1] = capturecount;
        finaldamage[day - 1] = damagecount;
        if (PublicOpinionManager.Instance != null)
            finalPO[day - 1] = PublicOpinionManager.Instance.GetPOpercent();
=======

    public void Counttype()
    {
        killtext.text = ""+ killcount;
        victimtext.text = ""+ victimcount;
        killcount= 0;
        victimcount = 0;
>>>>>>> parent of 58fa75f (Merge branch 'develop' of https://github.com/gaxit-project/GameXCross_Bear_Team3 into develop)
    }
    #endregion

    #region データ取得（各日）
    /// <summary>
    /// 指定した日の駆除数を取得
    /// </summary>
    public int GetKillCount(int day) => (day >= 1 && day <= 3) ? finalkill[day - 1] : 0;

    /// <summary>
    /// 指定した日の捕獲数を取得
    /// </summary>
    public int GetCaptureCount(int day) => (day >= 1 && day <= 3) ? finalcapture[day - 1] : 0;

    /// <summary>
    /// 指定した日の被害件数（家の破壊数）を取得
    /// </summary>
    public int GetDamageCount(int day) => (day >= 1 && day <= 3) ? finaldamage[day - 1] : 0;

    /// <summary>
    /// 指定した日の世論値を取得
    /// </summary>
    public int GetPOCount(int day) => (day >= 1 && day <= 3) ? finalPO[day - 1] : 0;
    #endregion

    #region データ取得（合計）
    /// <summary>
    /// 全日の駆除数合計を取得
    /// </summary>
    public int GetTotalKills() => finalkill.Sum();

    /// <summary>
    /// 全日の捕獲数合計を取得
    /// </summary>
    public int GetTotalCaptures() => finalcapture.Sum();

    /// <summary>
    /// 全日の被害件数（家の破壊数）合計を取得
    /// </summary>
    public int GetTotalDamages() => finaldamage.Sum();
    #endregion

    #region 評価計算
    /// <summary>
    /// 総合評価ランクを取得（S〜D）
    /// </summary>
    public string GetRank()
    {
        int score = GetTotalKills() * 10 + GetTotalCaptures() * 15 - GetTotalDamages() * 20;
        if (score >= 100) return "S";
        if (score >= 70) return "A";
        if (score >= 40) return "B";
        if (score >= 10) return "C";
        return "D";
    }
    #endregion
}
