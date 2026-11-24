using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    private Camera _cam;

    protected override void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void Update()
    {
        
    }

    public static void ZoomIn(Transform target)
    {
        var origin= target.position;
        origin.z = Instance._cam.transform.position.z;
        Instance._cam.transform.position = origin;
    }
}
