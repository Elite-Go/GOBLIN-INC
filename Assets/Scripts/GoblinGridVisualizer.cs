using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/// <summary>Renders separate debug overlays for A* nodes and obstacle-blocked nodes.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public sealed class GoblinGridVisualizer : MonoBehaviour
{
    [SerializeField] [Tooltip("길찾기에 사용하는 지형 타일맵입니다.")]
    [FormerlySerializedAs("ground")]
    private Tilemap groundMap;
    [SerializeField] [Tooltip("그리드와 장애물 정보를 제공하는 길찾기 컴포넌트입니다.")]
    [FormerlySerializedAs("pathfinder")]
    private GoblinPathfinder2D path;
    [SerializeField, FormerlySerializedAs("showGrid")] [Tooltip("A* 그리드 시각화를 표시할지 결정합니다.")]
    [FormerlySerializedAs("DebugShowGrid")]
    private bool DebugGrid = true;
    [SerializeField, FormerlySerializedAs("showBlockedNodes")] [Tooltip("장애물 노드 시각화를 표시할지 결정합니다.")]
    [FormerlySerializedAs("DebugShowBlockedNodes")]
    private bool DebugBlocked = true;
    [SerializeField] [Tooltip("그리드 선 색상입니다.")]
    [FormerlySerializedAs("gridColor")]
    private Color gridCol = new Color(0.08f, 0.07f, 0.05f, 0.28f);
    [SerializeField] [Tooltip("장애물 노드 색상입니다.")]
    [FormerlySerializedAs("blockedNodeColor")]
    private Color blockedCol = new Color(1f, 0.12f, 0.1f, 0.52f);
    [SerializeField, Min(0.002f)] [Tooltip("그리드 선의 두께입니다.")]
    [FormerlySerializedAs("lineWidth")]
    private float lineW = 0.008f;
    [SerializeField, Min(0.01f)] [Tooltip("장애물 표시 판정에 더할 여유 거리입니다.")]
    [FormerlySerializedAs("blockedNodeClearance")]
    private float blockClear = 0.34f;
    [SerializeField, Min(0.002f)] [Tooltip("장애물 노드 표시 안쪽 여백입니다.")]
    [FormerlySerializedAs("blockedNodeInset")]
    private float blockInset = 0.02f;
    [SerializeField] [Tooltip("타일맵 렌더 순서에 더할 오프셋입니다.")]
    [FormerlySerializedAs("sortingOrderOffset")]
    private int sortOff = 1;

    private GameObject gridOverlayObject;
    private GameObject blockedOverlayObject;
    private Mesh gridMesh;
    private Mesh blockedMesh;
    private Material runtimeMaterial;
    private MeshRenderer gridRenderer;
    private MeshRenderer blockedRenderer;

    private void Reset()
    {
        groundMap = GetComponent<Tilemap>();
        path = GetComponent<GoblinPathfinder2D>();
    }

    private void Awake()
    {
        if (groundMap == null) groundMap = GetComponent<Tilemap>();
        if (path == null) path = GetComponent<GoblinPathfinder2D>();
        if (path == null) path = FindFirstObjectByType<GoblinPathfinder2D>();
        CreateOverlays();
        UpdateOverlayVisibility();
    }

    private void LateUpdate() => UpdateOverlayVisibility();

    private void OnDestroy()
    {
        if (gridMesh != null) Destroy(gridMesh);
        if (blockedMesh != null) Destroy(blockedMesh);
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
        if (gridOverlayObject != null) Destroy(gridOverlayObject);
        if (blockedOverlayObject != null) Destroy(blockedOverlayObject);
    }

    private void CreateOverlays()
    {
        if (groundMap == null || path == null || !path.EnsureGridInitialized()) return;
        var columns = path.NodeColumns;
        var rows = path.NodeRows;
        var nodeSize = path.NodeSize;
        var origin = path.GridOrigin;
        if (columns <= 0 || rows <= 0) return;

        Physics2D.SyncTransforms();
        gridMesh = BuildGridMesh(columns, rows, nodeSize, origin);
        blockedMesh = BuildBlockedNodesMesh(columns, rows, nodeSize, origin);
        if (gridMesh == null && blockedMesh == null) return;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("GoblinGridVisualizer could not find the Sprites/Default shader.");
            DestroyMeshes();
            return;
        }

        runtimeMaterial = new Material(shader) { name = "Goblin A* Debug Material", color = Color.white };
        if (gridMesh != null)
            gridRenderer = CreateOverlayRenderer("Goblin Debug Node Grid", gridMesh, sortOff, out gridOverlayObject);
        if (blockedMesh != null)
            blockedRenderer = CreateOverlayRenderer("Goblin Debug Blocked Nodes", blockedMesh,
                sortOff + 1, out blockedOverlayObject);
    }

    private Mesh BuildGridMesh(int columns, int rows, float nodeSize, Vector2 origin)
    {
        var vertices = new List<Vector3>((columns + rows + 2) * 4);
        var triangles = new List<int>((columns + rows + 2) * 6);
        var colors = new List<Color>((columns + rows + 2) * 4);
        var baseZ = groundMap.transform.position.z - 0.025f;
        var scale = Mathf.Max(0.001f,
            Mathf.Max(Mathf.Abs(groundMap.transform.lossyScale.x), Mathf.Abs(groundMap.transform.lossyScale.y)));
        var localLineWidth = lineW / scale;

        for (var x = 0; x <= columns; x++)
        {
            var worldX = origin.x + x * nodeSize;
            var from = WorldToLocal(new Vector3(worldX, origin.y, baseZ));
            var to = WorldToLocal(new Vector3(worldX, origin.y + rows * nodeSize, baseZ));
            AddThickLine(vertices, triangles, colors, from, to, localLineWidth, gridCol);
        }
        for (var y = 0; y <= rows; y++)
        {
            var worldY = origin.y + y * nodeSize;
            var from = WorldToLocal(new Vector3(origin.x, worldY, baseZ));
            var to = WorldToLocal(new Vector3(origin.x + columns * nodeSize, worldY, baseZ));
            AddThickLine(vertices, triangles, colors, from, to, localLineWidth, gridCol);
        }

        return CreateMesh("Goblin A* Debug Grid", vertices, triangles, colors);
    }

    private Mesh BuildBlockedNodesMesh(int columns, int rows, float nodeSize, Vector2 origin)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var colors = new List<Color>();
        var halfSize = Mathf.Max(0.01f, nodeSize * 0.5f - blockInset);
        var baseZ = groundMap.transform.position.z - 0.03f;

        for (var y = 0; y < rows; y++)
        for (var x = 0; x < columns; x++)
        {
            var center = origin + new Vector2((x + 0.5f) * nodeSize, (y + 0.5f) * nodeSize);
            if (!groundMap.HasTile(groundMap.WorldToCell(center)) ||
                !path.IsObstacleAt(center, blockClear)) continue;

            var min = WorldToLocal(new Vector3(center.x - halfSize, center.y - halfSize, baseZ));
            var max = WorldToLocal(new Vector3(center.x + halfSize, center.y + halfSize, baseZ));
            AddQuad(vertices, triangles, colors, min, max, blockedCol);
        }

        return CreateMesh("Goblin Debug Blocked Nodes", vertices, triangles, colors);
    }

    private MeshRenderer CreateOverlayRenderer(string objectName, Mesh mesh, int order, out GameObject overlayObject)
    {
        overlayObject = new GameObject(objectName);
        overlayObject.transform.SetParent(transform, false);
        overlayObject.transform.localPosition = Vector3.zero;
        overlayObject.transform.localRotation = Quaternion.identity;
        overlayObject.transform.localScale = Vector3.one;
        overlayObject.AddComponent<MeshFilter>().sharedMesh = mesh;

        var renderer = overlayObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = runtimeMaterial;
        var groundRenderer = groundMap.GetComponent<TilemapRenderer>();
        if (groundRenderer != null)
        {
            renderer.sortingLayerID = groundRenderer.sortingLayerID;
            renderer.sortingOrder = groundRenderer.sortingOrder + order;
        }
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private void UpdateOverlayVisibility()
    {
        if (gridRenderer != null) gridRenderer.enabled = DebugGrid;
        if (blockedRenderer != null) blockedRenderer.enabled = DebugBlocked;
    }

    private void DestroyMeshes()
    {
        if (gridMesh != null) Destroy(gridMesh);
        if (blockedMesh != null) Destroy(blockedMesh);
        gridMesh = null;
        blockedMesh = null;
    }

    private Mesh CreateMesh(string meshName, List<Vector3> vertices, List<int> triangles, List<Color> colors)
    {
        if (vertices.Count == 0) return null;
        var mesh = new Mesh { name = meshName };
        if (vertices.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private Vector3 WorldToLocal(Vector3 world) => groundMap.transform.InverseTransformPoint(world);

    private static void AddQuad(List<Vector3> vertices, List<int> triangles, List<Color> colors,
        Vector3 min, Vector3 max, Color color)
    {
        var start = vertices.Count;
        vertices.Add(new Vector3(min.x, min.y, min.z));
        vertices.Add(new Vector3(max.x, min.y, min.z));
        vertices.Add(new Vector3(max.x, max.y, max.z));
        vertices.Add(new Vector3(min.x, max.y, max.z));
        AddRectangleTrianglesFacingCamera(triangles, start);
        for (var i = 0; i < 4; i++) colors.Add(color);
    }

    private static void AddThickLine(List<Vector3> vertices, List<int> triangles, List<Color> colors,
        Vector3 from, Vector3 to, float thickness, Color color)
    {
        var direction = (to - from).normalized;
        var perpendicular = new Vector3(-direction.y, direction.x, 0f) * (thickness * 0.5f);
        var start = vertices.Count;
        vertices.Add(from - perpendicular);
        vertices.Add(from + perpendicular);
        vertices.Add(to + perpendicular);
        vertices.Add(to - perpendicular);
        AddLineTrianglesFacingCamera(triangles, start);
        for (var i = 0; i < 4; i++) colors.Add(color);
    }

    private static void AddRectangleTrianglesFacingCamera(List<int> triangles, int start)
    {
        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
        triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
    }

    private static void AddLineTrianglesFacingCamera(List<int> triangles, int start)
    {
        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
    }
}
