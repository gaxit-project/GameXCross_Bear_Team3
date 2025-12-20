using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using UniRx;

public class BearSpawner : MonoBehaviour
{
    [SerializeField] private string bearPrefabAddress = "Bear.prefab";
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float moveSpeed = 5.0f;

    // ウェーブごとの敵の数（要素0が1ウェーブ目）
    [SerializeField] private int[] enemiesPerWave = new int[] { 1, 1, 1 };

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // バトル開始を検知して敵を生成
            GameManager.Instance.CurrentState
                .Where(state => state == GameState.Battle)
                .Subscribe(_ => SpawnWaveEnemies())
                .AddTo(this);
        }
    }

    private async void SpawnWaveEnemies()
    {
        int currentWaveIndex = GameManager.Instance.CurrentWave.Value - 1; // 0始まりにする

        // 設定値が足りない場合は最後の設定を使う
        int count = (currentWaveIndex < enemiesPerWave.Length)
            ? enemiesPerWave[currentWaveIndex]
            : enemiesPerWave[enemiesPerWave.Length - 1];

        Debug.Log($"Spawner: ウェーブ{currentWaveIndex + 1}開始。{count}体のクマを生成します。");

        for (int i = 0; i < count; i++)
        {
            // GameManagerに登録（生成前にカウントアップしておくのが安全）
            //GameManager.Instance.RegisterEnemy();

            await SpawnBearAsync();

            // 複数生成する場合は少しずらす
            if (count > 1) await Task.Delay(1000);
        }

        GameManager.Instance.NotifySpawningComplete();
    }

    private async Task SpawnBearAsync()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Vector3 spawnPos = spawnPoints[randomIndex].position;

        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        try
        {
            var op = Addressables.InstantiateAsync(bearPrefabAddress, spawnPos, Quaternion.identity);
            var bearObj = await op.Task;

            if (bearObj != null)
            {
                var controller = bearObj.GetComponent<BearController>();
                if (controller != null)
                {
                    controller.Initialize(moveSpeed);
                }
            }
            else
            {
                // 生成失敗したらカウントを戻す（そうしないとウェーブが終わらなくなる）
                GameManager.Instance.ReportEnemyDefeated();
            }
        }
        catch
        {
            GameManager.Instance.ReportEnemyDefeated();
        }
    }
}