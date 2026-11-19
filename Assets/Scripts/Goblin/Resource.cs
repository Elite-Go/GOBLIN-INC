using UnityEngine;

public class Resource : MonoBehaviour
{
    [SerializeField] private ResourceType type;
    [SerializeField] private int amount;
    
    public void Get()
    {
        PlayerResourceManager.Instance.GetResource(type, amount);
    }
}
