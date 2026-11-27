using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Goblin : MonoBehaviour
{
    private static readonly int IsMoveId = Animator.StringToHash("IsMove");
    private static readonly int IsAttack1Id = Animator.StringToHash("IsAttack1");
    private static readonly int HasResourceId = Animator.StringToHash("HasResource");

    private NavMeshAgent _agent;
    private SpriteRenderer _sr;
    private Animator _animator;
    [SerializeField] private Job job;
    [SerializeField] private GameObject hand;
    private Coroutine _jobRoutine;
    private bool _hasValidPath;

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
        _jobRoutine = null;
        job = null;
    }

    public void GiveJob(Job j)
    {
        if (j == null || j.resource == null || j.structure == null) return;

        if (_jobRoutine != null)
        {
            StopCoroutine(_jobRoutine);
            _jobRoutine = null;
        }

        hand.SetActive(false);
        _animator.SetBool(IsAttack1Id, false);
        _animator.SetBool(HasResourceId, false);
        job = j;
        _agent.enabled = true;

        if (!MoveTo(job.destination))
        {
            job = null;
            return;
        }

        _jobRoutine = StartCoroutine(RunJob());
    }

    public bool MoveTo(Vector2 destination)
    {
        if (NavMesh.SamplePosition(destination, out var hit, 2f, NavMesh.AllAreas))
        {
            _agent.isStopped = false;
            _agent.SetDestination(hit.position);
            _hasValidPath = true;
            PathVisualizer.Instance.ShowPath(_agent);
            return true;
        }

        Debug.LogError($"ERROR : 경로 오류, ObjectID : {gameObject.GetInstanceID()}");
        return false;
    }

    private IEnumerator RunJob()
    {
        _animator.SetBool(IsMoveId, true);
        yield return MoveUntilDestination();
        if (job == null) yield break;
        if (!_hasValidPath)
        {
            ReturnToPool();
            yield break;
        }

        _animator.SetBool(IsMoveId, false);
        _animator.SetBool(IsAttack1Id, true);
        hand.SetActive(true);

        yield return job.resource.DoCollect(null);
        if (job == null) yield break;

        _animator.SetBool(IsAttack1Id, false);
        _animator.SetBool(IsMoveId, true);
        _animator.SetBool(HasResourceId, true);

        _agent.isStopped = false;
        _agent.SetDestination(job.structure.transform.position);
        yield return MoveUntilDestination();

        if (job == null) yield break;
        if (!_hasValidPath)
        {
            ReturnToPool();
            yield break;
        }

        PlayerResource.Instance.GetResource(job.resource);
        ReturnToPool();
    }

    private IEnumerator MoveUntilDestination()
    {
        while (job != null && _agent.pathPending)
        {
            yield return null;
        }

        if (job != null && _agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            _hasValidPath = false;
            yield break;
        }

        while (job != null && _agent.remainingDistance > _agent.stoppingDistance)
        {
            UpdateFacing();
            yield return null;
        }

        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }
    }

    private void UpdateFacing()
    {
        _sr.flipX = _agent.velocity.x > 0f;
        hand.transform.localScale = new Vector3(_sr.flipX ? -1f : 1f, 1f, 1f);
    }

    private void ReturnToPool()
    {
        Structure assignedStructure = job.structure;
        _jobRoutine = null;
        PathVisualizer.Instance.Clear();
        assignedStructure.goblins.Release(this);
    }
}
