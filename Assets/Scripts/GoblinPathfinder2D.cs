using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/// <summary>Runs fine-grained A* on Ground and treats non-trigger 2D colliders as obstacles.</summary>
[DisallowMultipleComponent]
public sealed class GoblinPathfinder2D : MonoBehaviour, IGoblinPathPlanner2D
{
    [SerializeField] [Tooltip("길찾기 영역을 정의하는 지형 타일맵입니다.")]
    [FormerlySerializedAs("ground")]
    private Tilemap groundMap;
    [SerializeField, Min(0.05f)]
    [Tooltip("A* 노드 한 칸의 월드 크기입니다.")]
    private float nodeSize = 0.25f;
    [SerializeField] [Tooltip("경로를 막는 장애물 레이어입니다.")]
    [FormerlySerializedAs("blockingLayers")]
    private LayerMask blockLayers = ~0;
    [SerializeField, Min(0f)] [Tooltip("장애물 주위에 추가할 경로 여유 거리입니다.")]
    [FormerlySerializedAs("obstaclePadding")]
    private float obstPad = 0.14f;
    [SerializeField, Min(0.05f)] [Tooltip("경로 부드럽게 하기의 검사 간격입니다.")]
    [FormerlySerializedAs("smoothingSampleDistance")]
    private float smoothStep = 0.1f;
    [SerializeField, Min(0.1f)] [Tooltip("시작 위치를 통행 가능 노드로 찾는 최대 거리입니다.")]
    [FormerlySerializedAs("nearestWalkableSearchDistance")]
    private float startSnap = 2f;
    [SerializeField, Min(0.05f)] [Tooltip("목표 위치를 통행 가능 노드로 보정하는 최대 거리입니다.")]
    [FormerlySerializedAs("maximumGoalSnapDistance")]
    private float goalSnap = 0.5f;
    [SerializeField, Min(100)] [Tooltip("한 번의 길찾기에서 확장할 최대 노드 수입니다.")]
    [FormerlySerializedAs("maxExpandedNodes")]
    private int maxNodes = 50000;

    private static readonly Vector2Int[] Neighbours =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1),
        new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    private readonly Collider2D[] overlapBuffer = new Collider2D[64];
    private ContactFilter2D obstacleFilter;
    private BoundsInt cachedCellBounds;
    private Vector2 gridOrigin;
    private int nodeColumns;
    private int nodeRows;
    private float cachedNodeSize;
    private Matrix4x4 cachedGroundTransform;
    private bool gridInitialized;
    private bool obstacleFilterInitialized;

    private struct NodeRecord
    {
        public float Cost;
        public Vector2Int Parent;
        public bool HasParent;
    }

    private struct OpenEntry
    {
        public Vector2Int Node;
        public float Cost;
        public float Priority;
    }

    private sealed class MinHeap
    {
        private readonly List<OpenEntry> entries = new List<OpenEntry>();
        public int Count => entries.Count;

        public void Push(OpenEntry entry)
        {
            entries.Add(entry);
            var index = entries.Count - 1;
            while (index > 0)
            {
                var parent = (index - 1) / 2;
                if (entries[parent].Priority <= entries[index].Priority) break;
                Swap(index, parent);
                index = parent;
            }
        }

        public OpenEntry Pop()
        {
            var first = entries[0];
            var lastIndex = entries.Count - 1;
            entries[0] = entries[lastIndex];
            entries.RemoveAt(lastIndex);
            var index = 0;
            while (true)
            {
                var left = index * 2 + 1;
                if (left >= entries.Count) break;
                var right = left + 1;
                var smallest = right < entries.Count && entries[right].Priority < entries[left].Priority ? right : left;
                if (entries[index].Priority <= entries[smallest].Priority) break;
                Swap(index, smallest);
                index = smallest;
            }
            return first;
        }

        private void Swap(int a, int b)
        {
            var temporary = entries[a];
            entries[a] = entries[b];
            entries[b] = temporary;
        }
    }

    public bool IsReady => EnsureGridInitialized();
    public float NodeSize { get { EnsureGridInitialized(); return cachedNodeSize; } }
    public Vector2 GridOrigin { get { EnsureGridInitialized(); return gridOrigin; } }
    public int NodeColumns { get { EnsureGridInitialized(); return nodeColumns; } }
    public int NodeRows { get { EnsureGridInitialized(); return nodeRows; } }

    private void Reset() => groundMap = GetComponent<Tilemap>();

    private void Awake()
    {
        ResolveGroundTilemap();
        InitializeObstacleFilter();
        EnsureGridInitialized();
    }

    private void OnEnable() => ResolveGroundTilemap();

    private void ResolveGroundTilemap()
    {
        if (groundMap != null) return;
        groundMap = GetComponent<Tilemap>();
        if (groundMap != null) return;

        var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (var tilemap in tilemaps)
        {
            if (tilemap == null) continue;
            if (groundMap == null) groundMap = tilemap;
            if (tilemap.name != "Ground") continue;
            groundMap = tilemap;
            break;
        }
    }

    /// <summary>Builds the shared grid dimensions used by routing and visualization.</summary>
    public bool EnsureGridInitialized()
    {
        ResolveGroundTilemap();
        if (groundMap == null || nodeSize <= 0f) return false;

        var bounds = groundMap.cellBounds;
        var groundTransform = groundMap.transform.localToWorldMatrix;
        if (gridInitialized && bounds.Equals(cachedCellBounds) &&
            Mathf.Approximately(nodeSize, cachedNodeSize) && cachedGroundTransform.Equals(groundTransform))
            return nodeColumns > 0 && nodeRows > 0;

        var minimum = groundMap.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin, bounds.zMin));
        var maximum = groundMap.CellToWorld(new Vector3Int(bounds.xMax, bounds.yMax, bounds.zMin));
        var width = Mathf.Abs(maximum.x - minimum.x);
        var height = Mathf.Abs(maximum.y - minimum.y);
        gridOrigin = new Vector2(Mathf.Min(minimum.x, maximum.x), Mathf.Min(minimum.y, maximum.y));
        nodeColumns = Mathf.CeilToInt(width / nodeSize);
        nodeRows = Mathf.CeilToInt(height / nodeSize);
        cachedCellBounds = bounds;
        cachedNodeSize = nodeSize;
        cachedGroundTransform = groundTransform;
        gridInitialized = nodeColumns > 0 && nodeRows > 0;
        InitializeObstacleFilter();
        return gridInitialized;
    }

    private void InitializeObstacleFilter()
    {
        var mask = blockLayers.value;
        var obstacleLayer = LayerMask.NameToLayer("Obstacle");
        var selectableLayer = LayerMask.NameToLayer("Selectable");
        if (obstacleLayer >= 0) mask |= 1 << obstacleLayer;
        if (selectableLayer >= 0) mask |= 1 << selectableLayer;
        obstacleFilter = new ContactFilter2D();
        obstacleFilter.SetLayerMask(mask);
        obstacleFilter.useTriggers = false;
        obstacleFilterInitialized = true;
    }

    public Vector2 GetNodeCenter(Vector2Int node)
    {
        EnsureGridInitialized();
        return GetNodeCenterInternal(node);
    }

    public bool IsInsideGrid(Vector2Int node)
    {
        EnsureGridInitialized();
        return IsInsideGridInternal(node);
    }

    private Vector2 GetNodeCenterInternal(Vector2Int node)
    {
        return gridOrigin + new Vector2((node.x + 0.5f) * cachedNodeSize, (node.y + 0.5f) * cachedNodeSize);
    }

    private bool IsInsideGridInternal(Vector2Int node)
    {
        return node.x >= 0 && node.y >= 0 && node.x < nodeColumns && node.y < nodeRows;
    }

    public bool IsObstacleAt(Vector2 worldPosition, float clearanceRadius)
    {
        if (!obstacleFilterInitialized) InitializeObstacleFilter();
        if (groundMap == null || !groundMap.HasTile(groundMap.WorldToCell(worldPosition))) return false;
        return IsBlockedByCollider(worldPosition, clearanceRadius);
    }

    public bool TryFindPath(Vector2 start, Vector2 requestedDestination, float bodyRadius, List<Vector2> result)
    {
        result.Clear();
        if (!EnsureGridInitialized()) return false;
        if (!obstacleFilterInitialized) InitializeObstacleFilter();

        Physics2D.SyncTransforms();
        if (!TryFindNearestWalkableNode(start, bodyRadius, startSnap, out var startNode) ||
            !TryFindNearestWalkableNode(requestedDestination, bodyRadius,
                Mathf.Min(startSnap, goalSnap), out var goalNode)) return false;

        var rawPath = new List<Vector2>();
        if (!FindGridPath(startNode, goalNode, bodyRadius, rawPath)) return false;
        SmoothPath(start, requestedDestination, bodyRadius, rawPath, result);
        return result.Count > 0;
    }

    private bool FindGridPath(Vector2Int start, Vector2Int goal, float clearance, List<Vector2> rawPath)
    {
        var records = new Dictionary<Vector2Int, NodeRecord>(512);
        var open = new MinHeap();
        var closed = new HashSet<Vector2Int>();
        records[start] = new NodeRecord { Cost = 0f };
        open.Push(new OpenEntry { Node = start, Cost = 0f, Priority = Heuristic(start, goal) });
        var expandedNodes = 0;

        while (open.Count > 0 && expandedNodes < maxNodes)
        {
            var entry = open.Pop();
            if (closed.Contains(entry.Node) || entry.Cost > records[entry.Node].Cost) continue;
            var current = entry.Node;
            if (current == goal)
            {
                Reconstruct(current, records, rawPath);
                return true;
            }
            closed.Add(current);
            expandedNodes++;

            foreach (var direction in Neighbours)
            {
                var next = current + direction;
                if (closed.Contains(next) || !IsNodeWalkable(next, clearance)) continue;
                var diagonal = direction.x != 0 && direction.y != 0;
                if (diagonal && !CanMoveDiagonally(current, direction, clearance)) continue;

                var tentativeCost = records[current].Cost + (diagonal ? 1.41421356f : 1f);
                if (records.TryGetValue(next, out var existing) && tentativeCost >= existing.Cost) continue;
                records[next] = new NodeRecord { Cost = tentativeCost, Parent = current, HasParent = true };
                open.Push(new OpenEntry
                {
                    Node = next,
                    Cost = tentativeCost,
                    Priority = tentativeCost + Heuristic(next, goal)
                });
            }
        }

        return false;
    }

    private bool TryFindNearestWalkableNode(Vector2 worldPosition, float clearance, float searchDistance,
        out Vector2Int bestNode)
    {
        var center = WorldToNode(worldPosition);
        var searchRadius = Mathf.CeilToInt(searchDistance / cachedNodeSize);
        var bestDistance = float.PositiveInfinity;
        bestNode = default;

        for (var y = center.y - searchRadius; y <= center.y + searchRadius; y++)
        for (var x = center.x - searchRadius; x <= center.x + searchRadius; x++)
        {
            var candidate = new Vector2Int(x, y);
            if (!IsNodeWalkable(candidate, clearance)) continue;
            var distance = (GetNodeCenterInternal(candidate) - worldPosition).sqrMagnitude;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            bestNode = candidate;
        }

        return bestDistance <= searchDistance * searchDistance;
    }

    private Vector2Int WorldToNode(Vector2 worldPosition)
    {
        var local = (worldPosition - gridOrigin) / cachedNodeSize;
        return new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
    }

    private bool IsNodeWalkable(Vector2Int node, float clearance)
    {
        if (!IsInsideGridInternal(node)) return false;
        var world = GetNodeCenterInternal(node);
        var cell = groundMap.WorldToCell(world);
        return groundMap.HasTile(cell) && !IsBlockedByCollider(world, clearance);
    }

    private bool IsBlockedByCollider(Vector2 position, float clearance)
    {
        if (!obstacleFilterInitialized) InitializeObstacleFilter();
        var radius = Mathf.Max(0.01f, clearance + obstPad);
        var hitCount = Physics2D.OverlapCircle(position, radius, obstacleFilter, overlapBuffer);
        for (var i = 0; i < hitCount; i++)
        {
            var hit = overlapBuffer[i];
            if (hit == null || hit.GetComponentInParent<GoblinAgent2D>() != null) continue;
            return true;
        }
        return hitCount >= overlapBuffer.Length;
    }

    private bool CanMoveDiagonally(Vector2Int from, Vector2Int direction, float clearance)
    {
        var horizontal = from + new Vector2Int(direction.x, 0);
        var vertical = from + new Vector2Int(0, direction.y);
        return IsNodeWalkable(horizontal, clearance) && IsNodeWalkable(vertical, clearance);
    }

    private void SmoothPath(Vector2 start, Vector2 requestedDestination, float clearance,
        List<Vector2> rawPath, List<Vector2> result)
    {
        var anchor = start;
        var index = 0;
        while (index < rawPath.Count)
        {
            var furthestVisible = index;
            for (var candidate = rawPath.Count - 1; candidate >= index; candidate--)
            {
                if (!IsSegmentWalkable(anchor, rawPath[candidate], clearance)) continue;
                furthestVisible = candidate;
                break;
            }

            anchor = rawPath[furthestVisible];
            result.Add(anchor);
            index = furthestVisible + 1;
        }

        if (groundMap.HasTile(groundMap.WorldToCell(requestedDestination)) &&
            !IsBlockedByCollider(requestedDestination, clearance) &&
            IsSegmentWalkable(anchor, requestedDestination, clearance))
        {
            if (result.Count == 0 || Vector2.Distance(result[result.Count - 1], requestedDestination) > 0.03f)
                result.Add(requestedDestination);
        }
    }

    private bool IsSegmentWalkable(Vector2 from, Vector2 to, float clearance)
    {
        var distance = Vector2.Distance(from, to);
        var sampleStep = Mathf.Min(smoothStep, cachedNodeSize * 0.5f);
        var steps = Mathf.Max(1, Mathf.CeilToInt(distance / sampleStep));
        for (var step = 1; step <= steps; step++)
        {
            var point = Vector2.Lerp(from, to, step / (float)steps);
            if (!groundMap.HasTile(groundMap.WorldToCell(point)) || IsBlockedByCollider(point, clearance)) return false;
        }
        return true;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        var dx = Mathf.Abs(a.x - b.x);
        var dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + 0.41421356f * Mathf.Min(dx, dy);
    }

    private void Reconstruct(Vector2Int node, Dictionary<Vector2Int, NodeRecord> records, List<Vector2> result)
    {
        var reverse = new List<Vector2Int> { node };
        while (records[node].HasParent)
        {
            node = records[node].Parent;
            reverse.Add(node);
        }
        for (var i = reverse.Count - 2; i >= 0; i--) result.Add(GetNodeCenter(reverse[i]));
    }
}
