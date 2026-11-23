using System;
using UnityEngine;

[Serializable]
public class Job
{
    public Vector2 destination;
    public Structure structure;
    public Resource resource;

    public Job(Vector2 dest, Structure str, Resource res)
    {
        destination = dest;
        structure = str;
        resource = res;
    }

}
