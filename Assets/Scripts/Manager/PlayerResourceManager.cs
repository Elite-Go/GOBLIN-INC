using System.Collections.Generic;
using UnityEngine;

public enum ResourceType
{
    Wood
}

public class PlayerResourceManager : Singleton<PlayerResourceManager>
{
    
    public Dictionary<ResourceType, int> Res;

    protected override void Awake()
    {
        Res = new Dictionary<ResourceType, int> { { ResourceType.Wood, 0 } };
    }

    public void GetResource(ResourceType type, int amount)
    {
        var val = Res[type] + amount;
        Res[type] = Mathf.Min(val, 9999999);
    }

    public bool UseResource(ResourceType type, int amount)
    {
        var val = Res[type] - amount;
        
        if (val < 0) return false;
        
        Res[type] = val;
        return true;

    }
}

