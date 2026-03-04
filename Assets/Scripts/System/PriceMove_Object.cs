using TMPro;
using UnityEngine;

public class PriceMove_Object : MonoBehaviour
{
    [Header("‰¿Ši")]
    [SerializeField] private int basePrice;

    [Header("UI")]
    [SerializeField] private TextMeshPro MoneyText;

    [Header("PriceMoveManager")]
    [SerializeField] private PriceMoveManager PriceMoveManager;

    private int currentprice;
    void Update()
    {
        currentprice = PriceMoveManager.PriceMove(basePrice);
        MoneyText.text = currentprice.ToString("NO");
    }
}
