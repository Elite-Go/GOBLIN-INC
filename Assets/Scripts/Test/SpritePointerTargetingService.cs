using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Finds the topmost rendered sprite under a screen-space pointer.</summary>
[DisallowMultipleComponent]
public sealed class SpritePointerTargetingService : MonoBehaviour, IPointerTargetResolver
{
    [SerializeField] [Tooltip("화면의 포인터 위치를 판정할 카메라입니다.")]
    [FormerlySerializedAs("worldCamera")]
    private Camera cam;
    [SerializeField] [Tooltip("마우스 선택을 허용할 레이어입니다.")]
    [FormerlySerializedAs("selectableLayers")]
    private LayerMask selectLayers;

    private SpriteRenderer[] spriteRenderers = System.Array.Empty<SpriteRenderer>();
    private float nextRendererRefreshTime;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        RefreshSpriteRenderersIfNeeded();
    }

    public bool TryResolve(Vector2 screenPosition, out SpritePointerTarget target)
    {
        target = default;
        if (cam == null) return false;
        RefreshSpriteRenderersIfNeeded();

        var ray = cam.ScreenPointToRay(screenPosition);
        var renderer = FindTopmostSprite(screenPosition, ray);
        if (renderer == null) return false;

        target = new SpritePointerTarget(renderer);
        return true;
    }

    private void RefreshSpriteRenderersIfNeeded()
    {
        if (Time.unscaledTime < nextRendererRefreshTime) return;
        spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        nextRendererRefreshTime = Time.unscaledTime + 0.5f;
    }

    private SpriteRenderer FindTopmostSprite(Vector2 pointerScreen, Ray pointerRay)
    {
        SpriteRenderer bestRenderer = null;
        var bestLayerValue = int.MinValue;
        var bestSortingOrder = int.MinValue;
        var bestRendererPriority = int.MinValue;
        var bestSortDepth = float.PositiveInfinity;
        var bestDistance = float.PositiveInfinity;
        var usesOrthographicSort = GetTransparencySortMode(cam) == TransparencySortMode.Orthographic;
        var sortAxis = GetTransparencySortAxis(cam);

        foreach (var candidate in spriteRenderers)
        {
            if (candidate == null || !candidate.enabled || candidate.sprite == null ||
                !candidate.gameObject.activeInHierarchy ||
                (selectLayers.value & (1 << candidate.gameObject.layer)) == 0 ||
                !ContainsPointer(candidate, cam, pointerScreen, pointerRay, out var distance))
                continue;

            var layerValue = SortingLayer.GetLayerValueFromID(candidate.sortingLayerID);
            var sortingOrder = candidate.sortingOrder;
            var rendererPriority = candidate.rendererPriority;
            var sortPosition = candidate.spriteSortPoint == SpriteSortPoint.Pivot
                ? candidate.transform.position
                : candidate.bounds.center;
            var sortDepth = Vector3.Dot(sortPosition, sortAxis);
            var sameLayerAndOrder = layerValue == bestLayerValue && sortingOrder == bestSortingOrder;
            var sameRendererPriority = rendererPriority == bestRendererPriority;
            var isBetter = layerValue > bestLayerValue ||
                           (layerValue == bestLayerValue && sortingOrder > bestSortingOrder) ||
                           (sameLayerAndOrder && rendererPriority > bestRendererPriority) ||
                           (sameLayerAndOrder && sameRendererPriority && usesOrthographicSort && distance < bestDistance) ||
                           (sameLayerAndOrder && sameRendererPriority &&
                            (!usesOrthographicSort || Mathf.Approximately(distance, bestDistance)) && sortDepth < bestSortDepth) ||
                           (sameLayerAndOrder && sameRendererPriority && Mathf.Approximately(sortDepth, bestSortDepth) &&
                            distance < bestDistance);
            if (!isBetter) continue;

            bestRenderer = candidate;
            bestLayerValue = layerValue;
            bestSortingOrder = sortingOrder;
            bestRendererPriority = rendererPriority;
            bestSortDepth = sortDepth;
            bestDistance = distance;
        }

        return bestRenderer;
    }

    private static Vector3 GetTransparencySortAxis(Camera camera)
    {
        var mode = GetTransparencySortMode(camera);
        var axis = mode == TransparencySortMode.CustomAxis
            ? camera.transparencySortMode == TransparencySortMode.CustomAxis
                ? camera.transparencySortAxis
                : UnityEngine.Rendering.GraphicsSettings.transparencySortAxis
            : camera.orthographic ? camera.transform.up : camera.transform.forward;
        return axis.sqrMagnitude > 0f ? axis.normalized : Vector3.forward;
    }

    private static TransparencySortMode GetTransparencySortMode(Camera camera)
    {
        return camera.transparencySortMode == TransparencySortMode.Default
            ? UnityEngine.Rendering.GraphicsSettings.transparencySortMode
            : camera.transparencySortMode;
    }

    private static bool ContainsPointer(SpriteRenderer renderer, Camera camera, Vector2 pointerScreen,
        Ray pointerRay, out float distance)
    {
        distance = 0f;
        var localBounds = renderer.localBounds;
        var localCenter = localBounds.center;
        var plane = new Plane(renderer.transform.forward, renderer.transform.TransformPoint(localCenter));
        if (!plane.Raycast(pointerRay, out distance) || distance < 0f) return false;

        var min = localBounds.min;
        var max = localBounds.max;
        var a = camera.WorldToScreenPoint(renderer.transform.TransformPoint(new Vector3(min.x, min.y, localCenter.z)));
        var b = camera.WorldToScreenPoint(renderer.transform.TransformPoint(new Vector3(max.x, min.y, localCenter.z)));
        var c = camera.WorldToScreenPoint(renderer.transform.TransformPoint(new Vector3(max.x, max.y, localCenter.z)));
        var d = camera.WorldToScreenPoint(renderer.transform.TransformPoint(new Vector3(min.x, max.y, localCenter.z)));
        if (a.z < 0f || b.z < 0f || c.z < 0f || d.z < 0f) return false;

        var point = new Vector3(pointerScreen.x, pointerScreen.y, 0f);
        return IsPointInTriangle(point, a, b, c) || IsPointInTriangle(point, a, c, d);
    }

    private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        var ab = Cross(b - a, point - a);
        var bc = Cross(c - b, point - b);
        var ca = Cross(a - c, point - c);
        var hasNegative = ab < 0f || bc < 0f || ca < 0f;
        var hasPositive = ab > 0f || bc > 0f || ca > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
