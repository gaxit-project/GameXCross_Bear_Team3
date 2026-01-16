using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICursor_title : MonoBehaviour
{
    [SerializeField] float scrollSpeed = 10f; // スクロールの速さ
    [SerializeField] public GameObject cursor;

    [Header("SE設定")]
    [SerializeField] private string moveSE = "Select"; // 鳴らしたいSEのキー名
    [SerializeField]private GameObject lastSelected; // 「さっきまで選択されていたもの」を覚える変数


    void Start()
    {
        // 最初は現在の選択状態を初期値として入れておく（開幕で音が鳴るのを防ぐため）
        lastSelected = EventSystem.current.currentSelectedGameObject;
    }


    void Update()
    {
        // 【重要】EventSystem自体がまだ準備できていない場合は処理を飛ばす
        if (EventSystem.current == null)
        {
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        // 何も選択されていない場合は何もしない
        if (selected == null) return;

        // --- 以下、選択がある時の処理 ---
        if (selected != lastSelected)
        {
            // 念のため SEmanager の Instance もチェック
            if (SEmanager.Instance != null)
            {
                SEmanager.Instance.Play("cursor");
            }
            lastSelected = selected;
        }

        // 座標移動 (newPos.y を修正)
        Vector3 newPos = cursor.transform.position;
        newPos.y = Mathf.Lerp(newPos.y, selected.transform.position.y, Time.deltaTime * scrollSpeed);
        cursor.transform.position = newPos;
    }
}
