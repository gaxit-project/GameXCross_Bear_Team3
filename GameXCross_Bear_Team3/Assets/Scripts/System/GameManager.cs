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

    // [Header("ゲームバランス")]
    public int SetupTime => Balance != null ? Balance.setupTime : 20;
    public int MaxWaves => Balance != null ? Balance.maxWaves : 3;

    [Header("参照")]
    [SerializeField] private money moneyScript;
    [SerializeField] private PointerContoroller pointerController;

    [Header("バランスデータ参照")]
    [SerializeField] private GameBalanceData balanceData;
    public GameBalanceData Balance => balanceData;


    // 公開プロパティ
    public ReactiveProperty<GameState> CurrentState { get; private set; }

    public ReactiveProperty<int> TimeRemaining { get; private set; }

    public ReactiveProperty<int> CurrentWave { get; private set; }

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
        if (Instance != null) Destroy(gameObject);
        if (Instance == null) Instance = this;

        if (Balance == null)
    {
        Debug.LogError("Balance が null です！");
    }
    else
    {
        Debug.Log($"Balance はセットされています。アセット内の値: {Balance.setupTime}");
    }

        TimeRemaining = new ReactiveProperty<int>(SetupTime);
        CurrentState = new ReactiveProperty<GameState>(GameState.Setup);
        CurrentWave = new ReactiveProperty<int>(1);


        InitializeObservables();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        /* if (balanceData != null)
        {
            setupTime = Balance.setupTime;
            maxWaves = Balance.maxWaves;
        } */

        // ゲーム開始時に統計データをリセット
        KillCountManager.Instance?.AlldataReset();

        StartSetupPhase();
    }

    private void Update()
    {
        // 何かが再表示しても毎フレーム強制で非表示にする
        if (Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
    public void StartSetupPhase()
    {
        CurrentState.Value = GameState.Setup;
        TimeRemaining.Value = SetupTime;
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
        _isWaveSpawningComplete = true;
        Debug.Log($"--- 第 {CurrentWave.Value} ウェーブ 襲撃開始 ---");
    }

    // ---------------------------------------------------------
    // 敵・家の登録と報告
    // ---------------------------------------------------------
    public void RegisterEnemy(BearController bear = null)
    {
        activeEnemies++;
        if (bear != null && !_activeEnemies.Contains(bear)) _activeEnemies.Add(bear);
        Debug.Log($"熊出現。残り: {activeEnemies}体");
    }

    public void ReportEnemyDefeated(BearController bear = null)
    {
        /* var enemies = FindObjectsByType<BearController>(FindObjectsSortMode.None);

        _activeEnemies.Remove(bear);
        activeEnemies--;
        Debug.Log($"熊撃退または捕獲。残り: {activeEnemies}体");

        // 「死んでいない」敵がまだいるかチェック（捕獲済みはIsDead=trueで判定）
        bool anyAlive = enemies.Any(e => !e.IsDead && e.gameObject.activeInHierarchy);

        if (!anyAlive && _isWaveSpawningComplete)
        {
            Debug.Log("すべての熊を撃退または捕獲しました！");
            FinishWave();
        } */

        if (bear != null)
        {
            _activeEnemies.Remove(bear);
        }
        activeEnemies--;
        Debug.Log($"熊撃退または捕獲。残り: {activeEnemies}体");

        if(_isWaveSpawningComplete && activeEnemies <= 0)
        {
            Debug.Log("すべての熊を撃退または捕獲しました！");
            FinishWave();
        }
    }

    public void NotifySpawningComplete()
    {
        _isWaveSpawningComplete = true;
        Debug.Log("全ての熊の生成が完了しました。");
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
        // ウェーブ終了時にデータを保存
        KillCountManager.Instance?.DataSet(CurrentWave.Value);
        KillCountManager.Instance?.CountReset();

        if (CurrentWave.Value < MaxWaves)
        {
            CurrentState.Value = GameState.Result;
            PhaseResult.Instance.Result();
            CurrentWave.Value++;
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

        // リザルト画面遷移前に現在のウェーブのデータを保存
        //KillCountManager.Instance?.DataSet(CurrentWave.Value);(一つ前の工程ですでに済ませてます．)

        Observable.Timer(TimeSpan.FromSeconds(sceneTransitionDelay))
            .Subscribe(_ => SceneManager.LoadScene(resultSceneName))
            .AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables?.Dispose();
    }
}