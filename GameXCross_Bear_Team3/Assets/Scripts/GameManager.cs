using System;
using System.Linq;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Setup,  // 準備期間
    Battle, // 襲撃中
    Result  // 結果画面
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("シーン設定")]
    [SerializeField] private string resultSceneName = "Result";
    [SerializeField] private float sceneTransitionDelay = 2.0f;

    [Header("ゲームバランス")]
    [SerializeField] private float setupTime = 30.0f; // 準備時間
    [SerializeField] private int maxWaves = 3;        // 最大ウェーブ数

    [Header("参照")]
    [SerializeField] private money moneyScript;

    [Header("バランスデータ参照")]
    [SerializeField] private GameBalanceData balanceData; // ここにアセットをアタッチ
    public GameBalanceData Balance => balanceData; // 他クラスからのアクセサ

    // 公開プロパティ
    public ReactiveProperty<GameState> CurrentState { get; private set; }
        = new ReactiveProperty<GameState>(GameState.Setup);

    public ReactiveProperty<float> TimeRemaining { get; private set; }
        = new ReactiveProperty<float>();

    public ReactiveProperty<int> CurrentWave { get; private set; }
        = new ReactiveProperty<int>(1); // 1ウェーブ目から開始

    private int activeEnemies = 0; // 現在生存している敵の数
    private int pendingIncome = 0; // 次のフェーズで入る予定のお金
    private HouseHealth[] allHouses;
    private CompositeDisposable disposables = new CompositeDisposable();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (moneyScript == null)
        {
            moneyScript = FindFirstObjectByType<money>();
        }

        // バランスデータから設定時間を上書き
        if (balanceData != null)
        {
            setupTime = balanceData.setupTime;
            maxWaves = balanceData.maxWaves;
        }

        RefreshHouseList();
        StartSetupPhase();
    }

    private void OnDestroy()
    {
        disposables?.Dispose();
    }

    // ---------------------------------------------------------
    // フェーズ管理
    // ---------------------------------------------------------
    private void StartSetupPhase()
    {
        // 前のウェーブで捕獲した分のお金を支払う
        if (pendingIncome > 0 && moneyScript != null)
        {
            moneyScript.moneycount += pendingIncome;
            Debug.Log($"捕獲報酬: {pendingIncome}円 を獲得しました！");
            pendingIncome = 0; // リセット
        }

        CurrentState.Value = GameState.Setup;
        TimeRemaining.Value = setupTime;
        Debug.Log($"--- 第 {CurrentWave.Value} ウェーブ 準備開始 ---");

        // カウントダウン
        Observable.Interval(TimeSpan.FromSeconds(1))
            .TakeWhile(_ => CurrentState.Value == GameState.Setup)
            .Subscribe(_ =>
            {
                TimeRemaining.Value -= 1;
                if (TimeRemaining.Value <= 0)
                {
                    StartBattlePhase();
                }
            })
            .AddTo(disposables);
    }

    private void StartBattlePhase()
    {
        CurrentState.Value = GameState.Battle;
        TimeRemaining.Value = 0;
        activeEnemies = 0; // カウントリセット

        Debug.Log($"--- 第 {CurrentWave.Value} ウェーブ 襲撃開始 ---");

        // 家の生存チェック
        Observable.Interval(TimeSpan.FromSeconds(0.5f))
            .Where(_ => CurrentState.Value == GameState.Battle)
            .Subscribe(_ => CheckAllHousesDestroyed())
            .AddTo(disposables);
    }

    /// <summary>
    /// 敵の生成を登録する
    /// </summary>
    public void RegisterEnemy()
    {
        activeEnemies++;
        Debug.Log($"敵出現。残り敵数: {activeEnemies}");
    }

    /// <summary>
    /// 敵が倒されたことを報告する
    /// </summary>
    public void ReportEnemyDefeated()
    {
        if (CurrentState.Value != GameState.Battle) return;

        activeEnemies--;
        Debug.Log($"敵撃破。残り敵数: {activeEnemies}");

        if (activeEnemies <= 0)
        {
            FinishWave();
        }
    }

    /// <summary>
    /// 捕獲報酬を保留リストに追加する
    /// </summary>
    public void AddPendingReward(int amount)
    {
        pendingIncome += amount;
        Debug.Log($"捕獲報酬 {amount}円 をストックしました。(現在のストック: {pendingIncome}円)");
    }

    private void FinishWave()
    {
        Debug.Log($"ウェーブ {CurrentWave.Value} クリア！");

        if (CurrentWave.Value < maxWaves)
        {
            // 次のウェーブへ
            CurrentWave.Value++;
            StartSetupPhase();
        }
        else
        {
            // 全ウェーブクリア
            Debug.Log("全ウェーブクリア！勝利！");
            TransitionToResultScene(true); // true = クリア
        }
    }

    // ---------------------------------------------------------
    // 家の状態管理
    // ---------------------------------------------------------
    public void RefreshHouseList()
    {
        var houseObjects = GameObject.FindGameObjectsWithTag("House");
        allHouses = houseObjects
            .Select(h => h.GetComponent<HouseHealth>())
            .Where(h => h != null)
            .ToArray();
    }

    private void CheckAllHousesDestroyed()
    {
        if (allHouses == null || allHouses.Length == 0) return;

        bool allDestroyed = allHouses.All(h => h == null || !h.gameObject.activeSelf || h.IsDestroyed);

        if (allDestroyed)
        {
            Debug.Log("全滅しました... ゲームオーバー");
            TransitionToResultScene(false); // false = 敗北
        }
    }

    private void TransitionToResultScene(bool isClear)
    {
        if (CurrentState.Value == GameState.Result) return;

        CurrentState.Value = GameState.Result;

        Observable.Timer(TimeSpan.FromSeconds(sceneTransitionDelay))
            .Subscribe(_ =>
            {
                try { SceneManager.LoadScene(resultSceneName); }
                catch (Exception e) { Debug.LogError(e.Message); }
            })
            .AddTo(disposables);
    }
}