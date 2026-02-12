using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICursor_setting : MonoBehaviour
{
    [SerializeField] float scrollSpeed = 20f;
    [SerializeField] public GameObject cursor;

    // カーソルのRectTransform（サイズ変更用）
    private RectTransform cursorRectTransform;

    [Header("SE設定")]
    [SerializeField] private string moveSE = "Select";
    private GameObject lastSelected;

    [Header("位置調整")]
    [SerializeField] private Vector3 cursorOffset = Vector3.zero;

    [Header("サイズ設定")]
    // Ract -> Rect (Rectangleの略) に修正しておきました
    [SerializeField] private Vector2 normalSize = new Vector2(100, 100); // 通常時のサイズ
    [SerializeField] private Vector2 sliderSize = new Vector2(50, 50);   // スライダー時のサイズ

    void Start()
    {
        lastSelected = EventSystem.current.currentSelectedGameObject;

        // cursorオブジェクトについているRectTransformを取得しておく
        if (cursor != null)
        {
            cursorRectTransform = cursor.GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null) return;

        if (Time.timeScale <= 0)
        {
            lastSelected = selected;
            return;
        }

        if (selected != lastSelected)
        {
            SEmanager.Instance.Play("cursor");
            lastSelected = selected;
        }

        // --- 位置とサイズの決定 ---

        Vector3 targetPos;
        Vector2 targetSize; // 目標とするサイズ

        Slider currentSlider = selected.GetComponent<Slider>();

        // スライダーかつ、つまみ(Handle)がある場合
        if (currentSlider != null && currentSlider.handleRect != null)
        {
            // 1. 位置は「つまみ」に合わせる
            targetPos = currentSlider.handleRect.position;

            // 2. サイズは「スライダー用（小さめ）」にする
            targetSize = sliderSize;
        }
        else
        {
            // 1. 位置は「ボタン本体」に合わせる
            targetPos = selected.transform.position;

            // 2. サイズは「通常用」にする
            targetSize = normalSize;
        }

        // オフセットを加える
        targetPos += cursorOffset;


        // --- 反映処理 ---

        // 位置のアニメーション (Lerp)
        Vector3 newPos = cursor.transform.position;
        newPos.x = Mathf.Lerp(newPos.x, targetPos.x, Time.deltaTime * scrollSpeed);
        newPos.y = Mathf.Lerp(newPos.y, targetPos.y, Time.deltaTime * scrollSpeed);
        cursor.transform.position = newPos;

        // サイズの変更 (即時反映)
        // ※ヌルっと変えたい場合はここもLerpにしますが、まずはパッと切り替わるようにしています
        if (cursorRectTransform != null)
        {
            cursorRectTransform.sizeDelta = targetSize;
        }
    }
}