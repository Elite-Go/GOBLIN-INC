using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GoblinAgent2D))]
public sealed class GoblinResourceHarvester : MonoBehaviour
{
    [SerializeField]
    [Tooltip("이 고블린의 이동 컴포넌트입니다.")]
    private GoblinAgent2D agent;

    private IHarvestableResource currentResource;

    private void Reset() => CacheComponents();

    private void Awake() => CacheComponents();

    public bool OrderHarvest(IHarvestableResource resource, Vector2 destination)
    {
        CancelCurrentOrder();
        if (resource == null || resource.IsDepleted || agent == null) return false;

        currentResource = resource;
        currentResource.HarvestCompleted += HandleHarvestCompleted;
        agent.DestinationReached += HandleDestinationReached;
        agent.DestinationFailed += HandleDestinationFailed;

        if (agent.SetDestination(destination)) return true;

        CancelCurrentOrder();
        return false;
    }

    public void CancelCurrentOrder()
    {
        if (agent != null)
        {
            agent.DestinationReached -= HandleDestinationReached;
            agent.DestinationFailed -= HandleDestinationFailed;
        }
        if (currentResource != null)
        {
            currentResource.HarvestCompleted -= HandleHarvestCompleted;
            currentResource.RemoveHarvester(this);
            currentResource = null;
        }
    }

    private void HandleDestinationReached()
    {
        if (agent != null) agent.DestinationReached -= HandleDestinationReached;
        if (agent != null) agent.DestinationFailed -= HandleDestinationFailed;
        if (currentResource == null) return;
        if (!currentResource.TryAddHarvester(this)) CancelCurrentOrder();
    }

    private void HandleDestinationFailed()
    {
        CancelCurrentOrder();
    }

    private void HandleHarvestCompleted(IHarvestableResource resource)
    {
        if (currentResource != resource) return;
        currentResource.HarvestCompleted -= HandleHarvestCompleted;
        currentResource = null;
    }

    private void OnDisable() => CancelCurrentOrder();

    private void OnDestroy() => CancelCurrentOrder();

    private void CacheComponents()
    {
        if (agent == null) agent = GetComponent<GoblinAgent2D>();
    }
}
