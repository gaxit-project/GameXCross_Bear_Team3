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
    [SerializeField] private PointerContoroller pointerController;

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
        if (pointerController == null)
        {
            pointerController = FindFirstObjectByType<PointerContoroller>();
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

        // 家のリストが0になった時の監視（Setup中でもBattle中でも反応）
        _activeHouses.ObserveCountChanged()
            .Where(count => count == 0 && CurrentState.Value != GameState.Result)
            .Subscribe(_ =>
            {
                Debug.Log("全ての家が破壊されました！ゲームオーバー");
                TransitionToResultScene(false);
            })
            .AddTo(_disposables);
    }

    // ---------------------------------------------------------
    // フェーズ管理
    // ---------------------------------------------------------
    private void StartSetupPhase()
    {
        CurrentState.Value = GameState.Setup;
        TimeRemaining.Value = setupTime;
        pointerController.UIsettrue();

        // 報酬の支払い
        if (_pendingIncome > 0)
        {
            if (moneyScript != null)
            {
                moneyScript.moneycount += _pendingIncome;
                Debug.Log($"捕獲報酬: {_pendingIncome}円 を獲得しました！");
            }
            _pendingIncome = 0;
        }

        CleanupEnemies();

        // カウントダウンタイマーの開始
        _disposables.Clear(); // リセット

        Observable.Interval(TimeSpan.FromSeconds(1))
            .TakeWhile(_ => CurrentState.Value == GameState.Setup)
            .Subscribe(_ =>
            {
                TimeRemaining.Value -= 1;
                if (TimeRemaining.Value <= 0)
                {
                    StartBattlePhase(); // 0秒になるとバトル開始
                }
            })
            .AddTo(_disposables);
    }

    private void StartBattlePhase()
    {
        pointerController.UIsetfalse();
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
        var enemies = FindObjectsByType<BearController>(FindObjectsSortMode.None);

        // 「死んでいない」敵がまだいるかチェック（捕獲済みはIsDead=trueで判定）
        bool anyAlive = enemies.Any(e => !e.IsDead && e.gameObject.activeInHierarchy);

        if (!anyAlive && _isWaveSpawningComplete)
        {
            Debug.Log("すべての敵を撃退または捕獲しました！");
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
    
    public void ReportHouseDestroyed(HouseHealth house)
    {
        _activeHouses.Remove(house);
        
        // 家が残っているか即座に確認
        if (_activeHouses.Count == 0 && CurrentState.Value != GameState.Result)
        {
            Debug.Log("全ての家が破壊されました！ゲームオーバー");
            TransitionToResultScene(false);
        }
    }

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