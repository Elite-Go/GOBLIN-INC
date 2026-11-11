using System.Collections.Generic;
using UnityEngine;

/// <summary>Assigns compact, non-overlapping goal slots to a selected group of goblins.</summary>
public sealed class GoblinSquadMover : MonoBehaviour
{
    [SerializeField, Min(0.2f), Tooltip("Distance from the group click point to adjacent goblin slots.")]
    private float formationSpacing = 0.78f;

    public void Move(IReadOnlyList<GameObject> goblins, Vector2 requestedDestination)
    {
        if (goblins == null || goblins.Count == 0) return;

        for (var i = 0; i < goblins.Count; i++)
        {
            var goblin = goblins[i];
            if (goblin == null || !goblin.CompareTag("Goblin")) continue;

            var agent = goblin.GetComponent<GoblinAgent2D>();
            if (agent == null)
            {
                Debug.LogWarning($"Selected goblin '{goblin.name}' has no GoblinAgent2D.");
                continue;
            }

            var slot = requestedDestination + GetFormationOffset(i) * formationSpacing;
            if (!agent.SetDestination(slot))
                Debug.LogWarning($"No walkable route found for '{goblin.name}' near {slot}.");
        }
    }

    private static Vector2 GetFormationOffset(int index)
    {
        if (index == 0) return Vector2.zero;
        var angle = index * 137.50776f * Mathf.Deg2Rad;
        var radius = Mathf.Sqrt(index);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}
