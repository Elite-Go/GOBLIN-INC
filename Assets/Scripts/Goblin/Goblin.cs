using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Goblin : MonoBehaviour
{
    private NavMeshAgent _agent;
    //private NavMeshObstacle _obs;
    private SpriteRenderer _sr;
    private Animator _animator;
    [SerializeField] private Job job;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        //_obs = GetComponent<NavMeshObstacle>();
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        
        _sr = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
    }

    private void OnDisable()
    {
        job = null;
    }

    public void GiveJob(Job j)
    {
        job = j;
        _agent.enabled = true;
        //_obs.enabled = false;
        MoveTo(job.destination);
        
        StartCoroutine(AnimRoutine());
    }

    public void MoveTo(Vector2 destination)
    {
        if (NavMesh.SamplePosition(destination, out var hit, 2f, NavMesh.AllAreas))
        {
            Debug.Log("Locate Position : " + hit.position);
            _agent.SetDestination(hit.position);
        }
        else
        {
            Debug.LogError($"ERROR : 경로 오류, ObjectID : {gameObject.GetInstanceID()}");
        }
    }

    private IEnumerator AnimRoutine()
    {
        _animator.SetBool("IsMove", true);
        
        while (!_agent.isStopped)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                break;            
            _sr.flipX = _agent.velocity.x > 0f;
            yield return null;
        }
        
        _agent.isStopped = true;
        _agent.velocity = Vector2.zero;
        _agent.enabled = false;
        
        //_obs.enabled = true;
        _animator.SetBool("IsMove", false);
    }
}
