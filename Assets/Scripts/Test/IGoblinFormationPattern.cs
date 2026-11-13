using System;
using UnityEngine;

/// <summary>Supplies a relative destination for one unit in a group order.</summary>
public interface IGoblinFormationPattern
{
    Vector2 GetOffset(int index, float spacing);
}

[Serializable]
public sealed class RadialGoblinFormationPattern : IGoblinFormationPattern
{
    [SerializeField]
    [Tooltip("대형에서 연속된 고블린 사이의 회전 간격입니다.")]
    [UnityEngine.Serialization.FormerlySerializedAs("angularStepDegrees")]
    private float angleStep = 137.50776f;

    public Vector2 GetOffset(int index, float spacing)
    {
        if (index <= 0) return Vector2.zero;
        var angle = index * angleStep * Mathf.Deg2Rad;
        var radius = Mathf.Sqrt(index) * spacing;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}
