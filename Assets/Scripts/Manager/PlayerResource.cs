using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerResource : Singleton<PlayerResource>
{
    public List<Res> resources;
    private Dictionary<ResourceType, Res> _resourcesByType;

    private void Start()
    {
        BuildResourceLookup();

        if (resources == null) return;
        for (int i = 0; i < resources.Count; i++)
        {
            resources[i]?.Init();
        }
    }

    public void GetResource(Resource res)
    {
        if (res == null) return;

        if (_resourcesByType == null)
        {
            BuildResourceLookup();
        }

        if (_resourcesByType.TryGetValue(res.type, out Res resource))
        {
            resource.Get(res.amount);
        }
        else
        {
            Debug.LogWarning($"No UI resource entry configured for {res.type}.", this);
        }
    }

    private void BuildResourceLookup()
    {
        _resourcesByType = new Dictionary<ResourceType, Res>();
        if (resources == null) return;

        for (int i = 0; i < resources.Count; i++)
        {
            Res resource = resources[i];
            if (resource != null && !_resourcesByType.ContainsKey(resource.type))
            {
                _resourcesByType.Add(resource.type, resource);
            }
        }
    }
}

[Serializable]
public class Res
{
    public ResourceType type;
    public int amount;
    public TextMeshProUGUI amountText;

    public void Init()
    {
        amountText.text = amount.ToString();
    }
    
    public void Get(int amt)
    {
        var cur = amount;
        var prev = amount + amt;
        ApplyText(cur, prev);
        amount = prev;
    }

    private void ApplyText(int cur, int prev)
    {
        ResourceJumpEffect.Instance.Play(amountText, prev, cur);
    }
}
