using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SerectObject : MonoBehaviour
{
    [SerializeField] public GameObject ScrollUI;
    [SerializeField] public GameObject pointer;
    [SerializeField, Header("�������S�[�X�g")]
    public GameObject ghost;
    [SerializeField,Header("�ݒ肷��ݒu��")]
    public GameObject obj;
    [SerializeField, Header("���̃R�X�g")]
    public int cost;
    [SerializeField, Header("�K�v�Ȑ��_�l")]
    public float necessaryPOvalue;
    [SerializeField, Header("�u�����Ƃ��̐��_�l�̕ω���")]
    public float POchangevalue;
    [SerializeField, Header("�|�C���^�[")]
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
            Debug.Log("Onclickが実行されました");
            changeUI();
            SEmanager.Instance.Play("deside");
        }
        else
        {
            Debug.Log("���������_�l������܂���I");
            SEmanager.Instance.Play("unable");
        }
    }

    private void changeUI()
    {
        ScrollUI.SetActive(false);
        pointer.SetActive(true);
        ghost.SetActive(true);
        p.obj = obj;
        p.cost = currentcost;//���̏�Ԃł͐ݒu���ɉ��i���ϓ������ۂɌ��莞�̉��i�Őݒu�ł��Ă��܂��D
        p.ghost = ghost;
        p.POchangevalue = POchangevalue;
        p.necessaryPOvalue = necessaryPOvalue;
    }
}
