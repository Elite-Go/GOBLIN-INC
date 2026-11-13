using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Applies and restores the hover appearance for the current pointer target.</summary>
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

    [SerializeField] [Tooltip("마우스 아래 스프라이트를 찾는 서비스입니다.")]
    [FormerlySerializedAs("pointerTargeting")]
    private SpritePointerTargetingService targets;
    [SerializeField] [Tooltip("마우스 오버 외곽선 색상입니다.")]
    [FormerlySerializedAs("outlineColor")]
    private Color outlineCol = new Color(1f, 0.86f, 0.25f, 1f);
    [SerializeField, Min(0f)] [Tooltip("외곽선 두께를 픽셀 단위로 지정합니다.")]
    [FormerlySerializedAs("outlineWidthPixels")]
    private float outlinePx = 1f;
    [SerializeField]
    [Tooltip("외곽선 픽셀 기준 해상도입니다. 0이면 원본 스프라이트 해상도를 사용합니다.")]
    [FormerlySerializedAs("outlineReferenceResolution")]
    private Vector2Int refRes;
    [Header("Debug")]
    [SerializeField] [Tooltip("마우스 오버 외곽선 시각화를 표시할지 결정합니다.")]
    [FormerlySerializedAs("DebugShowHoverOutline")]
    private bool DebugOutline = true;

    private SpriteRenderer hoveredRenderer;
    private Material originalMaterial;
    private Material outlineMaterial;
    private Shader outlineShader;

    private void Awake()
    {
        if (targets == null) targets = FindFirstObjectByType<SpritePointerTargetingService>();
        outlineShader = Shader.Find("Custom/Pixel Sprite Outline");
        if (outlineShader == null)
            Debug.LogError("SpriteHoverOutlineController could not find Custom/Pixel Sprite Outline.", this);
    }

    private void LateUpdate()
    {
        if (!DebugOutline || targets == null || outlineShader == null ||
            !targets.TryResolve(Input.mousePosition, out var target))
        {
            ClearHoveredSprite();
            return;
        }

        if (target.Renderer == hoveredRenderer) return;
        ClearHoveredSprite();
        ApplyOutline(target.Renderer);
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
        outlineMaterial.SetColor(OutlineColorId, outlineCol);
        outlineMaterial.SetFloat(OutlineWidthId, outlinePx);
        outlineMaterial.SetVector(SpriteSizeId, target.sprite.rect.size);

        var reference = refRes.x > 0 && refRes.y > 0
            ? (Vector2)refRes
            : target.sprite.rect.size;
        outlineMaterial.SetVector(ReferenceResolutionId, reference);
        var pixelsPerUnit = Mathf.Max(1f, target.sprite.pixelsPerUnit);
        var localPixels = (Vector2)target.sprite.rect.size / reference * (outlinePx / pixelsPerUnit);
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
