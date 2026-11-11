using System.Collections.Generic;
using UnityEngine;

/// <summary>Executes a path using smooth, physics-backed movement for one goblin.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class GoblinAgent2D : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
    [SerializeField, Min(0.1f)] private float acceleration = 12f;
    [SerializeField, Min(0.01f)] private float waypointTolerance = 0.04f;
    [SerializeField, Min(0.1f)] private float stalledReplanDelay = 0.45f;
    [SerializeField, Min(0.005f)] private float progressThreshold = 0.02f;
    [SerializeField, Min(0f)] private float replanClearanceStep = 0.1f;
    [SerializeField, Min(0f)] private float maxAdditionalClearance = 0.3f;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private CircleCollider2D bodyCollider;
    [SerializeField] private List<Vector2> path = new List<Vector2>();
    [SerializeField] private Vector2 destination;
    [SerializeField] private bool hasDestination;

    private int nextWaypoint;
    private float currentSpeed;
    private float stalledDuration;
    private float additionalClearance;
    private Vector2 lastProgressPosition;
    private readonly List<Vector2> replanBuffer = new List<Vector2>();
    private GoblinPathfinder2D pathfinder;

    public float BodyRadius => bodyCollider != null
        ? bodyCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y)
        : 0.38f;

    public bool HasDestination => hasDestination;
    public IReadOnlyList<Vector2> Path => path;

    private void Reset() => CacheComponents();

    private void Awake()
    {
        CacheComponents();
        pathfinder = FindPathfinder();
    }

    private void CacheComponents()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<CircleCollider2D>();
    }

    public bool SetDestination(Vector2 worldPosition)
    {
        if (body == null) CacheComponents();
        if (pathfinder == null || !pathfinder.IsReady) pathfinder = FindPathfinder();

        if (body == null || pathfinder == null ||
            !pathfinder.TryFindPath(body.position, worldPosition, BodyRadius, path))
        {
            hasDestination = false;
            path.Clear();
            return false;
        }

        destination = worldPosition;
        nextWaypoint = 0;
        currentSpeed = 0f;
        stalledDuration = 0f;
        additionalClearance = 0f;
        lastProgressPosition = body.position;
        hasDestination = path.Count > 0;
        return hasDestination || Vector2.Distance(body.position, worldPosition) <= waypointTolerance;
    }

    private static GoblinPathfinder2D FindPathfinder()
    {
        var pathfinders = FindObjectsByType<GoblinPathfinder2D>(FindObjectsSortMode.None);
        foreach (var candidate in pathfinders)
            if (candidate != null && candidate.IsReady) return candidate;
        return null;
    }

    private void FixedUpdate()
    {
        if (!hasDestination || body == null) return;

        TrackProgress();
        if (stalledDuration >= stalledReplanDelay &&
            Vector2.Distance(body.position, destination) > waypointTolerance * 3f)
            TryReplanWithMoreClearance();

        while (nextWaypoint < path.Count &&
               Vector2.Distance(body.position, path[nextWaypoint]) <= waypointTolerance)
            nextWaypoint++;

        if (nextWaypoint >= path.Count)
        {
            hasDestination = false;
            currentSpeed = 0f;
            return;
        }

        var target = path[nextWaypoint];
        var remainingDistance = Vector2.Distance(body.position, target);
        var desiredSpeed = Mathf.Min(moveSpeed, Mathf.Sqrt(2f * acceleration * remainingDistance));
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * Time.fixedDeltaTime);
        body.MovePosition(Vector2.MoveTowards(body.position, target, currentSpeed * Time.fixedDeltaTime));
    }

    private void TrackProgress()
    {
        if (Vector2.Distance(body.position, lastProgressPosition) >= progressThreshold)
        {
            lastProgressPosition = body.position;
            stalledDuration = 0f;
            return;
        }

        stalledDuration += Time.fixedDeltaTime;
    }

    private void TryReplanWithMoreClearance()
    {
        if (pathfinder == null || replanClearanceStep <= 0f) return;

        additionalClearance = Mathf.Min(additionalClearance + replanClearanceStep, maxAdditionalClearance);
        var found = pathfinder.TryFindPath(body.position, destination,
            BodyRadius + additionalClearance, replanBuffer);
        stalledDuration = 0f;
        lastProgressPosition = body.position;

        if (!found || replanBuffer.Count == 0) return;
        currentSpeed = 0f;
        path.Clear();
        path.AddRange(replanBuffer);
        nextWaypoint = 0;
        destination = path[path.Count - 1];
    }
}
