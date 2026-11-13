using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(GoblinAgent2D), typeof(GoblinResourceHarvester))]
public sealed class GoblinAnimationPresenter : MonoBehaviour
{
    [SerializeField] [Tooltip("걷기 상태를 제어하는 Animator bool 파라미터 이름입니다.")]
    private string moveParam = "IsMove";
    [SerializeField] [Tooltip("자원 채취 상태를 제어하는 Animator bool 파라미터 이름입니다.")]
    private string attackParam = "IsAttack";
    [SerializeField] [Tooltip("애니메이션을 재생할 Animator입니다.")]
    private Animator animator;
    [SerializeField] [Tooltip("이 고블린의 이동 상태를 제공하는 컴포넌트입니다.")]
    private GoblinAgent2D agent;
    [SerializeField] [Tooltip("이 고블린의 채취 상태를 제공하는 컴포넌트입니다.")]
    private GoblinResourceHarvester harvester;

    private int moveHash;
    private int attackHash;
    private bool hasMoveParam;
    private bool hasAttackParam;

    private void Reset() => CacheComponents();

    private void Awake()
    {
        CacheComponents();
        moveHash = Animator.StringToHash(moveParam);
        attackHash = Animator.StringToHash(attackParam);
        if (animator == null) return;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.type != AnimatorControllerParameterType.Bool) continue;
            if (parameter.nameHash == moveHash) hasMoveParam = true;
            if (parameter.nameHash == attackHash) hasAttackParam = true;
        }
    }

    private void Update()
    {
        if (animator == null || agent == null || harvester == null) return;
        var attacking = harvester.IsHarvesting;
        if (hasMoveParam) animator.SetBool(moveHash, agent.HasDestination && !attacking);
        if (hasAttackParam) animator.SetBool(attackHash, attacking);
    }

    private void OnDisable()
    {
        if (animator == null) return;
        if (hasMoveParam) animator.SetBool(moveHash, false);
        if (hasAttackParam) animator.SetBool(attackHash, false);
    }

    private void CacheComponents()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<GoblinAgent2D>();
        if (harvester == null) harvester = GetComponent<GoblinResourceHarvester>();
    }
}
