using UnityEngine;
using UnityEngine.AI;

public class Goblin : MonoBehaviour
{
    [SerializeField] private Job job;
    
    private SpriteRenderer _sr;
    public SpriteRenderer Sr => _sr;
    
    private NavMeshAgent _agent;
    public NavMeshAgent Agent => _agent;
    
    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();   
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
    }
}
