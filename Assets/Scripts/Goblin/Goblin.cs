using UnityEngine;
using UnityEngine.AI;

public class Goblin : MonoBehaviour
{
    [SerializeField] private Job job;
    
    private SpriteRenderer _sr;
    public SpriteRenderer Sr => _sr;
    
    private Animator _anim;
    public Animator Anim => _anim;
    
    private NavMeshAgent _agent;
    public NavMeshAgent Agent => _agent;

    [SerializeField] private bool isMoving;
     
    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();   
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
    }

    private void FixedUpdate()
    {
        isMoving = !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.01f;

        Flip();
        _anim.SetBool("IsMove", isMoving);
    }

    private void Flip()
    {
        if (isMoving && _agent.velocity.x != 0)
            _sr.flipX = _agent.velocity.x > 0;
    }
}