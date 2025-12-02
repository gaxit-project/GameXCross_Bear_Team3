using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICursor : MonoBehaviour
{
    [SerializeField] float scrollSpeed = 10f; // スクロールの速さ
    [SerializeField] public GameObject cursor;




    void Start()
    {
        
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

        // 選択されたボタンのTransformを取得
        Transform target = selected.GetComponent<Transform>();

        float targetX = target.position.x;

        // 現在の位置と目標位置の間をスムーズに移動（Lerp）
        Vector3 newPos = cursor.transform.position;
        newPos.x = Mathf.Lerp(newPos.x, targetX, Time.deltaTime * scrollSpeed);
        cursor.transform.position = newPos;
    }
}
