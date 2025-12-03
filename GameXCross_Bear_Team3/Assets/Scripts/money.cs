using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class money : MonoBehaviour
{
    public int moneycount;

    [SerializeField] TextMeshProUGUI Text;

    void Start()
    {
        Text.text = moneycount.ToString("N0");
    }


    void Update()
    {
        Text.text = moneycount.ToString("N0");
    }
}
