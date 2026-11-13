using UnityEngine;

public interface IGoblinUnitAgent
{
    Vector2 Position { get; }
    float BodyRadius { get; }
    Vector2 ColliderWorldOffset { get; }
    bool SetDestination(Vector2 worldPosition);
}
