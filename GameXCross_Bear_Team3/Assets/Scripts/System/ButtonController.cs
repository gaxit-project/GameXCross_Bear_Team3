using System;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ButtonController : MonoBehaviour
{
    [SerializeField] private string clickSeKey = "deside";

    private bool _ispressing = false;

    void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            TransitionWithSE("Title");
        }
    }

    public void SwithToTitle()
    {
        // DontDestroyOnLoadオブジェクトをリセット
        KillCountManager.Instance?.AlldataReset();
        TransitionWithSE("Title");
    }
    public void SwithToMain() => TransitionWithSE("Main");
    public void SwithToSetting() => TransitionWithSE("Setting");

    private void TransitionWithSE(string sceneName)
    {
        // すでに終了処理中なら何もしない（連打防止）
        if (_ispressing) return;
        _ispressing = true;
        if (SoundManager.Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        // SE再生を開始し、完了したらシーンをロードする
        SEmanager.Instance.Play(clickSeKey);
            Observable.Timer(TimeSpan.FromSeconds(0.5f), Scheduler.MainThreadIgnoreTimeScale)
            .Subscribe(_ =>
            {
                SceneManager.LoadScene(sceneName);
            })
            .AddTo(this);
    }

    public void DoQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void QuitApplication()
    {
        // すでに終了処理中なら何もしない（連打防止）
        if (_ispressing) return;
        _ispressing = true;

        Debug.Log("終了プロセス開始：音を再生します");


        SEmanager.Instance.Play("deside");


        Observable.Timer(TimeSpan.FromSeconds(0.5f))
            .Subscribe(_ =>
            {
                // 3. 実際に終了する
                DoQuit();
            })
            .AddTo(this);
    }
}