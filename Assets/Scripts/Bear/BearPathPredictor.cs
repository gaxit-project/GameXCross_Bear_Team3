using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 熊の NavMeshAgent の経路を LineRenderer で描画するスクリプト。
/// 地面に移動予測線を表示します。
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(LineRenderer))]
public class BearPathPredictor : MonoBehaviour
{
    private NavMeshAgent _agent;
    private LineRenderer _lineRenderer;
    private BearController _bearController;

    [Header("表示設定")]
    [SerializeField] private float yOffset = 0.1f; // 地面からの浮かせ（Z-Fighting防止）
    [SerializeField] private bool hideWhenStopped = true;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _lineRenderer = GetComponent<LineRenderer>();
        _bearController = GetComponent<BearController>();

        // LineRenderer の初期設定（スクリプトから制御しやすい最低限の設定）
        // _lineRenderer.startWidth = 0.3f;
        // _lineRenderer.endWidth = 0.3f;
        _lineRenderer.positionCount = 0;
        _lineRenderer.useWorldSpace = true;
    }

    void LateUpdate()
    {
        // 熊が死亡または罠にかかっている場合は非表示にする
        if (_bearController != null && (_bearController.IsDead || _bearController.IsParalyzed))
        {
            _lineRenderer.positionCount = 0;
            return;
        }

        // 経路が存在するかチェック
        if (!_agent.hasPath || (_agent.isStopped && hideWhenStopped))
        {
            _lineRenderer.positionCount = 0;
            return;
        }

        DrawPath();
    }

    private void DrawPath()
    {
        NavMeshPath path = _agent.path;
        if (path.corners.Length < 2)
        {
            _lineRenderer.positionCount = 0;
            return;
        }

        _lineRenderer.positionCount = path.corners.Length;

        for (int i = 0; i < path.corners.Length; i++)
        {
            Vector3 point = path.corners[i];
            point.y += yOffset; // 少し浮かせる
            _lineRenderer.SetPosition(i, point);
        }
    }
}
