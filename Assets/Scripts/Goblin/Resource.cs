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
    public ResourceType type;
    public int amount;
    
    public float duration;

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
