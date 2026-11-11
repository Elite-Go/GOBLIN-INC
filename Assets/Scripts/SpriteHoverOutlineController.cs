using UnityEngine;

/// <summary>Applies a temporary pixel outline to the highest-sorted sprite under the pointer.</summary>
[DisallowMultipleComponent]
public sealed class SpriteHoverOutlineController : MonoBehaviour
{
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int SpriteSizeId = Shader.PropertyToID("_OutlineSpriteSize");
    private static readonly int ReferenceResolutionId = Shader.PropertyToID("_OutlineReferenceResolution");
    private static readonly int PixelLocalSizeId = Shader.PropertyToID("_OutlinePixelLocalSize");
    private static readonly int MeshCenterId = Shader.PropertyToID("_OutlineMeshCenter");
    private static readonly int UvBoundsId = Shader.PropertyToID("_OutlineUvBounds");

    [SerializeField] private Camera worldCamera;
    [SerializeField] private Color outlineColor = new Color(1f, 0.86f, 0.25f, 1f);
    [SerializeField, Min(0f)] private float outlineWidthPixels = 1f;
    [SerializeField, Tooltip("Zero uses each sprite's source resolution. Set both dimensions to use a custom pixel reference.")]
    private Vector2Int outlineReferenceResolution;
    [Header("Debug")]
    [SerializeField] private bool DebugShowHoverOutline = true;

    private SpriteRenderer hoveredRenderer;
    private Material originalMaterial;
    private Material outlineMaterial;
    private Shader outlineShader;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        outlineShader = Shader.Find("Custom/Pixel Sprite Outline");
        if (outlineShader == null)
            Debug.LogError("SpriteHoverOutlineController could not find Custom/Pixel Sprite Outline.", this);
    }

    private void Update()
    {
        if (!DebugShowHoverOutline || worldCamera == null || outlineShader == null)
        {
            ClearHoveredSprite();
            return;
        }

        var pointerWorld = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        var hits = Physics2D.OverlapPointAll(new Vector2(pointerWorld.x, pointerWorld.y));
        var nextRenderer = FindTopmostSprite(hits);
        if (nextRenderer == hoveredRenderer) return;

        ClearHoveredSprite();
        if (nextRenderer != null) ApplyOutline(nextRenderer);
    }

    private static SpriteRenderer FindTopmostSprite(Collider2D[] hits)
    {
        SpriteRenderer bestRenderer = null;
        var bestLayerValue = int.MinValue;
        var bestSortingOrder = int.MinValue;
        var bestDistance = float.PositiveInfinity;

        foreach (var hit in hits)
        {
            if (hit == null || !hit.enabled || hit.isTrigger) continue;
            var candidate = hit.GetComponent<SpriteRenderer>();
            if (candidate == null) candidate = hit.GetComponentInParent<SpriteRenderer>();
            if (candidate == null || !candidate.enabled || candidate.sprite == null || !candidate.gameObject.activeInHierarchy)
                continue;

            var layerValue = SortingLayer.GetLayerValueFromID(candidate.sortingLayerID);
            var sortingOrder = candidate.sortingOrder;
            var distance = candidate.transform.position.z;
            var isBetter = layerValue > bestLayerValue ||
                           (layerValue == bestLayerValue && sortingOrder > bestSortingOrder) ||
                           (layerValue == bestLayerValue && sortingOrder == bestSortingOrder && distance < bestDistance);
            if (!isBetter) continue;

            bestRenderer = candidate;
            bestLayerValue = layerValue;
            bestSortingOrder = sortingOrder;
            bestDistance = distance;
        }

        return bestRenderer;
    }

    private void ApplyOutline(SpriteRenderer target)
    {
        hoveredRenderer = target;
        originalMaterial = target.sharedMaterial;
        outlineMaterial = originalMaterial != null
            ? new Material(originalMaterial)
            : new Material(outlineShader);
        outlineMaterial.name = $"{target.name} Hover Outline (Runtime)";
        outlineMaterial.shader = outlineShader;
        outlineMaterial.SetColor(OutlineColorId, outlineColor);
        outlineMaterial.SetFloat(OutlineWidthId, outlineWidthPixels);
        outlineMaterial.SetVector(SpriteSizeId, target.sprite.rect.size);

        var reference = outlineReferenceResolution.x > 0 && outlineReferenceResolution.y > 0
            ? (Vector2)outlineReferenceResolution
            : target.sprite.rect.size;
        outlineMaterial.SetVector(ReferenceResolutionId, reference);
        var pixelsPerUnit = Mathf.Max(1f, target.sprite.pixelsPerUnit);
        var localPixels = (Vector2)target.sprite.rect.size / reference * (outlineWidthPixels / pixelsPerUnit);
        outlineMaterial.SetVector(PixelLocalSizeId, localPixels);
        outlineMaterial.SetVector(MeshCenterId, target.sprite.bounds.center);

        var uvs = target.sprite.uv;
        var minUv = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var maxUv = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var uv in uvs)
        {
            minUv = Vector2.Min(minUv, uv);
            maxUv = Vector2.Max(maxUv, uv);
        }
        outlineMaterial.SetVector(UvBoundsId, new Vector4(minUv.x, minUv.y, maxUv.x, maxUv.y));
        target.sharedMaterial = outlineMaterial;
    }

    private void ClearHoveredSprite()
    {
        if (hoveredRenderer != null && hoveredRenderer.sharedMaterial == outlineMaterial)
            hoveredRenderer.sharedMaterial = originalMaterial;

        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
            outlineMaterial = null;
        }

        hoveredRenderer = null;
        originalMaterial = null;
    }

    private void OnDisable() => ClearHoveredSprite();
    private void OnDestroy() => ClearHoveredSprite();
}
