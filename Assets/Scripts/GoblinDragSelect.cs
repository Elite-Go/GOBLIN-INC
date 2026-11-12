using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Owns the left-drag selection interaction for Goblin-tagged sprites.</summary>
public sealed class GoblinDragSelect : MonoBehaviour
{
    private const int SelectionButton = 0;
    private const float DragThreshold = 6f;

    [SerializeField]
    [Tooltip("마지막 선택 조작으로 선택된 고블린 목록입니다.")]
    [FormerlySerializedAs("selectedGoblins")]
    private List<GameObject> selected = new List<GameObject>();

    private Vector2 dragStart;
    private Vector2 dragCurrent;
    private bool isDragging;

    public IReadOnlyList<GameObject> SelectedGoblins => selected;
    public event Action<Vector2> PointerClicked;
    public bool IsDragging => isDragging;
    public Rect DragRectangle => GetScreenRect(dragStart, dragCurrent);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForLoadedScene()
    {
        if (FindFirstObjectByType<GoblinDragSelect>() != null) return;
        var host = new GameObject(nameof(GoblinDragSelect));
        DontDestroyOnLoad(host);
        host.AddComponent<GoblinDragSelect>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(SelectionButton))
        {
            dragStart = Input.mousePosition;
            dragCurrent = dragStart;
            isDragging = false;
        }

        if (Input.GetMouseButton(SelectionButton))
        {
            dragCurrent = Input.mousePosition;
            if ((dragCurrent - dragStart).sqrMagnitude >= DragThreshold * DragThreshold)
                isDragging = true;
        }

        if (Input.GetMouseButtonUp(SelectionButton))
        {
            dragCurrent = Input.mousePosition;
            if ((dragCurrent - dragStart).sqrMagnitude >= DragThreshold * DragThreshold)
                isDragging = true;
            if (isDragging) SelectGoblinsInRectangle(GetScreenRect(dragStart, dragCurrent));
            else PointerClicked?.Invoke(dragCurrent);
            isDragging = false;
        }

        selected.RemoveAll(goblin => goblin == null);
    }

    private void OnGUI()
    {
        if (!isDragging) return;
        var rect = GetGuiRect(dragStart, dragCurrent);
        var previousColor = GUI.color;
        GUI.color = new Color(0.25f, 0.85f, 1f, 0.16f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(0.35f, 0.9f, 1f, 0.95f);
        GUI.Box(rect, GUIContent.none);
        GUI.color = previousColor;
    }

    private void SelectGoblinsInRectangle(Rect screenRect)
    {
        selected.Clear();
        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogWarning("GoblinDragSelect needs a MainCamera to select goblins.");
            return;
        }

        foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.CompareTag("Goblin")) continue;
            var screenPoint = camera.WorldToScreenPoint(renderer.bounds.center);
            if (screenPoint.z >= 0f && screenRect.Contains(new Vector2(screenPoint.x, screenPoint.y)))
                selected.Add(renderer.gameObject);
        }
    }

    private static Rect GetScreenRect(Vector2 a, Vector2 b)
    {
        var min = Vector2.Min(a, b);
        var max = Vector2.Max(a, b);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static Rect GetGuiRect(Vector2 a, Vector2 b)
    {
        var rect = GetScreenRect(a, b);
        return new Rect(rect.xMin, Screen.height - rect.yMax, rect.width, rect.height);
    }
}
