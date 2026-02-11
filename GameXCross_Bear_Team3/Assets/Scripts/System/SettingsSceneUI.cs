using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UniRx;

/// <summary>
/// 設定シーン専用のUI制御スクリプト（コントローラー対応）
/// </summary>
public class SettingsSceneUI : MonoBehaviour
{
    [Header("スライダー参照")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    [Header("最初に選択するUI要素")]
    [SerializeField] private GameObject firstSelected;

    [Header("戻り先シーン")]
    [SerializeField] private string returnSceneName = "Title";

    [Header("SE設定")]
    [SerializeField] private string clickSeKey = "deside";

    private void Start()
    {
        // 現在の音量をスライダーに反映
        if (bgmSlider != null)
        {
            bgmSlider.value = VolumeSettings.BGMVolume;
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (seSlider != null)
        {
            seSlider.value = VolumeSettings.SEVolume;
            seSlider.onValueChanged.AddListener(OnSEVolumeChanged);
        }

        // コントローラー用：最初のUI要素を選択
        SelectFirstElement();
    }

    private void Update()
    {
        // コントローラーのBボタンで戻る
        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.bButton.wasPressedThisFrame)
        {
            OnBackButtonClicked();
        }

        // EventSystemの選択が外れた場合、再選択する
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            SelectFirstElement();
        }
    }

    private void SelectFirstElement()
    {
        if (EventSystem.current != null)
        {
            // firstSelectedが設定されていればそれを、なければbgmSliderを選択
            GameObject toSelect = firstSelected != null ? firstSelected : 
                                  (bgmSlider != null ? bgmSlider.gameObject : null);
            
            if (toSelect != null)
            {
                EventSystem.current.SetSelectedGameObject(toSelect);
            }
        }
    }

    private void OnBGMVolumeChanged(float value)
    {
        VolumeSettings.BGMVolume = value;
    }

    private void OnSEVolumeChanged(float value)
    {
        VolumeSettings.SEVolume = value;
        
        // スライダー変更時にテスト音を再生（音量確認用）
        if (SEmanager.Instance != null)
        {
            Debug.Log($"SEmanager.Instance.Play({clickSeKey}) を実行");
            SEmanager.Instance.Play(clickSeKey);
        }
        else if (SoundManager.Instance != null)
        {
            Debug.Log($"SoundManager.Instance.Play({clickSeKey}) を実行");
            SoundManager.Instance.Play(clickSeKey);
        }
        else
        {
            Debug.LogWarning("SEmanagerとSoundManagerの両方のInstanceがnullです。テスト音を再生できません。");
        }
    }

    /// <summary>
    /// 戻るボタン用（Inspectorから呼び出し）
    /// </summary>
    public void OnBackButtonClicked()
    {
        VolumeSettings.Save();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(clickSeKey)
                .Subscribe(_ => SceneManager.LoadScene(returnSceneName))
                .AddTo(this);
        }
        else
        {
            SceneManager.LoadScene(returnSceneName);
        }
    }

    private void OnDestroy()
    {
        // スライダーのリスナーを解除
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        if (seSlider != null)
            seSlider.onValueChanged.RemoveListener(OnSEVolumeChanged);
    }
}
