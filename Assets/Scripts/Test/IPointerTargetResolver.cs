using UnityEngine;

/// <summary>Resolves a screen-space pointer location to a world object target.</summary>
public interface IPointerTargetResolver
{
    bool TryResolve(Vector2 screenPosition, out SpritePointerTarget target);
}
