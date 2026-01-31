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

    private bool _isFading = false; // フェード中かどうか

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
        if (scene.name == titleSceneName)
        {
            Debug.Log($"BGMManager: タイトルシーン検知 -> {scene.name}");
            Time.timeScale = 1.0f;
            PlayNewClip(titleBGM);
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