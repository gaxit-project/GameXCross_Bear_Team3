using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using System.Linq;

public class BearSpawner : MonoBehaviour
{
    [Header("Addressablesアドレス")]
    [SerializeField] private string bearPrefabAddress = "Bear.prefab";

    [Header("ターゲット設定")]
    [SerializeField] private string houseTag = "House";

    [Header("クマの共通設定")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float radius = 25.0f;
    [SerializeField] private Vector3 centerPoint = Vector3.zero; 

    private AsyncOperationHandle<GameObject> bearHandle;

    async void Start()
    {
        await SpawnBearAsync();
    }

    void Update()
    {
        
    }

    private async Task SpawnBearAsync()
    {
        // 出現座標の計算
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);
        float x = centerPoint.x + radius * Mathf.Cos(randomAngle);
        float z = centerPoint.z + radius * Mathf.Sin(randomAngle);
        float y = centerPoint.y;

        Vector3 spownPoint = new Vector3(x, y, z);

        Collider nearestHouseCollider = GetNearestHouseCollider(spownPoint);

        if (nearestHouseCollider == null)
        {
            Debug.LogWarning("ターゲットとなる家(Tag: House)が見つかりません。出現を中止します。");
            return;
        }

        // 向きの計算
        Vector3 targetPoint = nearestHouseCollider.ClosestPoint(spownPoint);
        Vector3 directionToHouse = (targetPoint - spownPoint).normalized;
        directionToHouse.y = 0; // 水平方向のみ


        // Addressablesで生成
        var op = Addressables.InstantiateAsync(bearPrefabAddress, spownPoint, Quaternion.LookRotation(directionToHouse));
        // ハンドルを保持
        bearHandle = op;
        // ロード完了を待機
        GameObject bear = await op.Task;

        if (op.Status == AsyncOperationStatus.Succeeded)
        {
            var bearMove = bear.GetComponent<BearMove>();
            if (bearMove != null)
            {
                bearMove.Initialize(moveSpeed, radius, centerPoint, nearestHouseCollider);
            }
        }
        else
        {
            Debug.LogError($"クマの出現に失敗: {op.OperationException}");
        }

    }

    private Collider GetNearestHouseCollider(Vector3 referencePos)
    {
        GameObject[] houses = GameObject.FindGameObjectsWithTag(houseTag);

        if (houses.Length == 0) return null;

        var houseColliders = houses
            .Select(h => h.GetComponent<Collider>())
            .Where(c => c != null);

        if (!houseColliders.Any()) return null;

        Collider nearest = houseColliders
            .OrderBy(col => Vector3.SqrMagnitude(col.ClosestPoint(referencePos) - referencePos))
            .First();

        return nearest;
    }

    private void OnDestroy()
    {
        // Addressablesの解放
        if (bearHandle.IsValid())
        {
            Addressables.Release(bearHandle);
        }
    }
}
