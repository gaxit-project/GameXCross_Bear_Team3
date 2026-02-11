using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class SEmanager : MonoBehaviour
{
    // シングルトン化（どこからでも呼べるようにする）
    public static SEmanager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;

    // インスペクターで管理するためのデータクラス
    [System.Serializable]
    public class SEData
    {
        public string key;        // 呼び出す時の名前（例: "Jump", "Attack"）
        public AudioClip clip;    // 音源ファイル
        [Range(0f, 1f)] public float volume = 1.0f; // 個別の音量調整用
    }

    // これがインスペクターに表示されるリスト
    [SerializeField]
    private List<SEData> seList = new List<SEData>();

    // 高速検索用の辞書（ゲーム開始時にリストから自動作成）
    private Dictionary<string, SEData> _seDictionary = new Dictionary<string, SEData>();

    private void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーン遷移しても消さない
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // AudioSourceがアタッチされていなければ自動で取得
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // リストを辞書に変換（検索を速くするため）
        foreach (var se in seList)
        {
            if (!_seDictionary.ContainsKey(se.key))
            {
                _seDictionary.Add(se.key, se);
            }
            else
            {
                Debug.LogWarning($"SEmanager: キー '{se.key}' が重複しています。");
            }
        }
    }

    /// <summary>
    /// SEを再生する
    /// </summary>
    /// <param name="key">登録したキー名</param>
    public void Play(string key)
    {
        if (_seDictionary.TryGetValue(key, out var data))
        {
            if (data.clip != null)
            {
                // PlayOneShotはSEを重ねて再生できる
                audioSource.PlayOneShot(data.clip, data.volume * VolumeSettings.SEVolume);
            }
            else
            {
                Debug.LogWarning($"SEmanager: キー '{key}' にAudioClipが設定されていません。");
            }
        }
        else
        {
            Debug.LogWarning($"SEmanager: キー '{key}' が見つかりません。");
        }
    }
}