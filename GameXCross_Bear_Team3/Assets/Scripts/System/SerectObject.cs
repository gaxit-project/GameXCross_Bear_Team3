using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SerectObject : MonoBehaviour
{
    [SerializeField] public GameObject ScrollUI;
    [SerializeField] public GameObject pointer;
    [SerializeField, Header("表示するゴースト")]
    public GameObject ghost;
    [SerializeField,Header("設定する設置物")]
    public GameObject obj;
    [SerializeField, Header("そのコスト")]
    public int cost;
    [SerializeField, Header("必要な世論値")]
    public float necessaryPOvalue;
    [SerializeField, Header("置いたときの世論値の変化量")]
    public float POchangevalue;
    [SerializeField, Header("ポインター")]
    public PointerController p;

    [SerializeField, Header("PriceMoveManager")]
    public PriceMoveManager PriceMoveManager;

    public int currentcost;

    public Image Image;
    private Color canpush;
    private Color cantpush;

    [SerializeField] TextMeshProUGUI Text;

    private void Start()
    {
        Text.text = cost.ToString("N0");
        Image = GetComponent<Image>();
        canpush = Image.color;
        cantpush = Color.gray;
    }

    private void Update()
    {
        if(PublicOpinionManager.Instance.JudgePO(necessaryPOvalue)&&money.Instance.moneycount >= currentcost)
            Image.color = canpush;
        else
            Image.color = cantpush;
        currentcost = PriceMoveManager.PriceMove(cost);
        Text.text = currentcost.ToString("N0");
    }

    public void Onclick()
    {
        if(PublicOpinionManager.Instance.JudgePO(necessaryPOvalue) && money.Instance.moneycount >= currentcost)
        {
            changeUI();
            SEmanager.Instance.Play("deside");
        }
        else
        {
            Debug.Log("お金か世論値が足りません！");
            SEmanager.Instance.Play("unable");
        }
    }

    private void changeUI()
    {
        ScrollUI.SetActive(false);
        pointer.SetActive(true);
        ghost.SetActive(true);
        p.obj = obj;
        p.cost = currentcost;//今の状態では設置中に価格が変動した際に決定時の価格で設置できてしまう．
        p.ghost = ghost;
        p.POchangevalue = POchangevalue;
        p.necessaryPOvalue = necessaryPOvalue;
    }
}
