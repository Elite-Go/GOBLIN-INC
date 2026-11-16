using System.Collections.Generic;
using UnityEngine;

public class PlayerResourceManager : Singleton<PlayerResourceManager>
{
    public Dictionary<string, int> Res;

    protected override void Awake()
    {
        Res = new Dictionary<string, int> { { "Wood", 0 } };
    }
}

