using System;
using System.Collections;
using UnityEngine;

public class Resource : MonoBehaviour
{
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
