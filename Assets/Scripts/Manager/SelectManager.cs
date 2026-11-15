using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectManager : Singleton<SelectManager>
{
    [SerializeField] private Camera cam;
    [SerializeField] private string selectTag;
    [SerializeField] private List<GameObject> selectedObjects = new();

    private Vector2 _dragCurrent;

    private void Start()
    {
        if (!cam) Debug.LogError("에러 : 카메라가 할당 되지 않았습니다."); 
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            selectedObjects.Clear();
            var s = SelectPoint(Input.mousePosition);
            selectedObjects.Add(s);
            //StartCoroutine(SelectAreaAction(Input.mousePosition));
        }
    }

    public IEnumerator SelectAreaAction(Vector2 dragStart)
    {
        while (Input.GetMouseButton(0))
        {
            _dragCurrent = Input.mousePosition;
            yield return null;
        }
        
        selectedObjects.Clear();   
        selectedObjects = SelectArea(dragStart);
    }
    
    public List<GameObject> SelectArea(Vector2 dragStart)
    {
        List<GameObject> so = new();
        
        foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sr == null || !sr.enabled || !sr.gameObject.CompareTag(selectTag)) continue;
            var screenPoint = cam.WorldToScreenPoint(sr.bounds.center);
            var screenRect = GetScreenRect(dragStart, _dragCurrent);
            if (screenPoint.z >= 0f && screenRect.Contains(new Vector2(screenPoint.x, screenPoint.y)))
                so.Add(sr.gameObject);
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
}
