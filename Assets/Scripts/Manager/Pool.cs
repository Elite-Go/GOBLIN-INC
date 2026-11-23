using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Pool<T> where T : Component
{
    [SerializeField] private T prefab;
    [SerializeField] private int initialSize = 10;

    private Queue<T> _inactiveQueue = new Queue<T>();
    private HashSet<T> _activeSet = new HashSet<T>();
    private Transform _root;

    public Transform Root => _root;
    public int ActiveCount => _activeSet.Count;
    public int InactiveCount => _inactiveQueue.Count;

    public void Initialize(Transform parentTransform = null)
    {
        if (_root) return;
        
        _root = parentTransform;

        for (int i = 0; i < initialSize; i++)
        {
            var obj = CreateNewInstance();
            obj.gameObject.SetActive(false);
            _inactiveQueue.Enqueue(obj);
        }
    }

    private T CreateNewInstance()
    {
        var obj = Object.Instantiate(prefab, _root);
        return obj;
    }

    public T Get()
    {
        if (_root == null)
        {
            Initialize();
        }

        T obj = null;

        while (_inactiveQueue.Count > 0)
        {
            obj = _inactiveQueue.Dequeue();
            if (obj != null) break;
        }

        if (obj == null)
        {
            obj = CreateNewInstance();
        }

        _activeSet.Add(obj);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public T Get(Vector3 position, Quaternion rotation)
    {
        T obj = Get();
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }

    public void Release(T obj)
    {
        if (obj == null) return;

        if (_activeSet.Contains(obj))
        {
            _activeSet.Remove(obj);
        }

        obj.gameObject.SetActive(false);
        obj.transform.SetParent(_root);
        _inactiveQueue.Enqueue(obj);
    }

    public void Clear()
    {
        foreach (var obj in _activeSet)
        {
            if (obj != null) Object.Destroy(obj.gameObject);
        }
        foreach (var obj in _inactiveQueue)
        {
            if (obj != null) Object.Destroy(obj.gameObject);
        }

        _activeSet.Clear();
        _inactiveQueue.Clear();

        if (_root != null)
        {
            Object.Destroy(_root.gameObject);
        }
    }
}