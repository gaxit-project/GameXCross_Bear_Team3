using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

public class BearSpawner : MonoBehaviour
{
    [SerializeField] private string bearPrefabAddress = "Bear.prefab";

    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float moveSpeed = 5.0f; // 移動速度

    async void Start()
    {
        await SpawnBear();
    }

    private async Task SpawnBear()
    {
        if(spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("BearSpawner: スポーン地点 (Spawn Points) が設定されていません！Inspectorで設定してください。");
            return;
        }
        // 出現位置をランダムに決定
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform targetPoint = spawnPoints[randomIndex];
        Vector3 spawnPos = targetPoint.position;

        UnityEngine.AI.NavMeshHit hit;
        if(UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        // 生成
        var op = Addressables.InstantiateAsync(bearPrefabAddress, spawnPos, Quaternion.identity);
        var bearObj = await op.Task;

        if(bearObj != null)
        {
            var controller = bearObj.GetComponent<BearController>();
            if (controller) controller.Initialize(moveSpeed);
        }
    }
}
