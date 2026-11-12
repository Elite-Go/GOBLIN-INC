using System;

public interface IHarvestableResource
{
    bool IsDepleted { get; }
    bool IsHarvesting { get; }
    float Progress { get; }
    event Action<IHarvestableResource> HarvestStarted;
    event Action<IHarvestableResource, float> ProgressChanged;
    event Action<IHarvestableResource> HarvestStopped;
    event Action<IHarvestableResource> HarvestCompleted;
    bool TryAddHarvester(GoblinResourceHarvester harvester);
    void RemoveHarvester(GoblinResourceHarvester harvester);
}
