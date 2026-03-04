using UnityEngine;

public class PriceMoveManager : MonoBehaviour
{
    [Header("変化量（係数）")]
    [SerializeField] private float value;

    private int POpercent;
    private int AmountChange;
    void Update()
    {
        POpercent = PublicOpinionManager.Instance.GetPOpercent();
        AmountChange = 50 - POpercent;
    }

    /// <summary>
    /// 価格の変動を計算してくれるメソッドです．引数には価格の初期値を入れて下さい．
    /// </summary>
    /// <param name="basePrice"></param>
    /// <returns></returns>
    public int PriceMove(int basePrice)
    {
        float fluctuation = (float)basePrice * AmountChange * value * 0.01f;

        int intFluctuation = Mathf.RoundToInt(fluctuation);
        return basePrice + intFluctuation;
    }
}
