using System;
using System.Collections;
using UnityEngine;

public enum ResourceType
{
    Wood,
    Stone
}

public class Resource : MonoBehaviour
{
    private SpriteRenderer _renderer;
    
    public ResourceType type;
    public int amount;
    
    public float duration;
    public SpriteRenderer sr => _renderer;

    
    public IEnumerator DoCollect(Action<Resource> endAct)
    {
        var timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        endAct(this);
    }
}
