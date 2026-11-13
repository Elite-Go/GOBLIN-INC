using System.Collections.Generic;
using UnityEngine;

/// <summary>Contract for a 2D navigation implementation used by goblin agents.</summary>
public interface IGoblinPathPlanner2D
{
    bool IsReady { get; }
    bool TryFindPath(Vector2 start, Vector2 destination, List<Vector2> result);
}
