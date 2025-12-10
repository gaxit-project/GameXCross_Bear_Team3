using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx;
using System.Linq;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private string resultSceneName = "Result";
    [SerializeField] private float sceneTransitionDelay = 2.0f; // シーン遷移までの遅延時間（秒）
    [SerializeField] private float checkInterval = 0.5f; // チェック間隔（秒）

    private HouseHealth[] allHouses;
    private bool hasTransitioned = false;
    private CompositeDisposable disposables = new CompositeDisposable();

    private void Start()
    {
        // シーン内のすべての家（Houseタグを持つオブジェクト）を取得
        RefreshHouseList();

        if (allHouses == null || allHouses.Length == 0)
        {
            Debug.LogWarning("GameManager: 家が見つかりませんでした。Houseタグが正しく設定されているか確認してください。");
            return;
        }

        // 定期的にすべての家の破壊状態をチェック
        Observable.Interval(System.TimeSpan.FromSeconds(checkInterval))
            .Where(_ => !hasTransitioned)
            .Subscribe(_ =>
            {
                CheckAllHousesDestroyed();
            })
            .AddTo(disposables);
    }

    private void OnDestroy()
    {
        disposables?.Dispose();
    }

    /// <summary>
    /// 家のリストを更新する
    /// </summary>
    public void RefreshHouseList()
    {
        // "House"タグを持つすべてのオブジェクトを取得
        var houseObjects = GameObject.FindGameObjectsWithTag("House");
        allHouses = houseObjects
            .Select(h => h.GetComponent<HouseHealth>())
            .Where(h => h != null)
            .ToArray();

        Debug.Log($"GameManager: {allHouses.Length}個の家を検出しました。");
    }

    /// <summary>
    /// すべての家が破壊されたかチェック
    /// </summary>
    private void CheckAllHousesDestroyed()
    {
        if (allHouses == null || allHouses.Length == 0)
        {
            return;
        }

        // すべての家が破壊されているかチェック
        bool allDestroyed = allHouses.All(house => 
            house == null || !house.gameObject.activeSelf || house.IsDestroyed);

        if (allDestroyed && !hasTransitioned)
        {
            hasTransitioned = true;
            Debug.Log("GameManager: すべての家が破壊されました。Resultシーンに遷移します。");
            
            // 遅延後にシーン遷移
            Observable.Timer(System.TimeSpan.FromSeconds(sceneTransitionDelay))
                .Subscribe(_ =>
                {
                    TransitionToResultScene();
                })
                .AddTo(disposables);
        }
    }

    /// <summary>
    /// Resultシーンに遷移
    /// </summary>
    private void TransitionToResultScene()
    {
        try
        {
            SceneManager.LoadScene(resultSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameManager: シーン '{resultSceneName}' が見つかりません。エラー: {e.Message}");
        }
    }
}

