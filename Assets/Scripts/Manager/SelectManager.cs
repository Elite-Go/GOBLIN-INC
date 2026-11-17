using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectManager : Singleton<SelectManager>
{
    [SerializeField] private Camera cam;
    [SerializeField] private string selectTag;
    [SerializeField] private List<Goblin> selectedGoblins = new();

    public List<Goblin> SelectedGoblins => selectedGoblins;
    private Vector2 _dragCurrent;
    
    private GameObject _hoveredObject;
    public GameObject HoveredObject => _hoveredObject;

    private void Start()
    {
        if (!cam) Debug.LogError("에러 : 카메라가 할당 되지 않았습니다.");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(SelectAreaAction(Input.mousePosition));
        }

        _hoveredObject = SelectPoint(Input.mousePosition);
    }

    public IEnumerator SelectAreaAction(Vector2 dragStart)
    {
        while (Input.GetMouseButton(0))
        {
            _dragCurrent = Input.mousePosition;
            yield return null;
        }

        selectedGoblins.Clear();
        selectedGoblins = SelectArea(dragStart);
    }

    public List<Goblin> SelectArea(Vector2 dragStart)
    {
        List<Goblin> so = new();

        foreach (var sr in FindObjectsByType<Goblin>(FindObjectsSortMode.None))
        {
            if (sr == null || !sr.enabled || !sr.gameObject.CompareTag(selectTag)) continue;
            var screenPoint = cam.WorldToScreenPoint(sr.Sr.bounds.center);
            var screenRect = GetScreenRect(dragStart, _dragCurrent);
            if (screenPoint.z >= 0f && screenRect.Contains(new Vector2(screenPoint.x, screenPoint.y)))
                so.Add(sr);
        }

        return so;
    }

    public GameObject SelectPoint(Vector2 click)
    {
        Vector2 worldPoint = cam.ScreenToWorldPoint(click);

        var hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        return hit.collider != null ? hit.collider.gameObject : null;
    }

    private static Rect GetScreenRect(Vector2 a, Vector2 b)
    {
        var min = Vector2.Min(a, b);
        var max = Vector2.Max(a, b);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public void ControlGoblins(Action<Goblin> action)
    {
        foreach (var g in selectedGoblins)
        {
            action(g);
        }
    }
}