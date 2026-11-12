using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class GoblinSquadMover : MonoBehaviour
{
    [SerializeField, Min(0.2f)]
    [Tooltip("대형에서 고블린 자리 사이의 간격입니다.")]
    [FormerlySerializedAs("formationSpacing")]
    private float slotGap = 0.78f;
    [SerializeField, Min(0f)] [Tooltip("자원 피봇에서 고블린 채취 지점까지의 거리입니다.")]
    [FormerlySerializedAs("targetStandOff")]
    private float standOff = 0.25f;
    [SerializeReference] [Tooltip("고블린 이동 대형을 계산하는 패턴입니다.")]
    [FormerlySerializedAs("formationPattern")]
    private IGoblinFormationPattern pattern = new RadialGoblinFormationPattern();

    private readonly List<IGoblinUnitAgent> agentBuffer = new List<IGoblinUnitAgent>();
    private readonly List<GameObject> unitBuffer = new List<GameObject>();

    public float TargetStandOffDistance => standOff;

    private void Reset() => EnsureFormationPattern();
    private void Awake() => EnsureFormationPattern();

    public void Move(IReadOnlyList<GameObject> goblins, Vector2 requestedDestination)
    {
        if (goblins == null || goblins.Count == 0) return;
        GatherAgents(goblins);
        MoveAgents(requestedDestination);
    }

    public void MoveToTarget(IReadOnlyList<GameObject> goblins, SpritePointerTarget target)
    {
        if (goblins == null || goblins.Count == 0 || target.Renderer == null) return;
        GatherAgents(goblins);
        if (agentBuffer.Count == 0) return;

        MoveAgentsToTarget(target);
    }

    private void GatherAgents(IReadOnlyList<GameObject> goblins)
    {
        agentBuffer.Clear();
        unitBuffer.Clear();

        for (var i = 0; i < goblins.Count; i++)
        {
            var goblin = goblins[i];
            if (goblin == null || !goblin.CompareTag("Goblin")) continue;

            var agent = FindUnitAgent(goblin);
            if (agent == null)
            {
                Debug.LogWarning($"Selected goblin '{goblin.name}' has no movement agent.");
                continue;
            }

            agentBuffer.Add(agent);
            unitBuffer.Add(goblin);
        }
    }

    private void MoveAgents(Vector2 destination)
    {
        EnsureFormationPattern();
        for (var i = 0; i < agentBuffer.Count; i++)
        {
            var agent = agentBuffer[i];
            var goblin = unitBuffer[i];
            var slot = destination + pattern.GetOffset(i, slotGap);
            CancelResourceOrder(goblin);
            if (!agent.SetDestination(slot))
                Debug.LogWarning($"No walkable route found for '{goblin.name}' near {slot}.");
        }
    }

    private void MoveAgentsToTarget(SpritePointerTarget target)
    {
        for (var i = 0; i < agentBuffer.Count; i++)
        {
            var agent = agentBuffer[i];
            var destination = target.GetStandOffPosition(agent.Position, agent.BodyRadius, standOff) -
                              agent.ColliderWorldOffset;
            CancelResourceOrder(unitBuffer[i]);
            if (!agent.SetDestination(destination))
                Debug.LogWarning($"No walkable route found for '{unitBuffer[i].name}' near {destination}.");
        }
    }

    private void EnsureFormationPattern()
    {
        if (pattern == null) pattern = new RadialGoblinFormationPattern();
    }

    private static IGoblinUnitAgent FindUnitAgent(GameObject goblin)
    {
        foreach (var behaviour in goblin.GetComponents<MonoBehaviour>())
            if (behaviour is IGoblinUnitAgent agent) return agent;
        return null;
    }

    private static void CancelResourceOrder(GameObject goblin)
    {
        var harvester = goblin.GetComponent<GoblinResourceHarvester>();
        if (harvester != null) harvester.CancelCurrentOrder();
    }
}
