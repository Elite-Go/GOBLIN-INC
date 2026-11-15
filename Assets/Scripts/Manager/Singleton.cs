using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _isApplicationQuitting = false;

    public static T Instance
    {
        get
        {
            if (_isApplicationQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 씬에 이미 존재하는지 검색
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        // 씬에 없다면 새로 오브젝트를 생성해서 컴포넌트 부착
                        GameObject singletonObject = new GameObject(typeof(T).ToString());
                        _instance = singletonObject.AddComponent<T>();
                        
                        // 씬 전환 시 파괴되지 않도록 설정
                        DontDestroyOnLoad(singletonObject);
                    }
                }

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            // 이미 인스턴스가 존재하는데 새로 생겼다면 중복 생성된 것이므로 파괴
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
    }
}