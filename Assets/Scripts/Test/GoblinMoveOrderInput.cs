using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class GoblinMoveOrderInput : MonoBehaviour
{
    [SerializeField] [Tooltip("고블린 선택 입력을 처리하는 컴포넌트입니다.")]
    [FormerlySerializedAs("selection")]
    private GoblinDragSelect select;
    [SerializeField] [Tooltip("선택된 고블린의 이동을 처리하는 컴포넌트입니다.")]
    [FormerlySerializedAs("squadMover")]
    private GoblinSquadMover mover;
    [SerializeField] [Tooltip("마우스 아래 스프라이트를 찾는 서비스입니다.")]
    [FormerlySerializedAs("pointerTargeting")]
    private SpritePointerTargetingService targets;
    [SerializeField] [Tooltip("자원 채취 이동 명령을 처리하는 서비스입니다.")]
    [FormerlySerializedAs("resourceOrderService")]
    private GoblinResourceOrderService resources;
    [SerializeField] [Tooltip("화면 좌표를 월드 좌표로 변환할 카메라입니다.")]
    [FormerlySerializedAs("worldCamera")]
    private Camera cam;

    private void Awake()
    {
        if (select == null) select = FindFirstObjectByType<GoblinDragSelect>();
        if (mover == null) mover = GetComponent<GoblinSquadMover>();
        if (targets == null) targets = FindFirstObjectByType<SpritePointerTargetingService>();
        if (resources == null) resources = GetComponent<GoblinResourceOrderService>();
        if (cam == null) cam = Camera.main;
    }

    private void OnEnable()
    {
        if (select != null) select.PointerClicked += MoveSelectionToPointerTarget;
    }

    private void OnDisable()
    {
        if (select != null) select.PointerClicked -= MoveSelectionToPointerTarget;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(1) || select == null || mover == null)
            return;

        var pointerPosition = (Vector2)Input.mousePosition;
        if (TryMoveSelectionToPointerTarget(pointerPosition)) return;
        if (cam == null) return;

        var clickRay = cam.ScreenPointToRay(pointerPosition);
        var groundPlane = new Plane(Vector3.forward, Vector3.zero);
        if (!groundPlane.Raycast(clickRay, out var distance)) return;

        var worldPoint = clickRay.GetPoint(distance);
        mover.Move(select.SelectedGoblins, new Vector2(worldPoint.x, worldPoint.y));
    }

    private void MoveSelectionToPointerTarget(Vector2 screenPosition)
    {
        TryMoveSelectionToPointerTarget(screenPosition);
    }

    private bool TryMoveSelectionToPointerTarget(Vector2 screenPosition)
    {
        if (select == null || mover == null || targets == null ||
            !targets.TryResolve(screenPosition, out var target))
            return false;

        if (resources != null &&
            resources.TryIssueOrder(select.SelectedGoblins, target, mover))
            return true;

        mover.MoveToTarget(select.SelectedGoblins, target);
        return true;
    }
}
