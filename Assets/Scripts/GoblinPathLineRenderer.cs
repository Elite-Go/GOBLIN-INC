using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Renders the current or most recently issued route for one goblin.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GoblinAgent2D), typeof(LineRenderer))]
public sealed class GoblinPathLineRenderer : MonoBehaviour
{
    [SerializeField] [Tooltip("경로를 표시할 고블린 이동 컴포넌트입니다.")] private GoblinAgent2D agent;
    [SerializeField] [Tooltip("경로 표시용 LineRenderer입니다.")] private LineRenderer line;
    [SerializeField] [Tooltip("경로 점선의 색상입니다.")]
    [FormerlySerializedAs("pathColor")]
    private Color pathCol = new Color(1f, 0.72f, 0.18f, 0.95f);
    [SerializeField, Min(0.005f)] [Tooltip("경로 점선의 두께입니다.")]
    [FormerlySerializedAs("lineWidth")]
    private float lineW = 0.045f;
    [SerializeField, Min(0.01f)] [Tooltip("경로 점선 사이의 간격입니다.")]
    [FormerlySerializedAs("dotSpacing")]
    private float dotGap = 0.2f;
    [SerializeField, Range(0.1f, 1f)] [Tooltip("점선 한 칸에서 점이 차지하는 비율입니다.")] private float dotFill = 0.45f;
    [SerializeField] [Tooltip("경로를 표시할 Z축 오프셋입니다.")]
    [FormerlySerializedAs("renderHeight")]
    private float renderZ = -0.08f;
    [SerializeField] [Tooltip("경로 시각화를 표시할지 결정합니다.")]
    [FormerlySerializedAs("DebugShowPath")]
    private bool DebugPath = true;
    private Material runtimeMaterial;
    private Texture2D runtimeDotTexture;

    private void Reset() => CacheComponents();

    private void Awake()
    {
        CacheComponents();
        ConfigureLine();
    }

    private void LateUpdate()
    {
        if (!DebugPath)
        {
            if (line != null) line.enabled = false;
            return;
        }

        if (agent == null || line == null)
        {
            if (line != null) line.enabled = false;
            return;
        }

        var path = agent.Path;
        if (path.Count == 0)
        {
            line.enabled = false;
            return;
        }

        line.enabled = true;
        line.positionCount = path.Count + 1;
        var current = transform.position;
        current.z += renderZ;
        line.SetPosition(0, current);
        for (var i = 0; i < path.Count; i++)
            line.SetPosition(i + 1, new Vector3(path[i].x, path[i].y, current.z));
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
        if (runtimeDotTexture != null) Destroy(runtimeDotTexture);
    }

    private void CacheComponents()
    {
        if (agent == null) agent = GetComponent<GoblinAgent2D>();
        if (line == null) line = GetComponent<LineRenderer>();
    }

    private void ConfigureLine()
    {
        if (line == null) return;
        line.useWorldSpace = true;
        line.startWidth = lineW;
        line.endWidth = lineW;
        line.startColor = pathCol;
        line.endColor = pathCol;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.sortingOrder = 10;
        line.textureMode = LineTextureMode.Tile;
        line.textureScale = new Vector2(1f / dotGap, 1f);
        line.positionCount = 0;
        line.enabled = false;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        runtimeDotTexture = CreateDotTexture();
        runtimeMaterial = new Material(shader) { color = Color.white };
        runtimeMaterial.mainTexture = runtimeDotTexture;
        line.sharedMaterial = runtimeMaterial;
    }

    private Texture2D CreateDotTexture()
    {
        const int width = 32;
        const int height = 16;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Goblin Path Dots",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat
        };
        var pixels = new Color[width * height];
        var center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        var radius = Mathf.Min(height * 0.5f, width * dotFill * 0.5f);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var distance = Vector2.Distance(new Vector2(x, y), center);
            var alpha = Mathf.Clamp01(radius + 0.75f - distance);
            pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
