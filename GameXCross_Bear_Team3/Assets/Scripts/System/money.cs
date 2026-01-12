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
        // Ç±ÇÍÇ™Ç»Ç¢Ç∆ëºÇ©ÇÁåƒÇ◊Ç‹ÇπÇÒÅI
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
        Text.text = moneycount.ToString("N0");
    }


    void Update()
    {
        Text.text = moneycount.ToString("N0");
    }
}
