using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AutoScrolltoFocus : MonoBehaviour
{
    [SerializeField] ScrollRect scrollRect; // スクロールビュー本体
    [SerializeField] float scrollSpeed = 10f; // スクロールの速さ

    void Update()
    {
        // 今選択されているオブジェクトを取得
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        // 何も選択されていない、または選択されたものがScrollRectの中身でない場合は何もしない
        if (selected == null || !selected.transform.IsChildOf(scrollRect.content))
        {
            return;
        }

        // 選択されたボタンのRectTransformを取得
        RectTransform targetRect = selected.GetComponent<RectTransform>();

        // ターゲットの位置に合わせてContentの目標位置（X座標）を計算
        // 「-(ボタンのローカル位置) + (ビューポートの幅 / 2)」でボタンを画面中央に持ってくる計算
        // ※微調整が必要な場合は末尾の数値をいじってください
        float targetX = -targetRect.localPosition.x + (scrollRect.viewport.rect.width * 0.5f);

        // 現在の位置と目標位置の間をスムーズに移動（Lerp）
        Vector2 newPos = scrollRect.content.anchoredPosition;
        newPos.x = Mathf.Lerp(newPos.x, targetX, Time.deltaTime * scrollSpeed);

        // 値を適用
        scrollRect.content.anchoredPosition = newPos;
    }
}
