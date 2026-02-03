using UnityEngine;
using TMPro;

public class FieldLvUP_Button : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private FieldManager fieldManager;

    [Header("コスト表示")]
    [SerializeField] private TextMeshProUGUI cost_T;

    [Header("レベル表示")]
    [SerializeField] private TextMeshProUGUI level_T;

    private void Update()
    {
        cost_T.text = fieldManager.currentCost().ToString("N0");
        level_T.text = fieldManager.currentLv().ToString();
    }

    public void Onclick()
    {
        if(money.Instance.moneycount >= fieldManager.currentCost())
            fieldManager.LvUp();
    }

}
