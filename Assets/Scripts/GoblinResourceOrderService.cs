using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GoblinResourceOrderService : MonoBehaviour
{
    public bool TryIssueOrder(IReadOnlyList<GameObject> goblins, SpritePointerTarget target,
        GoblinSquadMover squadMover)
    {
        if (goblins == null || target.Renderer == null || squadMover == null ||
            !HasResourcesTagInParents(target.Renderer.transform))
            return false;

        var resource = FindResource(target.Renderer.gameObject);
        if (resource == null) return false;
        if (resource.IsDepleted) return true;

        var resourceComponent = resource as Component;
        var pivot = resourceComponent != null
            ? (Vector2)resourceComponent.transform.position
            : (Vector2)target.Renderer.transform.position;

        var workerCount = 0;
        foreach (var goblin in goblins)
        {
            if (goblin == null || !goblin.CompareTag("Goblin")) continue;
            if (goblin.GetComponent<GoblinResourceHarvester>() == null ||
                goblin.GetComponent<GoblinAgent2D>() == null) continue;
            workerCount++;
        }

        var assignedCount = 0;
        var workerIndex = 0;
        foreach (var goblin in goblins)
        {
            if (goblin == null || !goblin.CompareTag("Goblin")) continue;
            var harvester = goblin.GetComponent<GoblinResourceHarvester>();
            var agent = goblin.GetComponent<GoblinAgent2D>();
            if (harvester == null || agent == null) continue;

            var angle = workerCount <= 1 ? Mathf.PI * 0.5f :
                Mathf.PI * 2f * workerIndex / workerCount;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var destination = target.GetPivotOrbitPosition(pivot, direction,
                squadMover.TargetStandOffDistance);
            workerIndex++;
            if (harvester.OrderHarvest(resource, destination)) assignedCount++;
        }

        if (assignedCount == 0)
            Debug.LogWarning($"No selected goblins could accept a harvest order for '{target.Renderer.name}'.");

        return true;
    }

    private static IHarvestableResource FindResource(GameObject target)
    {
        foreach (var behaviour in target.GetComponentsInParent<MonoBehaviour>())
            if (behaviour is IHarvestableResource resource) return resource;
        return null;
    }

    private static bool HasResourcesTagInParents(Transform target)
    {
        for (var current = target; current != null; current = current.parent)
            if (current.CompareTag("Resources")) return true;
        return false;
    }
}
