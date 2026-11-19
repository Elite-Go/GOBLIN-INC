using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GoblinJobScheduler : Singleton<GoblinJobScheduler>
{
    [SerializeField] private SpriteRenderer sr;
    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (sr)
            {
                MaterialSwitcher.Instance.Change(sr, true);
                sr = null;
            }
            var obj = SelectManager.Instance.SelectPoint(Input.mousePosition);
            sr = obj.GetComponent<SpriteRenderer>();
            MaterialSwitcher.Instance.Change(sr, false);
            
            if (obj.TryGetComponent(out Architecture arch))
            {
                SelectManager.Instance.ControlGoblins(g => MoveToArch(g, arch));
            }
            if (obj.TryGetComponent(out Resource res))
            {
                SelectManager.Instance.ControlGoblins(g => MoveToRes(g, res));
            }
            
        }
    }

    private void MoveToArch(Goblin g, Architecture arch)
    {
        g.Agent.SetDestination(arch.GetArrivalPosition());
    }
    
    private void MoveToRes(Goblin g, Resource res)
    {
        g.Agent.SetDestination(res.transform.position);
    }
}

[Serializable]
public class Job
{
    public Vector2 arrivalPosition;
    public Action jobAction;
    public float duration;
    
    private NavMeshAgent _agent;

    public Job(NavMeshAgent agent)
    {
        _agent = agent;
    }
    
    public void Start()
    {
        _agent.SetDestination(arrivalPosition);

        CoroutineRunner.StartRoutine(JobRoutine());
    }

    private IEnumerator JobRoutine()
    {
        yield return new WaitWhile(() => _agent.isStopped);
        yield return new WaitForSeconds(duration);
        jobAction.Invoke();
    }
}