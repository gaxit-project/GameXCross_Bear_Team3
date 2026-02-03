using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FieldLvUP_Button : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private FieldManager fieldManager;

    [Header("コスト表示")]
    [SerializeField] private TextMeshProUGUI cost_T;

    [Header("レベル表示")]
    [SerializeField] private TextMeshProUGUI level_T;

    public Image Image;
    private Color canpush;
    private Color cantpush;

    private void Start()
    {
        Image = GetComponent<Image>();
        canpush = Image.color;
        cantpush = Color.gray;
    }
    private void Update()
    {
        if (money.Instance.moneycount >= fieldManager.currentCost() && fieldManager.currentLv() < 9)
            Image.color = canpush;
        else
            Image.color = cantpush;

        cost_T.text = fieldManager.currentCost().ToString("N0");

        if (fieldManager.currentLv() != 9)
        {
            int displayLv = fieldManager.currentLv() + 1;
            level_T.text = displayLv.ToString();
        }
        else
        {
            level_T.text = "MAX";
            level_T.color = Color.red;
        }
    }

    public void Onclick()
    {
        if (money.Instance.moneycount >= fieldManager.currentCost() && fieldManager.currentLv() < 9)
        {
            fieldManager.LvUp();
            money.Instance.moneycount -= fieldManager.currentCost();
        }
    }

}
