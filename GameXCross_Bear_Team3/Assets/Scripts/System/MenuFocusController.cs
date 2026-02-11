using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UniRx;

public class MenuFocusController : MonoBehaviour
{
    [Header("メニューが開いた時に最初に選択状態にするボタン")]
    public GameObject firstSelectButton;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false; 
        
        // Startのタイミングでも念のため0.05秒待ってから選択する（バグ防止）
        Observable.Timer(TimeSpan.FromSeconds(0.05f))
            .Subscribe(_ => SetFocus())
            .AddTo(this);
    }

    public void SetFocus()
    {
        EventSystem.current.SetSelectedGameObject(null);

        EventSystem.current.SetSelectedGameObject(firstSelectButton);

        var selectable = firstSelectButton.GetComponent<Selectable>();
        if (selectable != null) selectable.Select();
    }
}
