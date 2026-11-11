using UnityEngine;

/// <summary>Renders the current or most recently issued route for one goblin.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GoblinAgent2D), typeof(LineRenderer))]
public sealed class GoblinPathLineRenderer : MonoBehaviour
{
    [SerializeField] private GoblinAgent2D agent;
    [SerializeField] private LineRenderer line;
    [SerializeField] private Color pathColor = new Color(1f, 0.72f, 0.18f, 0.95f);
    [SerializeField, Min(0.005f)] private float lineWidth = 0.045f;
    [SerializeField] private float renderHeight = -0.08f;
    [SerializeField] private bool DebugShowPath = true;
    private Material runtimeMaterial;

    private void Reset() => CacheComponents();

    private void Awake()
    {
        CacheComponents();
        ConfigureLine();
    }

    private void LateUpdate()
    {
        if (!DebugShowPath)
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
        current.z += renderHeight;
        line.SetPosition(0, current);
        for (var i = 0; i < path.Count; i++)
            line.SetPosition(i + 1, new Vector3(path[i].x, path[i].y, current.z));
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
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
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = pathColor;
        line.endColor = pathColor;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.sortingOrder = 10;
        line.positionCount = 0;
        line.enabled = false;

        if (line.sharedMaterial != null) return;
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) return;
        runtimeMaterial = new Material(shader) { color = Color.white };
        line.sharedMaterial = runtimeMaterial;
    }
}
