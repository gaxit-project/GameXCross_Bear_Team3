using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PhaseResult : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject phaseResult;
    [SerializeField] private GameObject button;
    [SerializeField] private GameObject selectUIbutton;

    [Header("表示")]
    [SerializeField] private TextMeshProUGUI day;
    [SerializeField] private TextMeshProUGUI kill;
    [SerializeField] private TextMeshProUGUI capture;
    [SerializeField] private TextMeshProUGUI damage;


    private int daycount = 1;
    private int killcount = 0;
    private int capturecount = 0;
    private int damagecount = 0;


    public static PhaseResult Instance { get; private set; }

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

    public void Result()
    {
        
        phaseResult.SetActive(true);
        EventSystem.current.SetSelectedGameObject(button);

        killcount = KillCountManager.Instance.GetKillCount(daycount);
        capturecount = KillCountManager.Instance.GetCaptureCount(daycount);
        damagecount = KillCountManager.Instance.GetDamageCount(daycount);
        resulttype();

    }

    private void resulttype()
    {
        day.text = daycount + "日目報告"; 
        kill.text = "" + killcount;
        capture.text = "" + capturecount;
        damage.text = "" + damagecount;
    }

    public void NextDay()
    {
        phaseResult.SetActive(false);
        daycount++;
        GameManager.Instance.StartSetupPhase();
        EventSystem.current.SetSelectedGameObject(selectUIbutton);
    }

    /// <summary>
    /// 新しいゲーム開始時に状態をリセットする
    /// タイトルシーンへ戻る際に呼び出される
    /// </summary>
    public void ResetForNewGame()
    {
        daycount = 1;
        killcount = 0;
        capturecount = 0;
        damagecount = 0;
        if (phaseResult != null)
        {
            phaseResult.SetActive(false);
        }
    }
}