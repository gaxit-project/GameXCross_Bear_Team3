using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Button_Press_A : MonoBehaviour
{
    [Header("表示させたり消したりするUI")]
    [SerializeField] public GameObject Panel;

    [Header("フォーカス先")]
    [SerializeField] private GameObject button;

    [Header("その時消えるボタン")]
    [SerializeField] private GameObject button2;

    private void Start()
    {
        Panel.SetActive(false);
    }

    public void OnClick()
    {
        SEmanager.Instance.Play("deside");
        Panel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(button);
        button2.SetActive(false);
    }
}
