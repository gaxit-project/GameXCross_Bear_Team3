using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class money : MonoBehaviour
{
    public int moneycount;

    [SerializeField] TextMeshProUGUI Text;

    public static money Instance { get; private set; }

    private void Awake()
    {
        // ���ꂪ�Ȃ��Ƒ�����Ăׂ܂���I
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        moneycount = GameManager.Instance.Balance.initialMoney;
        Text.text = moneycount.ToString("N0");
    }


    void Update()
    {
        Text.text = moneycount.ToString("N0");
    }
}
