using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UniRx;

public class PointerContoroller : MonoBehaviour
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

    [Header("その設置物の設置時の変動世論値")]
    [SerializeField] public float POchangevalue;

    public bool canput;
    private Vector2 input;
    private bool left = false, right = false;
    private Vector3 rotation;

    // 【追加】カメラの情報をキャッシュする変数
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
        // 【追加】メインカメラを取得
        _mainCamera = Camera.main;

        ghost.SetActive(false);
        pointer.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentState
                .Subscribe(state =>
                {
                    if (state == GameState.Setup)
                    {
                        // 準備フェーズになったらUIを表示
                        ScrollUI.SetActive(true);
                        pointer.SetActive(false); // ポインターは初期状態オフ
                    }
                    else
                    {
                        // 襲撃中またはリザルト画面ならUIを隠す
                        ScrollUI.SetActive(false);

                        // 配置しようとしていたポインターも強制キャンセル
                        ghost.SetActive(false);
                        pointer.SetActive(false);
                        obj = null;
                    }
                })
                .AddTo(this);
        }
        else Debug.Log("ゲームマネージャーがないよ！");
    }

    // Update is called once per frame
    void Update()
    {
        // === 【変更箇所ここから】 ===

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

    public void OnCansel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ScrollUI.SetActive(true);
            ghost.SetActive(false);
            pointer.SetActive(false);
        }
    }

    public void OnPut(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.CurrentState.Value == GameState.Battle)
        {
            Debug.Log("襲撃中は配置できません！");
            return;
        }

        if (context.performed)
        {
            if (money.Instance.moneycount >= cost && canput)
            {
                Instantiate(obj, pointer.transform.position, pointer.transform.rotation);
                money.Instance.moneycount -= cost;
                PublicOpinionManager.Instance.POchanging(POchangevalue);
            }
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

    public void UIsetfalse()
    {
        // 襲撃中またはリザルト画面ならUIを隠す
        ScrollUI.SetActive(false);

        // 配置しようとしていたポインターも強制キャンセル
        ghost.SetActive(false);
        pointer.SetActive(false);
        obj = null;
    }

    public void UIsettrue()
    {
        // 準備フェーズになったらUIを表示
        ScrollUI.SetActive(true);
        pointer.SetActive(false); // ポインターは初期状態オフ
    }

}