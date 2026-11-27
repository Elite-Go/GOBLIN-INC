using System;
using System.Collections;
using UnityEngine;

public class Structure : MonoBehaviour
{
    private SpriteRenderer _renderer;
    public Pool<Goblin> goblins;
    
    public SpriteRenderer sr => _renderer;

    private void Awake()
    {
        goblins.Initialize(transform);
        _renderer = GetComponent<SpriteRenderer>();
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
