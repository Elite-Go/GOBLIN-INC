using UnityEngine;

public readonly struct SpritePointerTarget
{
    public SpriteRenderer Renderer { get; }
    public GameObject GameObject => Renderer != null ? Renderer.gameObject : null;
    public Vector3 WorldPosition => Renderer != null ? Renderer.bounds.center : Vector3.zero;

    public Vector2 GetStandOffPosition(Vector2 approachFrom, float bodyRadius, float standOffDistance)
    {
        if (Renderer == null) return approachFrom;

        var pivot = (Vector2)Renderer.transform.position;
        return GetStandOffPositionFromDirection(approachFrom - pivot, bodyRadius, standOffDistance);
    }

    public Vector2 GetStandOffPositionFromDirection(Vector2 approachDirection, float bodyRadius,
        float standOffDistance)
    {
        if (Renderer == null) return approachDirection;

        var pivot = (Vector2)Renderer.transform.position;
        var direction = approachDirection;
        if (direction.sqrMagnitude <= 0.0001f) direction = Vector2.up;
        direction.Normalize();

        var localDirection = Renderer.transform.InverseTransformVector(new Vector3(direction.x, direction.y, 0f));
        var localBounds = Renderer.localBounds;
        var boundaryDistance = float.PositiveInfinity;

        if (localDirection.x > 0.0001f)
            boundaryDistance = Mathf.Min(boundaryDistance, localBounds.max.x / localDirection.x);
        else if (localDirection.x < -0.0001f)
            boundaryDistance = Mathf.Min(boundaryDistance, localBounds.min.x / localDirection.x);

        if (localDirection.y > 0.0001f)
            boundaryDistance = Mathf.Min(boundaryDistance, localBounds.max.y / localDirection.y);
        else if (localDirection.y < -0.0001f)
            boundaryDistance = Mathf.Min(boundaryDistance, localBounds.min.y / localDirection.y);

        if (float.IsInfinity(boundaryDistance) || boundaryDistance < 0f)
            boundaryDistance = Mathf.Max(Renderer.bounds.extents.x, Renderer.bounds.extents.y);

        var visualBoundary = pivot + direction * boundaryDistance;
        return visualBoundary + direction * (Mathf.Max(0f, bodyRadius) + Mathf.Max(0f, standOffDistance));
    }

    public Vector2 GetPivotOrbitPosition(Vector2 pivot, Vector2 approachDirection, float orbitRadius)
    {
        var direction = approachDirection.sqrMagnitude <= 0.0001f
            ? Vector2.up
            : approachDirection.normalized;
        return pivot + direction * Mathf.Max(0f, orbitRadius);
    }

    public SpritePointerTarget(SpriteRenderer renderer)
    {
        Renderer = renderer;
    }

}
