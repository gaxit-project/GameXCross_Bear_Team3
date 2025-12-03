using UnityEngine;

public class SerectObject : MonoBehaviour
{
    [SerializeField] public GameObject ScrollUI;
    [SerializeField] public GameObject pointer;
    [SerializeField,Header("設定する設置物")]
    public GameObject obj;
    [SerializeField, Header("ポインター")]
    public PointerContoroller p;



    public void Onclick()
    {
        ScrollUI.SetActive(false);
        pointer.SetActive(true);
        p.obj = obj;
    }
}
