using UnityEngine;
using UnityEngine.AI;

public class Goblin : MonoBehaviour
{
    private SpriteRenderer _sr;
    private NavMeshAgent _agent;
    
    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();   
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
    }
}
