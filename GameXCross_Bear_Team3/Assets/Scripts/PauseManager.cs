using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("UI�R���|�[�l���g")]
    [SerializeField] private GameObject pausePanel;

    [Header("����Ώ�")]
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
        // ポーズパネルがアクティブな時は、常にカメラの回転を無効化
        if (pausePanel != null && pausePanel.activeSelf)
        {
            if (cameraController != null && cameraController.enableRotation)
            {
                cameraController.SetRotationEnabled(false);
            }
        }
        // ポーズパネルが非アクティブで、ポーズ状態もfalseの時は、カメラの回転を有効化
        else if (!isPaused && pausePanel != null && !pausePanel.activeSelf)
        {
            if (cameraController != null && !cameraController.enableRotation)
            {
                cameraController.SetRotationEnabled(true);
            }
        }
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

        // ポーズ解除時はカメラの回転を有効化
        // Update()でポーズパネルの状態をチェックしているため、ポーズパネルが表示されている場合は再度無効化される
        if(cameraController != null) cameraController.SetRotationEnabled(true);
    }
}
