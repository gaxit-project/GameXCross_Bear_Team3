using UnityEngine;
using UniRx;
using DG.Tweening; // フェード処理にDOTweenを使用
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    [Header("BGMクリップ設定")]
    [SerializeField] private AudioClip setupBGM;  // 準備フェーズ
    [SerializeField] private AudioClip battleBGM; // 襲撃フェーズ
    [SerializeField] private AudioClip resultBGM; // リザルト
    [SerializeField] private AudioClip titleBGM;  // タイトル用BGMを追加

    [Header("シーン名設定")]
    [SerializeField] private string titleSceneName = "Title"; // タイトルシーン名

    [Header("音量・フェード設定")]
    [SerializeField] private float maxVolume = 0.5f;    // BGMの音量
    [SerializeField] private float fadeDuration = 1.0f; // 切り替えにかかる時間

    private AudioSource _audioSource;
    private bool _isTitleScene = false;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.volume = 0f;

        // 現在シーンがタイトルなら即再生
        _isTitleScene = SceneManager.GetActiveScene().name == titleSceneName;
        if (_isTitleScene)
        {
            PlayNewClip(titleBGM);
        }
        else
        {
            // GameManagerが初期化されるまで待機してからサブスクライブ
            WaitForGameManagerAndSubscribe();
        }
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene prev, Scene next)
    {
        _isTitleScene = next.name == titleSceneName;

        if (_isTitleScene)
        {
            PlayNewClip(titleBGM); // タイトルBGM再生
        }
        else
        {
            // タイトルから出たら状態監視を始める（未登録なら登録）
            if (GameManager.Instance != null)
            {
                SwitchBGM(GameManager.Instance.CurrentState.Value);
            }
            else
            {
                WaitForGameManagerAndSubscribe();
            }
        }
    }

    private void WaitForGameManagerAndSubscribe()
    {
        // GameManager.Instanceがnullの場合、毎フレームチェックして待機
        Observable.EveryUpdate()
            .Where(_ => GameManager.Instance != null)
            .First() // 最初に見つかったら1回だけ実行
            .Subscribe(_ =>
            {
                Debug.Log("BGMManager: GameManagerを検出しました。BGM監視を開始します。");

                GameManager.Instance.CurrentState
                    .DistinctUntilChanged()
                    .Subscribe(state => SwitchBGM(state))
                    .AddTo(this);
            })
            .AddTo(this);
    }


    private void SwitchBGM(GameState state)
    {
        if (_isTitleScene) return; // タイトル中はステート再生しない

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

        Debug.Log($"BGMManager: 状態が {state} になりました。曲を選定します。");

        AudioClip nextClip = null;
        // フェーズに応じて曲を選ぶ
        switch (state)
        {
            case GameState.Setup:
                nextClip = setupBGM;
                break;
            case GameState.Battle:
                nextClip = battleBGM;
                break;
            case GameState.Result:
                nextClip = resultBGM;
                break;
        }

        // すでに流れている曲と同じなら何もしない
        if (_audioSource.clip == nextClip && _audioSource.isPlaying) return;

        if (_audioSource.isPlaying)
        {
            _audioSource.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                PlayNewClip(nextClip);
            });
        }
        else
        {
            PlayNewClip(nextClip);
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
            _audioSource.clip = clip;
            _audioSource.volume = 0f; // 音量0から開始
            _audioSource.Play();
            _audioSource.DOFade(maxVolume * VolumeSettings.BGMVolume, fadeDuration); // フェードイン
        }
        else
        {
            _audioSource.Stop();
            _audioSource.clip = null;
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