using UnityEngine;

public class HunterShooting : MonoBehaviour
{
    // アニメーターを操作するための変数
    private Animator anim;

    // 武器のモデル（見た目）を出し入れするための枠
    public GameObject revolverModel;
    public GameObject rifleModel;

    void Start()
    {
        // 自分の体についているAnimatorを探してくる
        anim = GetComponent<Animator>();

        // 念のため、開始時はリボルバーを表示・ライフルを非表示にセット
        revolverModel.SetActive(true);
        rifleModel.SetActive(false);
    }

    void Update()
    {
        // ■ '1'キーを押したらリボルバーモードへ
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // アニメーションをリボルバー構えに戻す
            anim.SetBool("IsRifle", false);
            // モデルの表示を切り替える
            revolverModel.SetActive(true);
            rifleModel.SetActive(false);
        }

        // ■ '2'キーを押したらライフルモードへ
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // アニメーションをライフル構えにする
            anim.SetBool("IsRifle", true);
            // モデルの表示を切り替える
            revolverModel.SetActive(false);
            rifleModel.SetActive(true);
        }

        // ■ 左クリックで発射
        if (Input.GetMouseButtonDown(0))
        {
            // Animatorに「撃て(Fire)」と命令する
            // ※今はリボルバーかライフルか、Animatorが勝手に判断してくれます
            anim.SetTrigger("Fire");
        }
    }
}