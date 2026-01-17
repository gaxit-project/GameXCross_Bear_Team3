using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillCountManager : MonoBehaviour
{
    public static KillCountManager Instance { get; private set; }

    [SerializeField] int killcount = 0;
    [SerializeField] int capturecount = 0;
    [SerializeField] int victimcount = 0;

    [Header("データ")]
    [SerializeField] static int[] finalkill = new int[3];
    [SerializeField] static int[] finalcapture = new int[3];
    [SerializeField] static int[] finalvictim = new int[3];
    [SerializeField] static int[] finalPO = new int[3];

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

    public void KillCounterplus()
    {
        killcount++;
    }
    public void Victimconterplus()
    {
        victimcount++;
    }
    public void Capturecounterplus()
    {
        capturecount++;
    }

    /// <summary>
    /// 現在カウント中の値のみ消します．これまでのデータはまだ消えません.
    /// </summary>
    public void CountReset()
    {
        killcount = 0;
        victimcount = 0;
        capturecount = 0;
    }

    /// <summary>
    /// 1～3日目の結果を消します．現在カウント中の値は消えません．
    /// </summary>
    public void AlldataReset()
    {
        Array.Clear(finalcapture, 0, finalcapture.Length);
        Array.Clear(finalvictim, 0, finalvictim.Length);
        Array.Clear(finalPO, 0, finalPO.Length);
        Array.Clear(finalkill, 0, finalkill.Length);
    }

    /// <summary>
    /// 現在のカウントを最終データとして格納します．引数には何日目に入れるかを入力してください．
    /// </summary>
    /// <param name="day"></param>
    public void DataSet(int day)
    {
        finalkill[day - 1] = killcount;
        finalcapture[day - 1] = capturecount;
        finalvictim[day - 1] = victimcount;
        finalPO[day - 1] =　PublicOpinionManager.Instance.GetPOpercent();
    }
}
