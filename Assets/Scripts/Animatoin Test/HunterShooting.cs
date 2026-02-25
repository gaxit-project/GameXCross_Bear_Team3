using UnityEngine;

public class HunterShooting : MonoBehaviour
{
    public Animator anim;
    // UnityのInspectorでセットする「武器モデル」の枠
    public GameObject revolverModel;
    public GameObject rifleModel;
    
    // 歩くスピード
    public float moveSpeed = 3.0f;

    // 現在選んでいる武器（0:リボルバー, 1:ライフル）
    private int currentWeaponType = 0; 

    void Start()
    {
        // ゲーム開始時、まずはリボルバーを選択状態にする
        currentWeaponType = 0;
        // 止まっているので武器を表示する
        UpdateWeaponVisibility(false); 
    }

    void Update()
    {
        // ■ 1. キーボードの「1」「2」で武器モード切替
        if (GameManager.Instance != null && GameManager.Instance.DebugMode)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                currentWeaponType = 0; // リボルバーに切り替え
                anim.SetBool("IsRifle", false);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                currentWeaponType = 1; // ライフルに切り替え
                anim.SetBool("IsRifle", true);
            }
        }

        // ■ 2. クリックで発砲（デバッグ用）
        if (GameManager.Instance != null && GameManager.Instance.DebugMode)
        {
            if (Input.GetMouseButtonDown(0))
            {
                anim.SetTrigger("Fire");
            }
        }

        // ■ 3. 移動入力の判定（デバッグ用：既存のコントローラー操作がないため、キーボード移動を制限）
        float h = 0;
        float v = 0;

        if (GameManager.Instance != null && GameManager.Instance.DebugMode)
        {
            h = Input.GetAxis("Horizontal"); // A, D キー
            v = Input.GetAxis("Vertical");   // W, S キー
        }

        // キー入力が少しでもあれば「歩いている(true)」と判定
        bool isMoving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        // アニメーターに「歩いている」と伝える（歩きモーション再生）
        anim.SetBool("IsWalking", isMoving);

        // ★ここが最重要！
        // 歩いている状態(isMoving)に合わせて、武器を出したり消したりする
        UpdateWeaponVisibility(isMoving);

        // ■ 4. 実際の移動処理
        if (isMoving)
        {
            Vector3 moveDir = new Vector3(h, 0, v).normalized;
            transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDir);
            }
        }
    }

    // 武器の表示・非表示をコントロールする専用の機能
    void UpdateWeaponVisibility(bool isWalking)
    {
        // 【パターンA】歩いている時
        if (isWalking)
        {
            // リボルバーもライフルも、両方とも強制的に消す！(false)
            revolverModel.SetActive(false);
            rifleModel.SetActive(false);
        }
        // 【パターンB】止まっている時
        else
        {
            // 選んでいる武器の方だけを表示する(true)
            if (currentWeaponType == 0) 
            {
                revolverModel.SetActive(true);  // リボルバーON
                rifleModel.SetActive(false);    // ライフルOFF
            }
            else 
            {
                revolverModel.SetActive(false); // リボルバーOFF
                rifleModel.SetActive(true);     // ライフルON
            }
        }
    }
}