using UnityEngine;

public class GoblinPoolManager : Singleton<GoblinPoolManager>
{
    [SerializeField] private Pool<Goblin> goblinPool;

    private void Start()
    {
        goblinPool.Initialize(transform);
    }
}
