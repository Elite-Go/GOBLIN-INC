using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Executes a path using smooth, physics-backed movement for one goblin.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class GoblinAgent2D : MonoBehaviour, IGoblinUnitAgent
{
    [SerializeField, Min(0.1f)] [Tooltip("고블린의 최대 이동 속도입니다.")]
    [FormerlySerializedAs("moveSpeed")]
    private float speed = 3.5f;
    [SerializeField, Min(0.1f)] [Tooltip("고블린의 가속도입니다.")]
    [FormerlySerializedAs("acceleration")]
    private float accel = 12f;
    [SerializeField, Min(0.01f)] [Tooltip("중간 경로 지점 도달 판정 거리입니다.")]
    [FormerlySerializedAs("waypointTolerance")]
    private float wayTol = 0.04f;
    [SerializeField, Min(0f)] [Tooltip("목적지 도착 및 정지 거리입니다.")]
    [FormerlySerializedAs("arrivalStoppingDistance")]
    private float stopDist = 0.12f;
    [SerializeField, Min(0.1f)] [Tooltip("이동 정체 후 경로를 다시 계산하기까지의 시간입니다.")]
    [FormerlySerializedAs("stalledReplanDelay")]
    private float stallDelay = 0.45f;
    [SerializeField, Min(0.005f)] [Tooltip("이동 진행으로 판정할 최소 거리입니다.")]
    [FormerlySerializedAs("progressThreshold")]
    private float progStep = 0.02f;
    [SerializeField, Min(0f)] [Tooltip("정체 시 경로 재계산마다 추가할 장애물 여유 거리입니다.")]
    [FormerlySerializedAs("replanClearanceStep")]
    private float clearStep = 0.1f;
    [SerializeField, Min(0f)] [Tooltip("경로 재계산에 추가할 장애물 여유 거리의 최댓값입니다.")]
    [FormerlySerializedAs("maxAdditionalClearance")]
    private float maxClear = 0.3f;
    [SerializeField] [Tooltip("이 고블린의 Rigidbody2D입니다.")]
    [FormerlySerializedAs("body")]
    private Rigidbody2D rb;
    [SerializeField] [Tooltip("이동 충돌 판정에 사용하는 원형 콜라이더입니다.")]
    [FormerlySerializedAs("bodyCollider")]
    private CircleCollider2D bodyCol;
    [SerializeField] [Tooltip("현재 계산된 이동 경로입니다.")]
    [FormerlySerializedAs("path")]
    private List<Vector2> route = new List<Vector2>();
    [SerializeField] [Tooltip("현재 이동 목적지입니다.")]
    [FormerlySerializedAs("destination")]
    private Vector2 dest;
    [SerializeField] [Tooltip("현재 이동 목적지가 있는지 표시합니다.")]
    [FormerlySerializedAs("hasDestination")]
    private bool hasDest;

    private int nextWaypoint;
    private float currentSpeed;
    private float stalledDuration;
    private float additionalClearance;
    private Vector2 lastProgressPosition;
    private bool pathPending;
    private readonly List<Vector2> replanBuffer = new List<Vector2>();
    private IGoblinPathPlanner2D pathfinder;
    private RigidbodyConstraints2D movementConstraints;

    public float BodyRadius => bodyCol != null
        ? bodyCol.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y)
        : 0.38f;
    public Vector2 ColliderWorldOffset => bodyCol != null
        ? bodyCol.transform.TransformVector(bodyCol.offset)
        : Vector2.zero;

    public bool HasDestination => hasDest;
    public event Action DestinationReached;
    public event Action DestinationFailed;
    public IReadOnlyList<Vector2> Path => route;
    public Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;

    private void Reset() => CacheComponents();

    private void Awake()
    {
        CacheComponents();
        pathfinder = FindPathfinder();
        if (rb != null) movementConstraints = rb.constraints;
    }

    private void CacheComponents()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCol == null) bodyCol = GetComponent<CircleCollider2D>();
    }

    public bool SetDestination(Vector2 worldPosition)
    {
        if (rb == null) CacheComponents();
        if (pathfinder == null || !pathfinder.IsReady) pathfinder = FindPathfinder();

        if (rb == null)
        {
            ClearDestination();
            return false;
        }

        if (Vector2.Distance(rb.position, worldPosition) <= stopDist)
        {
            route.Clear();
            dest = worldPosition;
            StopAtDestination();
            DestinationReached?.Invoke();
            return true;
        }

        dest = worldPosition;
        nextWaypoint = 0;
        currentSpeed = 0f;
        stalledDuration = 0f;
        additionalClearance = 0f;
        lastProgressPosition = rb.position;
        rb.constraints = movementConstraints;
        rb.WakeUp();
        rb.linearVelocity = Vector2.zero;

        if (pathfinder == null || !pathfinder.IsReady)
        {
            route.Clear();
            pathPending = true;
            hasDest = true;
            return true;
        }

        if (!TryBuildPath(rb.position, worldPosition, BodyRadius, route) || route.Count == 0)
        {
            ClearDestination();
            return false;
        }

        pathPending = false;
        dest = route[route.Count - 1];
        hasDest = true;
        return true;
    }

    private static IGoblinPathPlanner2D FindPathfinder()
    {
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var behaviour in behaviours)
            if (behaviour is IGoblinPathPlanner2D candidate && candidate.IsReady) return candidate;
        return null;
    }

    private void FixedUpdate()
    {
        if (!hasDest || rb == null) return;

        if (pathPending)
        {
            if (pathfinder == null || !pathfinder.IsReady) pathfinder = FindPathfinder();
            if (pathfinder == null || !pathfinder.IsReady) return;
            if (!TryBuildPath(rb.position, dest, BodyRadius, route) || route.Count == 0)
            {
                ClearDestination();
                DestinationFailed?.Invoke();
                return;
            }
            pathPending = false;
            nextWaypoint = 0;
            dest = route[route.Count - 1];
        }

        TrackProgress();
        if (stalledDuration >= stallDelay &&
            Vector2.Distance(rb.position, dest) > wayTol * 3f)
            TryReplanWithMoreClearance();

        while (nextWaypoint < route.Count)
        {
            var isFinalWaypoint = nextWaypoint == route.Count - 1;
            var tolerance = isFinalWaypoint ? stopDist : wayTol;
            if (Vector2.Distance(rb.position, route[nextWaypoint]) > tolerance) break;
            nextWaypoint++;
        }

        if (nextWaypoint >= route.Count)
        {
            StopAtDestination();
            DestinationReached?.Invoke();
            return;
        }

        var target = route[nextWaypoint];
        var remainingDistance = Vector2.Distance(rb.position, target);
        var desiredSpeed = Mathf.Min(speed, Mathf.Sqrt(2f * accel * remainingDistance));
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, accel * Time.fixedDeltaTime);
        rb.MovePosition(Vector2.MoveTowards(rb.position, target, currentSpeed * Time.fixedDeltaTime));
    }

    private void StopAtDestination()
    {
        hasDest = false;
        pathPending = false;
        currentSpeed = 0f;
        stalledDuration = 0f;
        route.Clear();
        nextWaypoint = 0;
        if (rb == null) return;
        rb.constraints = movementConstraints |
                           RigidbodyConstraints2D.FreezePositionX |
                           RigidbodyConstraints2D.FreezePositionY;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.Sleep();
    }

    private bool TryBuildPath(Vector2 start, Vector2 target, float clearance, List<Vector2> result)
    {
        var colliderOffset = ColliderWorldOffset;
        if (!pathfinder.TryFindPath(start + colliderOffset, target + colliderOffset, clearance, result))
            return false;
        for (var i = 0; i < result.Count; i++) result[i] -= colliderOffset;
        return result.Count > 0;
    }

    private void ClearDestination()
    {
        StopAtDestination();
    }

    private void TrackProgress()
    {
        if (Vector2.Distance(rb.position, lastProgressPosition) >= progStep)
        {
            lastProgressPosition = rb.position;
            stalledDuration = 0f;
            return;
        }

        stalledDuration += Time.fixedDeltaTime;
    }

    private void TryReplanWithMoreClearance()
    {
        if (pathfinder == null || clearStep <= 0f) return;

        additionalClearance = Mathf.Min(additionalClearance + clearStep, maxClear);
        var found = TryBuildPath(rb.position, dest,
            BodyRadius + additionalClearance, replanBuffer);
        stalledDuration = 0f;
        lastProgressPosition = rb.position;

        if (!found || replanBuffer.Count == 0) return;
        currentSpeed = 0f;
        route.Clear();
        route.AddRange(replanBuffer);
        nextWaypoint = 0;
    }
}
