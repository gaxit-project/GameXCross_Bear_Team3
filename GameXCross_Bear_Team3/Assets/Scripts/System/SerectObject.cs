using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SerectObject : MonoBehaviour
{
    [SerializeField] public GameObject ScrollUI;
    [SerializeField] public GameObject pointer;
    [SerializeField, Header("設置用ゴースト")]
    public GameObject ghost;
    [SerializeField,Header("設置するオブジェクト")]
    public GameObject obj;
    [SerializeField, Header("基本コスト")]
    public int cost;
    [SerializeField, Header("必要なPO値")]
    public float necessaryPOvalue;
    [SerializeField, Header("設置時のPO値の変化量")]
    public float POchangevalue;
    [SerializeField, Header("ポインターコントローラー")]
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
        if(money.Instance.moneycount >= currentcost)
            Image.color = canpush;
        else
            Image.color = cantpush;
        currentcost = PriceMoveManager.PriceMove(cost);
        Text.text = currentcost.ToString("N0");
    }

    public void Onclick()
    {
        if(money.Instance.moneycount >= currentcost)
        {
            Debug.Log("Onclickが実行されました");
            StartCoroutine(ChangeUIAfterWait());
            SEmanager.Instance.Play("deside");
        }
        else
        {
            Debug.Log("���������_�l������܂���I");
            SEmanager.Instance.Play("unable");
        }
    }

    private IEnumerator ChangeUIAfterWait()
    {
        // ここで「今のフレームが終わる」まで待機する
        // これにより、PointerControllerが同じボタン入力を検知しても、
        // まだ ghost が false なので誤動作しなくなる
        yield return null;

        // 待ち終わったら変更処理を実行
        changeUI();
    }

    private void changeUI()
    {
        // ScrollUI.SetActive(false);
        // pointer.SetActive(true);
        // ghost.SetActive(true);
        // p.obj = obj;
        // p.cost = currentcost;//���̏�Ԃł͐ݒu���ɉ��i���ϓ������ۂɌ��莞�̉��i�Őݒu�ł��Ă��܂��D
        // p.ghost = ghost;
        // p.POchangevalue = POchangevalue;
        // p.necessaryPOvalue = necessaryPOvalue;

        p.StartPlacement(obj, ghost, currentcost, necessaryPOvalue, POchangevalue);
    }
}
