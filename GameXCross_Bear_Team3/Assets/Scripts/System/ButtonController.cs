using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UniRx;

public class ButtonController : MonoBehaviour
{
    [SerializeField] private string clickSeKey = "SE_Click";

    void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            TransitionWithSE("Title");
        }
    }

    public void SwithToTitle() => TransitionWithSE("Title");
    public void SwithToMain() => TransitionWithSE("Main");
    public void SwithToSetting() => TransitionWithSE("Setting");

    private void TransitionWithSE(string sceneName)
    {
        if (SoundManager.Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        // SE再生を開始し、完了したらシーンをロードする
        SoundManager.Instance.PlaySE(clickSeKey)
            .First() // 1回のみ実行を保証
            .Subscribe(_ =>
            {
                SceneManager.LoadScene(sceneName);
            })
            .AddTo(this);
    }

    public void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}