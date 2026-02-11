using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UniRx;
using DG.Tweening; // フェード処理にDOTweenを使用

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    [System.Serializable]
    public struct BGMSet
    {
        public AudioClip setup;
        public AudioClip battle;
        public AudioClip phaseResult;
        public AudioClip gameClear;
        public AudioClip gameOver;
        public AudioClip title;
    }

    [Header("BGMモード切り替え")]
    [SerializeField] private bool useBGM1 = true;

    [Header("BGMセット1")]
    [SerializeField] private BGMSet bgmSet1;

    [Header("BGMセット2")]
    [SerializeField] private BGMSet bgmSet2;

    [Header("シーン名設定")]
    [SerializeField] private string titleSceneName = "Title"; // タイトルシーン名
    [SerializeField] private string resultSceneName = "Result"; // リザルトシーン名

    [Header("音量・フェード設定")]
    [SerializeField] private float maxVolume = 0.5f;    // BGMの音量
    [SerializeField] private float fadeDuration = 1.0f; // 切り替えにかかる時間

    [Header("ゲームオーバー演出")]
    [SerializeField] private float roarInterval = 4.0f;
    [SerializeField] private string roarSEKey = "bear";

    private AudioSource _audioSource;
    private bool _isTitleScene = false;
    private bool _isFading = false; // フェード中かどうか
    private IDisposable _roarStream;

    private AudioClip CurrentSetupBGM => useBGM1 ? bgmSet1.setup : bgmSet2.setup;
    private AudioClip CurrentBattleBGM => useBGM1 ? bgmSet1.battle : bgmSet2.battle;
    private AudioClip CurrentPhaseResultBGM => useBGM1 ? bgmSet1.phaseResult : bgmSet2.phaseResult;
    private AudioClip CurrentTitleBGM => useBGM1 ? bgmSet1.title : bgmSet2.title;

    private AudioClip CurrentGameResultBGM
    {
        get
        {
            bool isClear = KillCountManager.IsGameClear;

            if (useBGM1)
            {
                return isClear ? bgmSet1.gameClear : bgmSet1.gameOver;
            }
            else
            {
                return isClear ? bgmSet2.gameClear : bgmSet2.gameOver;
            }
        }
    }

        private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopGameOverRoar();
        if (scene.name == titleSceneName)
        {
            Debug.Log($"BGMManager: タイトルシーン検知 -> {scene.name}");
            Time.timeScale = 1.0f;
            PlayNewClip(CurrentTitleBGM);
        }
        else if (scene.name == resultSceneName)
        {
            Debug.Log($"BGMManager: リザルトシーン検知 -> {scene.name}");
            PlayNewClip(CurrentGameResultBGM);

            if (!KillCountManager.IsGameClear)
            {
                StartGameOverRoar();
            }
        }
    }
    
    private void StartGameOverRoar()
    {
        // 重複防止
        StopGameOverRoar();

        Debug.Log("Game Over: 鳴き声ループ開始");

        // 指定秒数ごとに実行
        _roarStream = Observable.Interval(TimeSpan.FromSeconds(roarInterval))
            .Subscribe(_ =>
            {
                if (SEmanager.Instance != null)
                {
                    SEmanager.Instance.Play(roarSEKey);
                }
            })
            .AddTo(this);
    }

    // 鳴き声ループ停止
    private void StopGameOverRoar()
    {
        if (_roarStream != null)
        {
            _roarStream.Dispose();
            _roarStream = null;
        }
    }

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.volume = 0f;
    }


    public void SwitchBGM(GameState state)
    {
        if (_isTitleScene) return; // タイトル中はステート再生しない

        StopGameOverRoar();

        // _audioSourceがnullの場合は取得
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
        {
            Debug.LogError("BGMManager: AudioSourceが見つかりません。");
            return;
        }

        Debug.Log($"BGMManager: 状態が {state} になりました。");

        AudioClip nextClip = null;
        // フェーズに応じて曲を選ぶ
        switch (state)
        {
            case GameState.Setup:
                nextClip = CurrentSetupBGM;
                break;
            case GameState.Battle:
                nextClip = CurrentBattleBGM;
                break;
            case GameState.Result:
                nextClip = CurrentPhaseResultBGM;
                break;
        }

        // すでに流れている曲と同じなら何もしない
        if (_audioSource.clip == nextClip && _audioSource.isPlaying) return;

        PlayWithFade(nextClip);
    }

    private void PlayWithFade(AudioClip newClip)
    {
        if (_audioSource.isPlaying)
        {
            _isFading = true;
            // フェードアウトしてから新しいクリップを再生
            _audioSource.DOFade(0f, fadeDuration)
                .OnComplete(() =>
                {
                    PlayNewClip(newClip);
                });
        }
        else
        {
            // すでに停止している場合は直接再生
            PlayNewClip(newClip);
        }
    }

    private void PlayNewClip(AudioClip clip)
    {
        // _audioSourceがnullの場合は取得
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (_audioSource == null)
        {
            Debug.LogError("BGMManager: AudioSourceが見つかりません。");
            return;
        }

        if (clip != null)
        {
            Debug.Log($"BGMManager: 再生開始 -> {clip.name}");
            _isFading = true;
            _audioSource.clip = clip;
            _audioSource.volume = 0f; // 音量0から開始
            _audioSource.Play();

            _audioSource.DOFade(maxVolume * VolumeSettings.BGMVolume, fadeDuration) // フェードイン
                .OnComplete(() => { _isFading = false; });
        }
        else
        {
            _audioSource.Stop();
            _audioSource.clip = null;
            _isFading = false;
        }
    }

    private void Update()
    {
        // 設定シーンでのスライダー操作をリアルタイムに反映
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.volume = maxVolume * VolumeSettings.BGMVolume;
        }
    }
}