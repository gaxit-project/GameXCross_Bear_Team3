using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("UIコンポーネント")]
    [SerializeField] private GameObject pausePanel;

    [Header("制御対象")]
    [SerializeField] private CameraController cameraController;

    private CameraControls controles;
    private bool isPaused = false;

    private void Awake()
    {
        controles = new CameraControls();
        pausePanel.SetActive(false);
    }
    void Start()
    {
        
    }

    private void OnEnable()
    {
        controles.Game.Pause.performed += OnPausePerformed;
        controles.Game.Enable();
    }

    private void OnDisable()
    {
        controles.Game.Pause.performed -= OnPausePerformed;
        controles.Game.Disable();
    }

    void Update()
    {
        
    }

    private void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if(isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        if(pausePanel != null) pausePanel.SetActive(true);

        Time.timeScale = 0f;

        if(cameraController != null) cameraController.SetRotationEnabled(false);
    }

    public void ResumeGame()
    {
        isPaused = false;

        if(pausePanel != null) pausePanel.SetActive(false);

        Time.timeScale = 1f;

        if(cameraController != null) cameraController.SetRotationEnabled(true);
    }
}
