using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UniRx;

public class PauseManager : MonoBehaviour
{
    [Header("UIコンポーネント")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject selectUI;
    [SerializeField] private GameObject firstSelectedOnPause; // 追加: 最初に選択するUI
    [SerializeField] private GameObject lastSelectedOnPause; // 追加: ポーズ前に選択していたUI

    [Header("コンポーネント")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private PointerController pointerController;
    [SerializeField] private CanvasGroup canvasGroup;

    private CameraControls controles;
    private bool isPaused = false;

    private void Awake()
    {
        //canvasGroup = pausePanel.GetComponent<CanvasGroup>();

        controles = new CameraControls();
        pausePanel.SetActive(false);
        if (EventSystem.current == null)
        {
            Debug.LogWarning("EventSystem がシーンにありません。UI入力が動きません。");
        }
    }

    private void Start()
    {
        if (pointerController == null)
        {
            pointerController = FindFirstObjectByType<PointerController>();
        }
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
        // GameManagerが存在しない場合は何もしない（タイトルシーンなど）
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.CurrentState.Value != GameState.Result)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void TitleBack()
    {
        SEmanager.Instance.Play("deside");
        Time.timeScale = 1f;

        // DontDestroyOnLoadオブジェクトをリセット
        KillCountManager.Instance?.AlldataReset();

        // ポーズ状態をリセット
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }

    public void PauseGame()
    {
        SEmanager.Instance.Play("UIpopup");

        Time.timeScale = 0f;
        canvasGroup.interactable = false;
        isPaused = true;

        if (pausePanel != null) pausePanel.SetActive(true);

        if (pointerController != null)
        {
            pointerController.SetPauseVisuals(true);
        }

        // コントローラー選択先を設定
        if (EventSystem.current != null && firstSelectedOnPause != null)
        {
            lastSelectedOnPause = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(firstSelectedOnPause);
        }



        if (cameraController != null) cameraController.SetRotationEnabled(false);
    }

    public void ResumeGame()
    {
        SEmanager.Instance.Play("UIpopup");
        canvasGroup.interactable = true;
        isPaused = false;

        if (pausePanel != null) pausePanel.SetActive(false);

        if (pointerController != null)
        {
            pointerController.SetPauseVisuals(false);
        }

        Time.timeScale = 1f;

        if (cameraController != null) cameraController.SetRotationEnabled(true);

        // ポーズ解除時に選択をクリア
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedOnPause);
        }
    }
}
