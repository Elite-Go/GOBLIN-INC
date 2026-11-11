using UnityEngine;

/// <summary>Translates right-click input into a world-space move order for the current selection.</summary>
public sealed class GoblinMoveOrderInput : MonoBehaviour
{
    [SerializeField] private GoblinDragSelect selection;
    [SerializeField] private GoblinSquadMover squadMover;
    [SerializeField] private Camera worldCamera;

    private void Awake()
    {
        if (selection == null) selection = FindFirstObjectByType<GoblinDragSelect>();
        if (squadMover == null) squadMover = GetComponent<GoblinSquadMover>();
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(1) || selection == null || squadMover == null || worldCamera == null)
            return;

        var clickRay = worldCamera.ScreenPointToRay(Input.mousePosition);
        var groundPlane = new Plane(Vector3.forward, Vector3.zero);
        if (!groundPlane.Raycast(clickRay, out var distance)) return;

        var worldPoint = clickRay.GetPoint(distance);
        squadMover.Move(selection.SelectedGoblins, new Vector2(worldPoint.x, worldPoint.y));
    }
}
