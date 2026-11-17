using System;
using UnityEngine;

public class GoblinJobScheduler : Singleton<GoblinJobScheduler>
{
    [SerializeField] private Architecture architecture;
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            architecture = SelectManager.Instance.SelectPoint(Input.mousePosition).GetComponent<Architecture>();
            if (!architecture) return;
            
            SelectManager.Instance.ControlGoblins(MoveToArch);
        }
    }

    private void MoveToArch(Goblin g)
    {
        g.Agent.SetDestination(architecture.GetArrivalPosition());
    }

}

[Serializable]
public class Job
{
    [Header("Job Info")]
    public float duration;

    [Header("Comp Info")] 
    public string resName;
    public string resAmount;
}