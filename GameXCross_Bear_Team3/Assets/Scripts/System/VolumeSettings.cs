using UnityEngine;

/// <summary>
/// BGM/SE音量を管理する静的クラス
/// PlayerPrefsで設定を永続化
/// </summary>
public static class VolumeSettings
{
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SE_VOLUME_KEY = "SEVolume";

    private static float _bgmVolume = -1f;
    private static float _seVolume = -1f;

    /// <summary>
    /// BGM音量 (0.0 ~ 1.0)
    /// </summary>
    public static float BGMVolume
    {
        get
        {
            if (_bgmVolume < 0f)
                _bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1.0f);
            return _bgmVolume;
        }
        set
        {
            _bgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, _bgmVolume);
        }
    }

    /// <summary>
    /// SE音量 (0.0 ~ 1.0)
    /// </summary>
    public static float SEVolume
    {
        get
        {
            if (_seVolume < 0f)
                _seVolume = PlayerPrefs.GetFloat(SE_VOLUME_KEY, 1.0f);
            return _seVolume;
        }
        set
        {
            _seVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SE_VOLUME_KEY, _seVolume);
        }
    }

    /// <summary>
    /// 設定を保存する
    /// </summary>
    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
