using DG.Tweening;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [Header("Move Settings")]
    [SerializeField] private float dragSpeed = 1f;
    [SerializeField] private bool invertDrag = true;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float zoomSmoothDuration = 0.2f;
    [SerializeField] private float minOrthoSize = 2f;
    [SerializeField] private float maxOrthoSize = 15f;

    private Camera _cam;
    private Tweener _moveTween;
    private Tweener _zoomTween;

    private Vector3 _lastDragWorldPos;
    private float _targetOrthoSize;
    private bool _isDragging;

    protected override void Awake()
    {
        base.Awake();
        _cam = GetComponent<Camera>();
        if (_cam != null)
        {
            _targetOrthoSize = Mathf.Clamp(_cam.orthographicSize, minOrthoSize, maxOrthoSize);
            _cam.orthographicSize = _targetOrthoSize;
        }
    }

    private void Update()
    {
        if (_cam == null) return;

        HandleRightClickDrag();
        HandleMouseWheelZoom();
    }

    public static void MoveTo(Transform target, float duration = 0.5f, Ease ease = Ease.OutQuad)
    {
        if (target == null || Instance == null || Instance._cam == null) return;

        Instance._isDragging = false;
        Instance._moveTween?.Kill();

        float z = Instance._cam.transform.position.z;
        Vector3 destination = new Vector3(target.position.x, target.position.y, z);

        Instance._moveTween = Instance._cam.transform
            .DOMove(destination, duration)
            .SetEase(ease)
            .SetLink(Instance.gameObject);
    }

    private void HandleRightClickDrag()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _moveTween?.Kill();
            _isDragging = true;
            _lastDragWorldPos = GetMouseWorldPositionOnCameraPlane();
        }
        else if (Input.GetMouseButton(1) && _isDragging)
        {
            _moveTween?.Kill();

            Vector3 currentWorldPos = GetMouseWorldPositionOnCameraPlane();
            Vector3 delta = currentWorldPos - _lastDragWorldPos;
            delta.z = 0f;

            float direction = invertDrag ? -1f : 1f;
            transform.position += delta * (direction * dragSpeed);

            _lastDragWorldPos = GetMouseWorldPositionOnCameraPlane();
        }
        else if (Input.GetMouseButtonUp(1))
        {
            _isDragging = false;
        }
    }

    private void HandleMouseWheelZoom()
    {
        if (!_cam.orthographic) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll, 0f)) return;

        _targetOrthoSize = Mathf.Clamp(
            _targetOrthoSize - scroll * zoomSpeed,
            minOrthoSize,
            maxOrthoSize
        );

        _zoomTween?.Kill();
        _zoomTween = _cam.DOOrthoSize(_targetOrthoSize, zoomSmoothDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject);
    }

    private Vector3 GetMouseWorldPositionOnCameraPlane()
    {
        float distanceFromCamera = Mathf.Abs(transform.position.z);
        Vector3 screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, distanceFromCamera);
        return _cam.ScreenToWorldPoint(screenPos);
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
        _zoomTween?.Kill();
    }
}