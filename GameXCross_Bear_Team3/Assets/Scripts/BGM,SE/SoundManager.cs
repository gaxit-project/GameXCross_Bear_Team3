using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UniRx;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource seSource;
    private readonly Dictionary<string, AudioClip> _clipCache = new();

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
    }

    public IObservable<Unit> PlaySE(string key, float volume = 1.0f)
    {
        return GetOrLoadClip(key)
            .SelectMany(clip =>
            {
                if (clip == null) return Observable.ReturnUnit();

                seSource.PlayOneShot(clip, volume);
                // 再生が終わるまで待機するストリーム
                return Observable.Timer(TimeSpan.FromSeconds(clip.length)).AsUnitObservable();
            });
    }

    private IObservable<AudioClip> GetOrLoadClip(string key)
    {
        // キャッシュにある場合は即座に返す
        if (_clipCache.TryGetValue(key, out var clip))
            return Observable.Return(clip);

        var handle = Addressables.LoadAssetAsync<AudioClip>(key);

        return handle.ToObservable()
            .Select(_ => handle.Result)
            .Do(loadedClip =>
            {
                if (loadedClip != null) _clipCache[key] = loadedClip;
            });
    }
}