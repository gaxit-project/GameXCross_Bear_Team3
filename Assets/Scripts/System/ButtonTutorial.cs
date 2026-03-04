using UnityEngine;

public class ButtonTutorial : MonoBehaviour
{
    [SerializeField] private GameObject select;
    [SerializeField] private GameObject installation;
    [SerializeField] private GameObject noUI;

    private void Start()
    {
        select.SetActive(true);
        installation.SetActive(false);
        noUI.SetActive(false);
    }

    public void Set_select()
    {
        select.SetActive(true);
        installation.SetActive(false);
        noUI.SetActive(false);
    }

    public void Set_installation()
    {
        select.SetActive(false);
        installation.SetActive(true);
        noUI.SetActive(false);
    }

    public void Set_noUI()
    {
        select.SetActive(false);
        installation.SetActive(false);
        noUI.SetActive(true);
    }
}
