using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerResource : Singleton<PlayerResource>
{
    public List<Res> resources;

    private void Start()
    {
        resources.ForEach(r => r.Init());
    }

    public void GetResource(Resource res)
    {
        resources.First(r => r.type == res.type).Get(res.amount);
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