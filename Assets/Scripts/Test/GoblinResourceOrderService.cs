using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GoblinResourceOrderService : MonoBehaviour
{
    private readonly List<Worker> workers = new List<Worker>();
    private readonly List<Vector2> slots = new List<Vector2>();

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

        GatherWorkers(goblins);
        if (workers.Count == 0) return true;

        var slotCount = Mathf.Min(workers.Count, resource.MaxConcurrentWorkers,
            GetOrbitCapacity(squadMover.TargetStandOffDistance));
        BuildSlots(pivot, squadMover.TargetStandOffDistance, slotCount);
        AssignNearestWorkers(resource, slotCount);
        return true;
    }

    private void GatherWorkers(IReadOnlyList<GameObject> goblins)
    {
        workers.Clear();
        for (var i = 0; i < goblins.Count; i++)
        {
            var goblin = goblins[i];
            if (goblin == null || !goblin.CompareTag("Goblin")) continue;
            var harvester = goblin.GetComponent<GoblinResourceHarvester>();
            var agent = goblin.GetComponent<GoblinAgent2D>();
            if (harvester != null && agent != null) workers.Add(new Worker(goblin, harvester, agent));
        }
    }

    private int GetOrbitCapacity(float orbitRadius)
    {
        var radius = Mathf.Max(0f, orbitRadius);
        var bodyRadius = 0f;
        for (var i = 0; i < workers.Count; i++)
            bodyRadius = Mathf.Max(bodyRadius, workers[i].Agent.BodyRadius);

        var diameter = bodyRadius * 2f;
        if (radius <= 0f || diameter <= 0f) return 1;
        var sine = diameter / (2f * radius);
        if (sine >= 1f) return 1;
        return Mathf.Max(1, Mathf.FloorToInt(Mathf.PI / Mathf.Asin(sine)));
    }

    private void BuildSlots(Vector2 pivot, float radius, int count)
    {
        slots.Clear();
        for (var i = 0; i < count; i++)
        {
            var angle = Mathf.PI * 0.5f + Mathf.PI * 2f * i / count;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            slots.Add(pivot + direction * Mathf.Max(0f, radius));
        }
    }

    private void AssignNearestWorkers(IHarvestableResource resource, int count)
    {
        var availableSlots = new List<Vector2>(slots);
        var availableWorkers = new List<Worker>(workers);
        var assignedWorkers = new HashSet<GameObject>();
        var assignedCount = 0;

        while (assignedCount < count && availableSlots.Count > 0 && availableWorkers.Count > 0)
        {
            var bestWorker = -1;
            var bestSlot = -1;
            var bestDistance = float.PositiveInfinity;
            for (var workerIndex = 0; workerIndex < availableWorkers.Count; workerIndex++)
            for (var slotIndex = 0; slotIndex < availableSlots.Count; slotIndex++)
            {
                var distance = (availableWorkers[workerIndex].Agent.Position - availableSlots[slotIndex]).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestWorker = workerIndex;
                bestSlot = slotIndex;
            }

            if (bestWorker < 0 || bestSlot < 0) break;
            var worker = availableWorkers[bestWorker];
            if (worker.Harvester.OrderHarvest(resource, availableSlots[bestSlot]))
            {
                assignedCount++;
                assignedWorkers.Add(worker.Goblin);
            }
            availableWorkers.RemoveAt(bestWorker);
            availableSlots.RemoveAt(bestSlot);
        }

        for (var i = 0; i < workers.Count; i++)
        {
            var worker = workers[i];
            if (assignedWorkers.Contains(worker.Goblin)) continue;
            worker.Harvester.CancelCurrentOrder();
            worker.Agent.SetDestination(worker.Agent.Position);
        }

        if (assignedCount == 0)
            Debug.LogWarning("No selected goblins could accept a harvest order.");
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

    private readonly struct Worker
    {
        public readonly GameObject Goblin;
        public readonly GoblinResourceHarvester Harvester;
        public readonly GoblinAgent2D Agent;

        public Worker(GameObject goblin, GoblinResourceHarvester harvester, GoblinAgent2D agent)
        {
            Goblin = goblin;
            Harvester = harvester;
            Agent = agent;
        }
    }
}
