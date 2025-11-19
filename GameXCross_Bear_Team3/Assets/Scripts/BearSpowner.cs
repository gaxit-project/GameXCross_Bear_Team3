using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

public class BearSpawner : MonoBehaviour
{
    [Header("Addressablesアドレス")]
    [SerializeField] private string bearPrefabAddress = "Bear.prefab";

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

        // 向きの計算
        Vector3 centerOnPlane = new Vector3(centerPoint.x, y, centerPoint.z);
        Vector3 initialDirection = (centerOnPlane - spownPoint).normalized;

        // Addressablesで生成
        var op = Addressables.InstantiateAsync(bearPrefabAddress, spownPoint, Quaternion.LookRotation(initialDirection));

        // ハンドルを保持
        bearHandle = op;

        // ロード完了を待機
        GameObject bear = await op.Task;

        if (op.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"クマ({bear.name})の生成成功");

            var bearMove = bear.GetComponent<BearMove>();
            if (bearMove != null)
            {
                bearMove.Initialize(moveSpeed, radius, centerPoint, initialDirection);
            }
        }
        else
        {
            Debug.LogError($"クマの出現に失敗: {op.OperationException}");
        }

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
