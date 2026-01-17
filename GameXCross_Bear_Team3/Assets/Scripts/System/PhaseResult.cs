using UnityEngine;

public class PhaseResult : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject phaseResult;

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
    }
    public void NextDay()
    {
        phaseResult.SetActive(false);
    }
}
