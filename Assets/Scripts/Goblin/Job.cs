using System;
using UnityEngine;

[Serializable]
public class Job
{
    public Vector2 destination;

    public Job(Vector2 dest)
    {
        destination = dest;        
    }
}
