using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AutoScrollToFocus : MonoBehaviour
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

        // 1. まずは画面中央に来るような理想のターゲット位置を計算
        float targetX = -targetRect.localPosition.x + (scrollRect.viewport.rect.width * 0.5f);

        // 2. スクロールできる限界の範囲を計算
        // Contentの幅 と Viewportの幅 を取得
        float contentWidth = scrollRect.content.rect.width;
        float viewportWidth = scrollRect.viewport.rect.width;

        // 右端の限界座標（通常はマイナス値）： Viewportの幅 - Contentの幅
        // ※Contentの方が小さい場合は0にする
        float minX = viewportWidth - contentWidth;
        if (minX > 0) minX = 0;

        // 左端の限界座標： 通常は 0
        float maxX = 0f;

        // 3. 計算したtargetXが、限界を超えないように制限（Clamp）する
        targetX = Mathf.Clamp(targetX, minX, maxX);

        // 現在の位置と目標位置の間をスムーズに移動（Lerp）
        Vector2 newPos = scrollRect.content.anchoredPosition;
        newPos.x = Mathf.Lerp(newPos.x, targetX, Time.deltaTime * scrollSpeed);

        // 値を適用
        scrollRect.content.anchoredPosition = newPos;
    }
}