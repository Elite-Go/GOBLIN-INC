using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

public class Goblin : MonoBehaviour
{
    private NavMeshAgent _agent;
    private SpriteRenderer _sr;
    private Animator _animator;
    [SerializeField] private Job job;
    [SerializeField] private GameObject hand;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
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
        hand.SetActive(false);
        
        job = j;
        _agent.enabled = true;
        MoveTo(job.destination);
        
        StartCoroutine(MoveRoutine());
    }

    public void MoveTo(Vector2 destination)
    {
        if (NavMesh.SamplePosition(destination, out var hit, 2f, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
            PathVisualizer.Instance.ShowPath(_agent);
        }
        else
        {
            Debug.LogError($"ERROR : 경로 오류, ObjectID : {gameObject.GetInstanceID()}");
        }
    }

    private IEnumerator MoveRoutine()
    {
        _animator.SetBool("IsMove", true);
        
        while (!_agent.isStopped)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                break;
            _sr.flipX = _agent.velocity.x > 0f;
            hand.transform.localScale = new Vector3(_sr.flipX ? -1f : 1f, 1f, 1f);
            yield return null;
        }
        
        _agent.isStopped = true;
        _agent.velocity = Vector2.zero;
        
        _animator.SetBool("IsMove", false);

        StartCoroutine(OnArrival());
    }

    private IEnumerator OnArrival()
    {
        _animator.SetBool("IsAttack1", true);
        hand.SetActive(true);
        
        yield return job.resource.DoCollect(r => Debug.Log($"Get Wood : {r.amount}"));
        
        _animator.SetBool("IsAttack1", false);
        _animator.SetBool("IsMove", true);
        _animator.SetBool("HasResource", true);
        
        _agent.isStopped = false;
        _agent.SetDestination(job.structure.transform.position);
        
        while (!_agent.isStopped)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                break;
            _sr.flipX = _agent.velocity.x > 0f;
            hand.transform.localScale = new Vector3(_sr.flipX ? -1f : 1f, 1f, 1f);
            yield return null;
        }

        PlayerResource.Instance.GetResource(job.resource);
        job.structure.goblins.Release(this);
        
        PathVisualizer.Instance.Clear();
    }
}
