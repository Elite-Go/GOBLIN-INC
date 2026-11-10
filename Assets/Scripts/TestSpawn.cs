using GoblinGame.Goblins;
using UnityEngine;

public class TestSpawn : MonoBehaviour
{
    public GoblinSpawner spawner;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (spawner.TryCreateAndSpawn(
                    "goblin.common",
                    Vector3.zero,
                    out GoblinActor actor,
                    out string error))
            {
                Debug.Log($"소환 성공: {actor.Instance.InstanceId}");
            }
            else
            {
                Debug.LogError(error);
            }
        }
    }
}
