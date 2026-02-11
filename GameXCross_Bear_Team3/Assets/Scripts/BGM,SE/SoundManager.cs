using UnityEngine;
using System;
using System.Collections.Generic;
using UniRx;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource seSource;
    
    // インスペクターで管理するためのデータクラス
    [System.Serializable]
    public class SEData
    {
        public string key;        // 呼び出す時の名前（例: "SE_Click"）
        public AudioClip clip;    // 音源ファイル
        [Range(0f, 1f)] public float volume = 1.0f; // 個別の音量調整用
    }

    // インスペクターで設定するリスト
    [SerializeField]
    private List<SEData> seList = new List<SEData>();

    // 高速検索用の辞書
    private Dictionary<string, SEData> _seDictionary = new Dictionary<string, SEData>();

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

        // リストを辞書に変換（検索を速くするため）
        foreach (var se in seList)
        {
            if (!string.IsNullOrEmpty(se.key) && !_seDictionary.ContainsKey(se.key))
            {
                _seDictionary.Add(se.key, se);
            }
        }
    }

    public IObservable<Unit> PlaySE(string key, float volume = 1.0f)
    {
        if (_seDictionary.TryGetValue(key, out var data))
        {
            if (data.clip != null)
            {
                seSource.PlayOneShot(data.clip, data.volume * volume * VolumeSettings.SEVolume);
                // 再生が終わるまで待機するストリーム
                return Observable.Timer(TimeSpan.FromSeconds(data.clip.length)).AsUnitObservable();
            }
            else
            {
                Debug.LogWarning($"SoundManager: キー '{key}' にAudioClipが設定されていません。");
            }
        }
        else
        {
            Debug.LogWarning($"SoundManager: キー '{key}' が見つかりません。");
        }
        
        return Observable.ReturnUnit();
    }

    /// <summary>
    /// 同期的にSEを再生する（Observableを使わない場合用）
    /// </summary>
    public void Play(string key, float volume = 1.0f)
    {
        if (_seDictionary.TryGetValue(key, out var data))
        {
            if (data.clip != null)
            {
                seSource.PlayOneShot(data.clip, data.volume * volume * VolumeSettings.SEVolume);
            }
            else
            {
                Debug.LogWarning($"SoundManager: キー '{key}' にAudioClipが設定されていません。");
            }
        }
        else
        {
            Debug.LogWarning($"SoundManager: キー '{key}' が見つかりません。");
        }
    }
}