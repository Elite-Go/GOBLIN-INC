using System;
using System.Collections;
using UnityEngine;

public class Structure : MonoBehaviour
{
    public Pool<Goblin> goblins;

    private void Awake()
    {
        goblins.Initialize(transform);
    }
    
    public IEnumerator ControlAllGoblins(Action<Goblin> action)
    {
        var cnt = goblins.InactiveCount;
        for (int i = 0; i < cnt; i++)
        {
            var g = goblins.Get();
            action(g);

            yield return new WaitForSeconds(0.3f);
        }
    }
}
