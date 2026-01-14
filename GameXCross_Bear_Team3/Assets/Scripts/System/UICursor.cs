using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICursor : MonoBehaviour
{
    [SerializeField] float scrollSpeed = 10f; // スクロールの速さ
    [SerializeField] public GameObject cursor;

    [Header("SE設定")]
    [SerializeField] private string moveSE = "Select"; // 鳴らしたいSEのキー名
    private GameObject lastSelected; // 「さっきまで選択されていたもの」を覚える変数


    void Start()
    {
        // 最初は現在の選択状態を初期値として入れておく（開幕で音が鳴るのを防ぐため）
        lastSelected = EventSystem.current.currentSelectedGameObject;
    }


    void Update()
    {
        // 今選択されているオブジェクトを取得
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        // 何も選択されていない、または選択されたものがScrollRectの中身でない場合は何もしない
        if (selected == null )
        {
            return;
        }

        if (selected != lastSelected)
        {
            // 選択されているものが「さっき」と違うなら、移動したということ＝音を鳴らす
            SEmanager.Instance.Play("cursor");

            // 「さっき」を「今」の情報で上書き更新
            lastSelected = selected;
        }

        // 選択されたボタンのTransformを取得
        Transform target = selected.GetComponent<Transform>();

        float targetX = target.position.x;

        // 現在の位置と目標位置の間をスムーズに移動（Lerp）
        Vector3 newPos = cursor.transform.position;
        newPos.x = Mathf.Lerp(newPos.x, targetX, Time.deltaTime * scrollSpeed);
        cursor.transform.position = newPos;
    }
}
