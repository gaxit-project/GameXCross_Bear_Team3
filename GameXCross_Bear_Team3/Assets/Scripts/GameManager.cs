using DG.Tweening;
using System;
using System.Linq;
using UniRx;
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
    [SerializeField] private float setupTime = 30.0f;
    [SerializeField] private int maxWaves = 3;

    [Header("参照")]
    [SerializeField] private money moneyScript;

    [Header("バランスデータ参照")]
    [SerializeField] private GameBalanceData balanceData;
    public GameBalanceData Balance => balanceData;

    // 公開プロパティ
    public ReactiveProperty<GameState> CurrentState { get; private set; }
        = new ReactiveProperty<GameState>(GameState.Setup);

    public ReactiveProperty<float> TimeRemaining { get; private set; }
        = new ReactiveProperty<float>();

    public ReactiveProperty<int> CurrentWave { get; private set; }
        = new ReactiveProperty<int>(1);

    // シーン内の実体を管理するコレクション
    private readonly ReactiveCollection<BearController> _activeEnemies = new();
    private readonly ReactiveCollection<HouseHealth> _activeHouses = new();

    // 内部変数
    private int activeEnemies = 0;
    private int _pendingIncome = 0;
    private bool _isWaveSpawningComplete = false;
    private CompositeDisposable _disposables = new CompositeDisposable();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        InitializeObservables();
    }

    private void Start()
    {
        if (moneyScript == null)
        {
            moneyScript = FindFirstObjectByType<money>();
        }

        if (balanceData != null)
        {
            setupTime = balanceData.setupTime;
            maxWaves = balanceData.maxWaves;
        }

        StartSetupPhase();
    }

    private void InitializeObservables()
    {
        // 敵のリストが0になった時の監視
        _activeEnemies.ObserveCountChanged()
            .Where(count => count == 0 && _isWaveSpawningComplete && CurrentState.Value == GameState.Battle)
            .Subscribe(_ => FinishWave())
            .AddTo(_disposables);

        // 家のリストが0になった時の監視
        _activeHouses.ObserveCountChanged()
            .Where(count => count == 0 && CurrentState.Value == GameState.Battle)
            .Subscribe(_ => TransitionToResultScene(false))
            .AddTo(_disposables);
    }

    // ---------------------------------------------------------
    // フェーズ管理
    // ---------------------------------------------------------
    private void StartSetupPhase()
    {
        CleanupEnemies();

        // 報酬の支払い
        if (_pendingIncome > 0 && moneyScript != null)
        {
            moneyScript.moneycount += _pendingIncome;
            Debug.Log($"捕獲報酬: {_pendingIncome}円 を獲得しました！");
            _pendingIncome = 0;
        }

        CurrentState.Value = GameState.Setup;
        TimeRemaining.Value = setupTime;

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
            .AddTo(_disposables);
    }

    private void StartBattlePhase()
    {
        CurrentState.Value = GameState.Battle;
        TimeRemaining.Value = 0;
        _isWaveSpawningComplete = false;
        Debug.Log($"--- 第 {CurrentWave.Value} ウェーブ 襲撃開始 ---");
    }

    // ---------------------------------------------------------
    // 敵・家の登録と報告
    // ---------------------------------------------------------
    public void RegisterEnemy(BearController bear = null)
    {
        activeEnemies++;
        if (bear != null && !_activeEnemies.Contains(bear)) _activeEnemies.Add(bear);
        Debug.Log($"敵出現。残り敵数: {activeEnemies}");
    }

    public void ReportEnemyDefeated(BearController bear = null)
    {
        if (CurrentState.Value != GameState.Battle) return;

        if (activeEnemies > 0) activeEnemies--;
        if (bear != null) _activeEnemies.Remove(bear);

        Debug.Log($"敵撃破。残り敵数: {activeEnemies}");

        if (activeEnemies <= 0 && _isWaveSpawningComplete)
        {
            FinishWave();
        }
    }

    public void NotifySpawningComplete()
    {
        _isWaveSpawningComplete = true;
        Debug.Log("全ての敵の生成が完了しました。");
        if (activeEnemies <= 0) FinishWave();
    }

    public void RegisterHouse(HouseHealth house) => _activeHouses.Add(house);
    public void ReportHouseDestroyed(HouseHealth house) => _activeHouses.Remove(house);

    public void AddPendingReward(int amount)
    {
        _pendingIncome += amount;
        Debug.Log($"報酬 {amount}円 ストック。合計: {_pendingIncome}円");
    }

    private void FinishWave()
    {
        if (CurrentWave.Value < maxWaves)
        {
            CurrentWave.Value++;
            StartSetupPhase();
        }
        else
        {
            TransitionToResultScene(true);
        }
    }

    private void CleanupEnemies()
    {
        var allBears = FindObjectsByType<BearController>(FindObjectsSortMode.None);
        foreach (var bear in allBears)
        {
            if (bear.IsParalyzed)
            {
                GameObject trap = bear.GetAssignedTrap();
                if (trap != null) Destroy(trap);
                bear.transform.DOScale(Vector3.zero, 0.5f).OnComplete(() => Destroy(bear.gameObject));
            }
        }
    }

    private void TransitionToResultScene(bool isClear)
    {
        if (CurrentState.Value == GameState.Result) return;
        CurrentState.Value = GameState.Result;

        Observable.Timer(TimeSpan.FromSeconds(sceneTransitionDelay))
            .Subscribe(_ => SceneManager.LoadScene(resultSceneName))
            .AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables?.Dispose();
    }
}