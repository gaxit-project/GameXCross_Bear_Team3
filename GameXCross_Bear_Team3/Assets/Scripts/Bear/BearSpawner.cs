using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using System.Collections.Generic;
using UniRx;
using DG.Tweening;

public class BearSpawner : MonoBehaviour
{
    [SerializeField] private string bearPrefabAddress = "Bear.prefab";
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float moveSpeed = 5.0f;

    // ウェーブごとの敵の数（要素0が1ウェーブ目）
    [SerializeField] private int[] enemiesPerWave = new int[] { 1, 1, 1 };

    [Header("スポーンマーカー設定")]
    [SerializeField] private Transform[] markerPositions; // マーカー表示位置（spawnPointsと対応）
    [SerializeField] private Color markerColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float markerWidth = 2.0f;   // 長方形の幅（X軸）
    [SerializeField] private float markerDepth = 3.0f;   // 長方形の奥行き（Z軸）
    [SerializeField] private float markerHeight = 0.1f;  // 長方形の高さ（Y軸）

    private List<GameObject> _spawnMarkers = new List<GameObject>();
    private List<int> _plannedSpawnPointIndices = new List<int>();

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // Setupフェーズ開始を検知してマーカーを表示
            GameManager.Instance.CurrentState
                .Where(state => state == GameState.Setup)
                .Subscribe(_ => ShowSpawnMarkers())
                .AddTo(this);

            // バトル開始を検知して敵を生成
            GameManager.Instance.CurrentState
                .Where(state => state == GameState.Battle)
                .Subscribe(_ => SpawnWaveEnemies())
                .AddTo(this);
        }
    }

    /// <summary>
    /// Setupフェーズ開始時：次のウェーブのスポーン位置を事前決定してマーカー表示
    /// </summary>
    private void ShowSpawnMarkers()
    {
        // 既存のマーカーをクリア
        ClearSpawnMarkers();

        Debug.Log("[Marker] ShowSpawnMarkers開始");

        int currentWaveIndex = GameManager.Instance.CurrentWave.Value - 1;

        // ウェーブの敵数を取得
        int count = (currentWaveIndex < enemiesPerWave.Length)
            ? enemiesPerWave[currentWaveIndex]
            : enemiesPerWave[enemiesPerWave.Length - 1];

        Debug.Log($"[Marker] ウェーブ{currentWaveIndex + 1}のマーカーを表示。{count}体が出現予定。");
        Debug.Log($"[Marker] spawnPoints配列: {(spawnPoints != null ? spawnPoints.Length : 0)}個, markerPositions配列: {(markerPositions != null ? markerPositions.Length : 0)}個");

        _plannedSpawnPointIndices.Clear();

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Marker] spawnPointsが未設定またはサイズ0です。マーカーをスキップします。");
            return;
        }

        // 利用可能なスポーン位置のリストを作成
        List<int> availableSpawnPoints = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            availableSpawnPoints.Add(i);
        }

        // スポーン位置を事前決定（重複なし）
        for (int i = 0; i < count; i++)
        {
            if (availableSpawnPoints.Count == 0)
            {
                Debug.LogWarning($"[Marker] 敵{i + 1}体目：利用可能なスポーン位置がありません。リストをリセットします。");
                // 全てのスポーン位置を使い切った場合は再利用
                for (int j = 0; j < spawnPoints.Length; j++)
                {
                    availableSpawnPoints.Add(j);
                }
            }

            // ランダムに選択して削除
            int randomListIndex = Random.Range(0, availableSpawnPoints.Count);
            int spawnPointIndex = availableSpawnPoints[randomListIndex];
            availableSpawnPoints.RemoveAt(randomListIndex);

            _plannedSpawnPointIndices.Add(spawnPointIndex);
            Debug.Log($"[Marker] 敵{i + 1}: spawnPoint[{spawnPointIndex}]を選択（残り{availableSpawnPoints.Count}箇所）");
        }

        // マーカー位置をグループごとに決定
        // spawnPointIndexをグループ化: 0,1,2 → markerIndex 0、3,4,5 → markerIndex 1、など
        HashSet<int> displayedMarkers = new HashSet<int>();
        
        for (int i = 0; i < _plannedSpawnPointIndices.Count; i++)
        {
            int spawnPointIndex = _plannedSpawnPointIndices[i];
            
            // spawnPointIndexを3で割ってグループ化
            // spawnPoint[0,1,2] → markerIndex 0
            // spawnPoint[3,4,5] → markerIndex 1
            // spawnPoint[6,7,8] → markerIndex 2
            // spawnPoint[9,10,11] → markerIndex 3
            int markerIndex = spawnPointIndex / 3;

            // このマーカーがまだ表示されていなければ表示
            if (displayedMarkers.Add(markerIndex))
            {
                if (markerPositions != null && markerIndex < markerPositions.Length)
                {
                    Vector3 markerPos = markerPositions[markerIndex].position;
                    
                    // 4方向に配置：各方向で90°異なる
                    float rotation = markerIndex * 90f;
                    
                    Debug.Log($"[Marker] マーカー生成: spawnPoint[{spawnPointIndex}](グループ{markerIndex}) → markerPosition[{markerIndex}] at {markerPos}, rotation={rotation}°");
                    GameObject marker = CreateSpawnMarker(markerPos, rotation);
                    _spawnMarkers.Add(marker);
                }
                else
                {
                    Debug.LogWarning($"[Marker] markerPositions[{markerIndex}]にアクセスできません（配列サイズ: {(markerPositions != null ? markerPositions.Length : 0)}）");
                }
            }
            else
            {
                Debug.Log($"[Marker] マーカースキップ: spawnPoint[{spawnPointIndex}]はグループ{markerIndex}（既に表示済み）");
            }
        }
        Debug.Log($"[Marker] マーカー生成完了: {_spawnMarkers.Count}個のマーカーを配置");
    }

    /// <summary>
    /// spawnPointのインデックスから対応するmarkerPositionのインデックスを取得
    /// </summary>
    private int GetMarkerIndexForSpawnPoint(int spawnPointIndex)
    {
        if (markerPositions == null || markerPositions.Length == 0) return -1;
        
        // 1対1対応の場合（spawnPointsとmarkerPositionsの要素数が同じ）
        if (spawnPointIndex < markerPositions.Length)
        {
            return spawnPointIndex;
        }
        
        // 要素数が異なる場合は循環して対応
        return spawnPointIndex % markerPositions.Length;
    }

    /// <summary>
    /// 地面マーカーを生成
    /// </summary>
    private GameObject CreateSpawnMarker(Vector3 position, float rotationY = 0f)
    {
        // 長方形のマーカーを作成
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "SpawnMarker";
        marker.transform.position = position + Vector3.up * (markerHeight / 2);
        marker.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        marker.transform.localScale = new Vector3(markerWidth, markerHeight, markerDepth);

        // マテリアル設定
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = markerColor;
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }

        // コライダーは不要なので削除
        Collider collider = marker.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        // パルスアニメーション
        marker.transform.DOScale(marker.transform.localScale * 1.2f, 1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(marker);

        return marker;
    }

    /// <summary>
    /// スポーンマーカーをクリア
    /// </summary>
    private void ClearSpawnMarkers()
    {
        Debug.Log($"[Marker] マーカークリア: {_spawnMarkers.Count}個のマーカーを削除");
        foreach (var marker in _spawnMarkers)
        {
            if (marker != null)
            {
                marker.transform.DOKill();
                Destroy(marker);
            }
        }
        _spawnMarkers.Clear();
    }

    private async void SpawnWaveEnemies()
    {
        Debug.Log($"[Marker] バトル開始: {_spawnMarkers.Count}個のマーカーをフェードアウト");
        // マーカーをフェードアウトして削除
        foreach (var marker in _spawnMarkers)
        {
            if (marker != null)
            {
                marker.transform.DOKill();
                marker.transform.DOScale(Vector3.zero, 0.5f)
                    .OnComplete(() => Destroy(marker));
            }
        }
        _spawnMarkers.Clear();

        int currentWaveIndex = GameManager.Instance.CurrentWave.Value - 1;

        // 設定値が足りない場合は最後の設定を使う
        int count = (currentWaveIndex < enemiesPerWave.Length)
            ? enemiesPerWave[currentWaveIndex]
            : enemiesPerWave[enemiesPerWave.Length - 1];

        Debug.Log($"Spawner: ウェーブ{currentWaveIndex + 1}開始。{count}体のクマを生成します。");

        for (int i = 0; i < _plannedSpawnPointIndices.Count && i < count; i++)
        {
            int spawnPointIndex = _plannedSpawnPointIndices[i];
            Vector3 spawnPos = spawnPoints[spawnPointIndex].position;

            // NavMesh上の位置にスナップ
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            Debug.Log($"敵{i + 1}: spawnPoint[{spawnPointIndex}]から出現");
            await SpawnBearAsync(spawnPos);

            // 複数生成する場合は少しずらす
            if (count > 1) await Task.Delay(1000);
        }

        _plannedSpawnPointIndices.Clear();
        GameManager.Instance.NotifySpawningComplete();
    }

    private async Task SpawnBearAsync(Vector3 spawnPos)
    {
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