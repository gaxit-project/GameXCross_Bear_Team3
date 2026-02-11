using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConstructionTimer : MonoBehaviour
{
    private GameObject _realPrefab;
    private float _timeLeft;
    private GameObject _timerUIInstance;
    private TextMeshProUGUI _tmpText;
    private Collider[] _allColliders;

    // 初期化：本物のプレハブと、待ち時間を受け取る
    public void Initialize(GameObject realPrefab, float duration, GameObject uiPrefab)
    {
        _realPrefab = realPrefab;
        _timeLeft = duration;

        _allColliders = GetComponentsInChildren<Collider>();

        // タイマーUIを表示（親をnullにして歪みを防ぐ）
        if (uiPrefab != null)
        {
            _timerUIInstance = Instantiate(uiPrefab, transform.position + Vector3.up * 2.0f, Quaternion.identity, null);
            _tmpText = _timerUIInstance.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        if (_timeLeft > 0)
        {
            _timeLeft -= Time.deltaTime;

            if (_timerUIInstance != null)
            {
                Vector3 uiPos;

                if (_allColliders != null && _allColliders.Length > 0)
                {
                    // 1つ目のboundsを基準にする
                    Bounds combinedBounds = _allColliders[0].bounds;

                    // 2つ目以降があれば合成（Encapsulate）して、全体の枠を広げる
                    for (int i = 1; i < _allColliders.Length; i++)
                    {
                        combinedBounds.Encapsulate(_allColliders[i].bounds);
                    }

                    // 合成した枠の中心 + (高さの半分) + 少し余白
                    uiPos = combinedBounds.center + Vector3.up * (combinedBounds.extents.y + 0.5f);
                }
                else
                {
                    // コライダーがない場合の予備（従来の計算）
                    uiPos = transform.position + Vector3.up * 30f;
                }

                _timerUIInstance.transform.position = uiPos;
                _timerUIInstance.transform.LookAt(Camera.main.transform);
                _timerUIInstance.transform.Rotate(0, 180, 0);

                if (_tmpText != null)
                    _tmpText.text = Mathf.Ceil(_timeLeft).ToString();
            }

            if (_timeLeft <= 0)
            {
                CompleteConstruction();
            }
        }  
    }

    private void CompleteConstruction()
    {
        // 本物を生成する
        if (_realPrefab != null)
        {
            Instantiate(_realPrefab, transform.position, transform.rotation);
            // if (SEmanager.Instance != null) SEmanager.Instance.Play("complete");
        }
        
        // 自分（ゴースト/仮設現場）を削除する
        Destroy(gameObject);
    }

    // 自分が消える時に、UIも一緒に消す（消し忘れ防止）
    private void OnDestroy()
    {
        if (_timerUIInstance != null)
        {
            Destroy(_timerUIInstance);
        }
    }
}