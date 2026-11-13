using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class NavMeshPlusPathPlanner2D : MonoBehaviour, IGoblinPathPlanner2D
{
    [SerializeField, Min(0)] [Tooltip("이동에 사용할 네브메쉬 에이전트 유형 번호입니다.")]
    private int agentType;
    [SerializeField, Min(0.1f)] [Tooltip("시작 위치를 네브메쉬 위로 보정하는 최대 거리입니다.")]
    private float startSnap = 2f;
    [SerializeField, Min(0.05f)] [Tooltip("목표 위치를 네브메쉬 위로 보정하는 최대 거리입니다.")]
    private float goalSnap = 0.5f;

    private NavMeshPath navPath;
    private NavMeshQueryFilter queryFilter;
    private bool navReady;

    public bool IsReady => navReady || RefreshNavigationState();

    private void Awake()
    {
        navPath = new NavMeshPath();
        queryFilter.agentTypeID = agentType;
        queryFilter.areaMask = NavMesh.AllAreas;
        RefreshNavigationState();
    }

    private bool RefreshNavigationState()
    {
        navReady = NavMesh.CalculateTriangulation().vertices.Length > 0;
        return navReady;
    }

    public bool TryFindPath(Vector2 start, Vector2 destination, List<Vector2> result)
    {
        result.Clear();
        if (!IsReady) return false;
        queryFilter.agentTypeID = agentType;
        queryFilter.areaMask = NavMesh.AllAreas;
        if (navPath == null) navPath = new NavMeshPath();

        var start3 = new Vector3(start.x, start.y, 0f);
        var dest3 = new Vector3(destination.x, destination.y, 0f);
        if (!NavMesh.SamplePosition(start3, out var startHit, startSnap, queryFilter) ||
            !NavMesh.SamplePosition(dest3, out var destHit, goalSnap, queryFilter)) return false;

        navPath.ClearCorners();
        if (!NavMesh.CalculatePath(startHit.position, destHit.position, queryFilter, navPath) ||
            navPath.status != NavMeshPathStatus.PathComplete) return false;

        var corners = navPath.corners;
        for (var i = 0; i < corners.Length; i++)
            result.Add(new Vector2(corners[i].x, corners[i].y));
        return result.Count > 0;
    }
}
