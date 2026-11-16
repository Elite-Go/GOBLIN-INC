using System;
using UnityEngine;

public class GoblinJobScheduler : Singleton<GoblinJobScheduler>
{
    [SerializeField] private GameObject architecture;
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
            architecture = SelectManager.Instance.SelectPoint(Input.mousePosition);
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