using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PhaseResult : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject phaseResult;
    [SerializeField] private GameObject button;
    [SerializeField] private GameObject selectUIbutton;
    [SerializeField] private GameObject UI;

    [Header("表示")]
    [SerializeField] private TextMeshProUGUI day;
    [SerializeField] private TextMeshProUGUI kill;
    [SerializeField] private TextMeshProUGUI capture;
    [SerializeField] private TextMeshProUGUI damage;
    [SerializeField] private TextMeshProUGUI PO;
    [SerializeField] private TextMeshProUGUI money;

    [Header("参照")]
    [SerializeField] private money m;

    // シーン内で使用するためのInstance（DontDestroyOnLoadは使わない）
    public static PhaseResult Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        phaseResult.SetActive(false); 
    }

    private void OnDestroy()
    {
        // シーン破棄時にInstanceをクリア
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 現在の日数を取得（GameManagerから参照）
    /// </summary>
    private int CurrentDay => GameManager.Instance != null ? GameManager.Instance.CurrentWave.Value : 1;

    public void Result()
    {
        UI.SetActive(false);
        if (phaseResult == null)
        {
            Debug.LogWarning("PhaseResult: phaseResultパネルが設定されていません");
            return;
        }

        Time.timeScale = 0f;

        // 結果表示前に現在の日のデータを保存
        int daycount = CurrentDay;
        //if (KillCountManager.Instance != null)
        //{
        //    KillCountManager.Instance.DataSet(daycount);
        //}
        
        phaseResult.SetActive(true);
        if (button != null)
        {
            EventSystem.current.SetSelectedGameObject(button);
        }

        int killcount = KillCountManager.Instance != null ? KillCountManager.Instance.GetKillCount(daycount) : 0;
        int capturecount = KillCountManager.Instance != null ? KillCountManager.Instance.GetCaptureCount(daycount) : 0;
        int damagecount = KillCountManager.Instance != null ? KillCountManager.Instance.GetDamageCount(daycount) : 0;
        int POcount = PublicOpinionManager.Instance != null ? PublicOpinionManager.Instance.GetPOpercent() : 0;
        
        // 結果表示
        if (day != null) day.text = daycount + "日目の報告";
        if (kill != null) kill.text = killcount + "体";
        if (capture != null) capture.text = capturecount + "体";
        if (damage != null) damage.text = damagecount + "件";
        if (PO != null) PO.text = POcount + "％";
        if (money != null) money.text = m.moneycount.ToString("N0") + "円";
    }

    public void NextDay()
    {
        UI.SetActive(true);
        if (phaseResult != null)
        {
            phaseResult.SetActive(false);
        }
        GameManager.Instance?.StartSetupPhase();
        if (selectUIbutton != null)
        {
            EventSystem.current.SetSelectedGameObject(selectUIbutton);
        }
        Time.timeScale = 1f;
    }
}