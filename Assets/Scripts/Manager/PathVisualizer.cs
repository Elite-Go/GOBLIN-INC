using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PathVisualizer : Singleton<PathVisualizer>
{
    [Header("Line 설정")]
    public LineRenderer line;
    [SerializeField] private float heightOffset = 0.05f;

    public bool IsShowed { get; private set; } = false;

    private Coroutine _drawRoutine;

    protected override void Awake()
    {
        //base.Awake();
        if (line != null)
        {
            line.useWorldSpace = true;
            line.positionCount = 0;
        }
    }

    public void ShowPath(NavMeshAgent agent)
    {
        if (agent == null || line == null) return;

        if (_drawRoutine != null)
            StopCoroutine(_drawRoutine);

        _drawRoutine = StartCoroutine(DrawPathWhenReady(agent));
    }

    public void ShowPath(NavMeshPath path)
    {
        if (line == null || path == null) return;
        if (path.status == NavMeshPathStatus.PathInvalid) return;

        ApplyCorners(path.corners);
    }

    private IEnumerator DrawPathWhenReady(NavMeshAgent agent)
    {
        if (!agent.isOnNavMesh)
            yield break;

        int safetyFrames = 0;
        while (agent.pathPending && safetyFrames < 60)
        {
            safetyFrames++;
            yield return null;
        }

        if (agent.hasPath && agent.path.status != NavMeshPathStatus.PathInvalid)
        {
            ApplyCorners(agent.path.corners);
        }

        _drawRoutine = null;
    }

    private void ApplyCorners(Vector3[] corners)
    {
        if (corners == null || corners.Length < 2)
        {
            Clear();
            return;
        }

        line.positionCount = corners.Length;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 pos = corners[i];
            pos.y += heightOffset;
            line.SetPosition(i, pos);
        }

        IsShowed = true;
    }

    public void Clear()
    {
        if (_drawRoutine != null)
        {
            StopCoroutine(_drawRoutine);
            _drawRoutine = null;
        }

        if (line != null)
        {
            line.positionCount = 0;
        }

        IsShowed = false;
    }
}