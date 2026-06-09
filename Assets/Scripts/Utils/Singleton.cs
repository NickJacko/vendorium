using UnityEngine;

namespace Vendorium
{
    // Generische Singleton-Basisklasse. Alle Manager erben davon.
    // DontDestroyOnLoad sorgt dafür, dass der Manager Scene-Wechsel überlebt.
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instanz von {typeof(T)} wird angefragt, aber die Anwendung beendet sich.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (FindObjectsOfType<T>().Length > 1)
                        {
                            Debug.LogError($"[Singleton] Mehrere Instanzen von {typeof(T)} gefunden!");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            var singletonGO = new GameObject($"[{typeof(T).Name}]");
                            _instance = singletonGO.AddComponent<T>();
                            DontDestroyOnLoad(singletonGO);
                            Debug.Log($"[Singleton] {typeof(T)} wurde als neues Singleton erstellt.");
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
                Debug.LogWarning($"[Singleton] Duplikat von {typeof(T)} gefunden. Wird zerstört.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}
