using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using DG.Tweening;
using TMPro;

public class Button_Press_A : MonoBehaviour
{
    [Header("�����������������肷��UI")]
    [SerializeField] public GameObject Panel;
    [SerializeField] private TextMeshProUGUI flashText;
    private Tween flashBlinkTween;

    [Header("�t�H�[�J�X��")]
    [SerializeField] private GameObject button;

    [Header("���̎�������{�^��")]
    [SerializeField] private GameObject button2;

    private void Start()
    {
        Panel.SetActive(false);
        flashText.text = "Press A to start";
            flashBlinkTween = flashText.DOFade(0.0f, 0.8f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
    }
    public void OnClick()
    {
        flashBlinkTween?.Kill();
        SEmanager.Instance.Play("deside");
        Panel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(button);
        button2.SetActive(false);
    }
}
