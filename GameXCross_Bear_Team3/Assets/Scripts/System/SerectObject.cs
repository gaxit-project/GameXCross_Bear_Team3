using TMPro;
using UnityEngine;

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
    public PointerContoroller p;

    [SerializeField] TextMeshProUGUI Text;

    private void Start()
    {
        Text.text = cost.ToString("N0");
    }


    public void Onclick()
    {
        ScrollUI.SetActive(false);
        pointer.SetActive(true);
        ghost.SetActive(true);
        p.obj = obj;
        p.cost = cost;
        p.ghost = ghost;
        p.POchangevalue = POchangevalue;
    }
}
