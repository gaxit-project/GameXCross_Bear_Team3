using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class BearSpawner : MonoBehaviour
{
    [SerializeField] private string bearPrefabAddress = "Bear.prefab";

    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float moveSpeed = 5.0f; // 移動速度

    private bool hasSpawned = false;
    private bool startedOnce = false; // DontDestroyOnLoad 時に Start が二度呼ばれないことへの対応

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnEnable()
    {
        // シーン再読み込み時にも確実に生成されるように
        hasSpawned = false;
    }

    private void Start()
    {
        startedOnce = true;
        // StartCoroutineを使わず、直接asyncメソッドを呼び出す
        if (!hasSpawned)
        {
            SpawnBearAsync();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // DontDestroyOnLoad で残っている場合にリセットして再スポーンする
        if (!startedOnce) return; // 通常シーンオブジェクトは再生成されるので不要

        hasSpawned = false;
        if (gameObject.activeInHierarchy)
        {
            SpawnBearAsync();
        }
    }

    private async void SpawnBearAsync()
    {
        // 少し待機してから実行（シーン遷移直後の初期化を待つ）
        await Task.Delay(100);

        if(spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("BearSpawner: スポーン地点 (Spawn Points) が設定されていません！Inspectorで設定してください。");
            return;
        }

        // 出現位置をランダムに決定
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform targetPoint = spawnPoints[randomIndex];
        
        if (targetPoint == null)
        {
            Debug.LogError("BearSpawner: スポーン地点が無効です。");
            return;
        }

        Vector3 spawnPos = targetPoint.position;

        UnityEngine.AI.NavMeshHit hit;
        if(UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        try
        {
            // 生成
            var op = Addressables.InstantiateAsync(bearPrefabAddress, spawnPos, Quaternion.identity);
            var bearObj = await op.Task;

            if(bearObj != null)
            {
                var controller = bearObj.GetComponent<BearController>();
                if (controller != null)
                {
                    controller.Initialize(moveSpeed);
                    hasSpawned = true;
                    Debug.Log("BearSpawner: クマを生成しました。");
                }
                else
                {
                    Debug.LogWarning("BearSpawner: BearControllerコンポーネントが見つかりませんでした。");
                }
            }
            else
            {
                Debug.LogError("BearSpawner: クマの生成に失敗しました。Addressablesの設定を確認してください。");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BearSpawner: エラーが発生しました: {e.Message}");
        }
    }
}
