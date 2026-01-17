using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillCountManager : MonoBehaviour
{
    public static KillCountManager Instance { get; private set; }

    [SerializeField] int killcount;
    [SerializeField] int victimcount;

    [SerializeField] TextMeshProUGUI killtext;
    [SerializeField] TextMeshProUGUI victimtext;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void killCounterplus()
    {
        killcount++;
    }
    public void victimconterplus()
    {
        victimcount++;
    }

    public void Counttype()
    {
        killtext.text = ""+ killcount;
        victimtext.text = ""+ victimcount;
        killcount= 0;
        victimcount = 0;
    }
}
