using System;
using UnityEngine;

[Serializable]
public class Job
{
    public Vector2 destination;
    public Resource resource;

    public Job(Vector2 dest, Resource res)
    {
        destination = dest;
        resource = res;
    }

}
