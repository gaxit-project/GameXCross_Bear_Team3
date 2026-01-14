using UnityEngine;
using UnityEngine.EventSystems;

public class PauseManager : MonoBehaviour
{
    [Header("UIコンポーネント")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject firstSelectedOnPause; // 追加: 最初に選択するUI

    [Header("コンポーネント")]
    [SerializeField] private CameraController cameraController;

    private CameraControls controles;
    private bool isPaused = false;

    private void Awake()
    {
        controles = new CameraControls();
        pausePanel.SetActive(false);
        if (EventSystem.current == null)
        {
            Debug.LogWarning("EventSystem がシーンにありません。UI入力が動きません。");
        }
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

    public void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
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

        if (pausePanel != null) pausePanel.SetActive(true);

        // コントローラー選択先を設定
        if (EventSystem.current != null && firstSelectedOnPause != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedOnPause);
        }

        Time.timeScale = 0f;

        if(cameraController != null) cameraController.SetRotationEnabled(false);
    }

    public void ResumeGame()
    {
        isPaused = false;

        if(pausePanel != null) pausePanel.SetActive(false);

        Time.timeScale = 1f;

        if (cameraController != null) cameraController.SetRotationEnabled(true);

        // ポーズ解除時に選択をクリア
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
