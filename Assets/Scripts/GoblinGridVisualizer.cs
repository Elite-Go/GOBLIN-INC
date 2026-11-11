using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

/// <summary>Renders separate debug overlays for A* nodes and obstacle-blocked nodes.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public sealed class GoblinGridVisualizer : MonoBehaviour
{
    [SerializeField] private Tilemap ground;
    [SerializeField] private GoblinPathfinder2D pathfinder;
    [SerializeField, FormerlySerializedAs("showGrid")] private bool DebugShowGrid = true;
    [SerializeField, FormerlySerializedAs("showBlockedNodes")] private bool DebugShowBlockedNodes = true;
    [SerializeField] private Color gridColor = new Color(0.08f, 0.07f, 0.05f, 0.28f);
    [SerializeField] private Color blockedNodeColor = new Color(1f, 0.12f, 0.1f, 0.52f);
    [SerializeField, Min(0.002f)] private float lineWidth = 0.008f;
    [SerializeField, Min(0.01f)] private float blockedNodeClearance = 0.34f;
    [SerializeField, Min(0.002f)] private float blockedNodeInset = 0.02f;
    [SerializeField] private int sortingOrderOffset = 1;

    private GameObject gridOverlayObject;
    private GameObject blockedOverlayObject;
    private Mesh gridMesh;
    private Mesh blockedMesh;
    private Material runtimeMaterial;
    private MeshRenderer gridRenderer;
    private MeshRenderer blockedRenderer;

    private void Reset()
    {
        ground = GetComponent<Tilemap>();
        pathfinder = GetComponent<GoblinPathfinder2D>();
    }

    private void Awake()
    {
        if (ground == null) ground = GetComponent<Tilemap>();
        if (pathfinder == null) pathfinder = GetComponent<GoblinPathfinder2D>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GoblinPathfinder2D>();
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
        if (ground == null || pathfinder == null || !pathfinder.EnsureGridInitialized()) return;
        var columns = pathfinder.NodeColumns;
        var rows = pathfinder.NodeRows;
        var nodeSize = pathfinder.NodeSize;
        var origin = pathfinder.GridOrigin;
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
            gridRenderer = CreateOverlayRenderer("Goblin Debug Node Grid", gridMesh, sortingOrderOffset, out gridOverlayObject);
        if (blockedMesh != null)
            blockedRenderer = CreateOverlayRenderer("Goblin Debug Blocked Nodes", blockedMesh,
                sortingOrderOffset + 1, out blockedOverlayObject);
    }

    private Mesh BuildGridMesh(int columns, int rows, float nodeSize, Vector2 origin)
    {
        var vertices = new List<Vector3>((columns + rows + 2) * 4);
        var triangles = new List<int>((columns + rows + 2) * 6);
        var colors = new List<Color>((columns + rows + 2) * 4);
        var baseZ = ground.transform.position.z - 0.025f;
        var scale = Mathf.Max(0.001f,
            Mathf.Max(Mathf.Abs(ground.transform.lossyScale.x), Mathf.Abs(ground.transform.lossyScale.y)));
        var localLineWidth = lineWidth / scale;

        for (var x = 0; x <= columns; x++)
        {
            var worldX = origin.x + x * nodeSize;
            var from = WorldToLocal(new Vector3(worldX, origin.y, baseZ));
            var to = WorldToLocal(new Vector3(worldX, origin.y + rows * nodeSize, baseZ));
            AddThickLine(vertices, triangles, colors, from, to, localLineWidth, gridColor);
        }
        for (var y = 0; y <= rows; y++)
        {
            var worldY = origin.y + y * nodeSize;
            var from = WorldToLocal(new Vector3(origin.x, worldY, baseZ));
            var to = WorldToLocal(new Vector3(origin.x + columns * nodeSize, worldY, baseZ));
            AddThickLine(vertices, triangles, colors, from, to, localLineWidth, gridColor);
        }

        return CreateMesh("Goblin A* Debug Grid", vertices, triangles, colors);
    }

    private Mesh BuildBlockedNodesMesh(int columns, int rows, float nodeSize, Vector2 origin)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var colors = new List<Color>();
        var halfSize = Mathf.Max(0.01f, nodeSize * 0.5f - blockedNodeInset);
        var baseZ = ground.transform.position.z - 0.03f;

        for (var y = 0; y < rows; y++)
        for (var x = 0; x < columns; x++)
        {
            var center = origin + new Vector2((x + 0.5f) * nodeSize, (y + 0.5f) * nodeSize);
            if (!ground.HasTile(ground.WorldToCell(center)) ||
                !pathfinder.IsObstacleAt(center, blockedNodeClearance)) continue;

            var min = WorldToLocal(new Vector3(center.x - halfSize, center.y - halfSize, baseZ));
            var max = WorldToLocal(new Vector3(center.x + halfSize, center.y + halfSize, baseZ));
            AddQuad(vertices, triangles, colors, min, max, blockedNodeColor);
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
        var groundRenderer = ground.GetComponent<TilemapRenderer>();
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
        if (gridRenderer != null) gridRenderer.enabled = DebugShowGrid;
        if (blockedRenderer != null) blockedRenderer.enabled = DebugShowBlockedNodes;
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

    private Vector3 WorldToLocal(Vector3 world) => ground.transform.InverseTransformPoint(world);

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
