using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;

public class PointerController : MonoBehaviour
{
    [Header("移動速度")]
    [SerializeField] private float Speed = 1;
    [SerializeField] public GameObject ScrollUI;
    [SerializeField] public GameObject pointer;

    [Header("現在選択している設置物")]
    [SerializeField] public GameObject obj;

    [Header("現在表示しているゴースト")]
    [SerializeField] public GameObject ghost;

    [Header("その設置物の設置コスト")]
    [SerializeField] public int cost;

    [SerializeField, Header("必要な世論値")]
    public float necessaryPOvalue;

    [Header("その設置物の設置時の変動世論値")]
    [SerializeField] public float POchangevalue;

    public bool canput;
    private Vector2 input;

    // カメラの情報をキャッシュする変数
    private Camera _mainCamera;

    [Header("設定")]
    [Tooltip("回転速度（度/秒）")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("回転軸（例: (0, 1, 0) でY軸回転）")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Header("入力設定")]
    [Tooltip("回転に使用するボタン設定")]
    public InputActionProperty rotationrightInput;
    public InputActionProperty rotationleftInput;

    void Start()
    {
        _mainCamera = Camera.main;

        UIsetFalse();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentState
                .Subscribe(state =>
                {
                    if (state == GameState.Setup)
                    {
                        // 準備フェーズになったらUIを表示（戦闘フェーズ中も表示）
                        UIsetTrue();
                    }
                    else if (state == GameState.Result)
                    {
                        UIsetFalse(); // リザルト画面ならUIを隠す
                    }
                })
                .AddTo(this);
        }
        else Debug.Log("ゲームマネージャーがないよ！");
    }

    void Update()
    {
        // 1. スティック入力を3Dベクトルに変換
        Vector3 inputDir = new Vector3(input.x, 0, input.y);

        // 2. 入力がある場合だけ計算（無駄な処理を省くため）
        if (inputDir.sqrMagnitude > 0.001f)
        {
            // 3. カメラのY軸（水平回転）の角度だけを取り出す
            float cameraY = _mainCamera.transform.eulerAngles.y;

            // 4. その角度の回転情報（クォータニオン）を作成
            Quaternion cameraRotation = Quaternion.Euler(0, cameraY, 0);

            // 5. 入力ベクトルをカメラの向きに合わせて回転させる
            Vector3 moveDirection = cameraRotation * inputDir;

            // 6. 移動（Space.Worldであることに注意）
            transform.Translate(moveDirection * Speed * Time.deltaTime, Space.World);
        }

        // ボタンが押されているか判定 (IsPressed)
        if (rotationrightInput.action.IsPressed())
        {
            // 等速回転の処理: 軸 * 速度 * フレーム間の時間
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
        }
        if (rotationleftInput.action.IsPressed())
        {
            // 等速回転の処理: 軸 * 速度 * フレーム間の時間
            transform.Rotate(rotationAxis * -rotationSpeed * Time.deltaTime);
        }
    }

    public void OnPerformed(InputAction.CallbackContext context)
    {
        input = context.ReadValue<Vector2>();
    }

    public void OnToggleMenu(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("OnToggleMenu呼び出し");

        // ポインターが出てるなら、設置をキャンセルしてメニューを開く
        if (pointer.activeSelf)
        {
            ghost.SetActive(false);
            pointer.SetActive(false);
            ScrollUI.SetActive(true);
        }
        // メニューを開いているなら、メニューを閉じる
        else if(ScrollUI.activeSelf)
        {
            SEmanager.Instance.Play("cancel");
            ScrollUI.SetActive(false);
        }
        // メニューを閉じているのでメニューを開く
        else
        {
            ScrollUI.SetActive(true);
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("OnCancel呼び出し");

        if (ScrollUI != null)
        {
            Debug.Log($"登録されているScrollUIの名前: {ScrollUI.name}");
            Debug.Log($"そのUIはアクティブか？: {ScrollUI.activeSelf}");
        }
        else
        {
            Debug.LogError("ScrollUIの中身が 空っぽ（Null） です！ Inspectorで設定してください！");
            return;
        }

        // ポインターが出ている場合は、配置をキャンセルしてメニューを表示
        if (pointer.activeSelf)
        {
            Debug.Log("ルート1: ポインターキャンセルを実行（メニューは開いたままになります）");
            pointerCancel(); // メニューを表示
            return;
        }

        // 既にメニューを開いている場合はメニューを隠す
        if (ScrollUI.activeSelf)
        {
            Debug.Log("ルート2: メニューを閉じます");
            ScrollUI.SetActive(false);
            SEmanager.Instance.Play("cancel");
            return;
        }

        Debug.Log("ルート3: 何も実行しませんでした（どちらも非表示、または条件不一致）");
    }


    // 配置をキャンセルしてメニューを表示
    private void pointerCancel()
    {
        Debug.Log("pointerCancel呼び出し");
        SEmanager.Instance.Play("cancel");
        ghost.SetActive(false);
        pointer.SetActive(false);

        ScrollUI.SetActive(true);
    }

    public void OnPut(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("OnPut呼び出し");

        if (context.performed)
        {
            if (money.Instance.moneycount >= cost && canput)
            {
                Instantiate(obj, pointer.transform.position, pointer.transform.rotation);
                money.Instance.moneycount -= cost;
                PublicOpinionManager.Instance.POchanging(POchangevalue);

                if(money.Instance.moneycount < cost || !PublicOpinionManager.Instance.JudgePO(necessaryPOvalue))
                    pointerCancel();//設置後世論値か金が足りなくなると強制的に選択前に戻される
            }
        }
    }

    public void OnSkip(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("OnSkip呼び出し");

        if (GameManager.Instance != null && GameManager.Instance.CurrentState.Value == GameState.Setup)
        {
            GameManager.Instance.SkipSetupPhase();
        }
    }

    // オブジェクトが有効になったときに入力を有効化
    private void OnEnable()
    {
        if (rotationrightInput.action != null) rotationrightInput.action.Enable();
        if (rotationleftInput.action != null) rotationleftInput.action.Enable();
    }

    // オブジェクトが無効になったときに入力を無効化
    private void OnDisable()
    {
        if (rotationrightInput.action != null) rotationrightInput.action.Disable();
        if (rotationleftInput.action != null) rotationleftInput.action.Disable();
    }

    // 強制キャンセル
    public void UIsetFalse()
    {
        // メニューを隠す
        ScrollUI.SetActive(false);

        // 配置しようとしていたポインターもキャンセル
        ghost.SetActive(false);
        pointer.SetActive(false);
        obj = null;
    }

    public void UIsetTrue()
    {
        // メニューを表示
        ScrollUI.SetActive(true);

        // ポインターやゴーストは初期状態オフ
        ghost.SetActive(false);
        pointer.SetActive(false); 
    }

}