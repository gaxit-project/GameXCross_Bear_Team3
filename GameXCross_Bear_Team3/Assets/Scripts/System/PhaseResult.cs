using UnityEngine;
using UnityEngine.EventSystems;

public class PhaseResult : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject phaseResult;
    [SerializeField] private GameObject button;
    [SerializeField] private GameObject selectUIbutton;

    public static PhaseResult Instance { get; private set; }

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

    public void Result()
    {
        
        phaseResult.SetActive(true);
        EventSystem.current.SetSelectedGameObject(button);
    }
    public void NextDay()
    {
        phaseResult.SetActive(false);
        GameManager.Instance.StartSetupPhase();
        EventSystem.current.SetSelectedGameObject(selectUIbutton);
    }
}
