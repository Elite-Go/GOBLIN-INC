using System.Collections.Generic;
using UnityEngine;

public class SelectManager : MonoBehaviour
{
    [SerializeField] private Structure structure;
    [SerializeField] private Resource resource;
    
    [SerializeField] private Camera cam;
    
    private void Start()
    {
        
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var obj = SelectObject();
            if (!obj)
            {
                structure = null;
                resource = null;
                return;
            }
             
            if (obj.TryGetComponent(out Structure str))
            {
                structure = str;
                CameraManager.ZoomIn(str.transform);
            }
            else if (obj.TryGetComponent(out Resource res))
            {
                resource = res;
                CameraManager.ZoomIn(res.transform);
                var job = new Job(res.transform.position, structure, res);
                StartCoroutine(structure?.ControlAllGoblins(g => g.GiveJob(job)));
            }
        }
    }

    public GameObject SelectObject()
    {
        Vector2 worldPoint = cam.ScreenToWorldPoint(Input.mousePosition);

        var hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        return hit.collider ? hit.collider.gameObject : null;
    }
}
