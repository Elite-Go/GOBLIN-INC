using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Architecture : MonoBehaviour
{
    [SerializeField] private List<Job> jobs;
    [SerializeField] private Vector2 arrivalPos;

    public Vector2 ArrivalPos 
    { 
        get => arrivalPos; 
        set => arrivalPos = value; 
    }

    public Vector2 GetArrivalPosition()
    {
        if (arrivalPos != Vector2.zero) 
            return (Vector2)transform.position + arrivalPos;
        else if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
            return hit.position;
        else
            return transform.position;
    }
}