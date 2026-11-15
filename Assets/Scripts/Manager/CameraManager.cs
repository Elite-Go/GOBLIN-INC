using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraManager : MonoBehaviour
{
    private Camera _camera;
    private PixelPerfectCamera _pixelPerfectCamera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
    }

    private void Update()
    {
    }
}
