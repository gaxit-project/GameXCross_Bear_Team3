using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

public class BearSpawner : MonoBehaviour
{
    [SerializeField] private string bearPrefabAddress = "Bear.prefab";

    [SerializeField] private float spawnDistance = 40.0f;
    [SerializeField] private float moveSpeed = 5.0f; // 移動速度

    async void Start()
    {
        await SpawnBear();
    }

    private async Task SpawnBear()
    {
        // 出現位置の計算
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnDistance;
        Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        UnityEngine.AI.NavMeshHit hit;
        if(UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 10.0f, UnityEngine.AI.NavMesh.AllAreas))
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