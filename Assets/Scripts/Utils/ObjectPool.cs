using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    // Generischer Object Pool — vermeidet Instantiate/Destroy für häufige Objekte.
    // Ziel: 60 FPS mit 20 Kunden, Münz-Partikeln und SFX gleichzeitig.
    //
    // Verwendung:
    //   var pool = new ObjectPool<CustomerController>(prefab, parent, 10);
    //   var obj  = pool.Get(position, rotation);
    //   pool.Return(obj);

    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _available = new Queue<T>();
        private readonly List<T> _all = new List<T>();
        private readonly int _maxSize;

        public int TotalCount  => _all.Count;
        public int ActiveCount => _all.Count - _available.Count;

        public ObjectPool(T prefab, Transform parent, int initialSize, int maxSize = -1)
        {
            _prefab  = prefab;
            _parent  = parent;
            _maxSize = maxSize < 0 ? initialSize * 3 : maxSize;

            for (int i = 0; i < initialSize; i++)
                CreateNew();
        }

        // Holt ein Objekt aus dem Pool (oder erstellt ein neues wenn Pool leer)
        public T Get(Vector3 position = default, Quaternion rotation = default)
        {
            T obj;

            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.gameObject.SetActive(true);
            }
            else if (_all.Count < _maxSize)
            {
                obj = CreateNew();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Pool voll ({_maxSize}). Ältestes Objekt recycelt.");
                obj = _all[0];
                obj.gameObject.SetActive(false);
                Return(obj);
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.gameObject.SetActive(true);
            }

            return obj;
        }

        // Gibt ein Objekt in den Pool zurück
        public void Return(T obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(false);
            if (!_available.Contains(obj))
                _available.Enqueue(obj);
        }

        // Gibt alle aktiven Objekte zurück
        public void ReturnAll()
        {
            foreach (var obj in _all)
            {
                if (obj != null && obj.gameObject.activeSelf)
                    Return(obj);
            }
        }

        // Bereinigt den Pool komplett
        public void Destroy()
        {
            foreach (var obj in _all)
                if (obj != null) Object.Destroy(obj.gameObject);

            _all.Clear();
            _available.Clear();
        }

        private T CreateNew()
        {
            var go  = Object.Instantiate(_prefab.gameObject, _parent);
            var obj = go.GetComponent<T>();
            go.SetActive(false);
            _all.Add(obj);
            return obj;
        }
    }

    // MonoBehaviour-Wrapper für einfache Pool-Verwaltung im Inspector.
    // Für Kunden: CustomerManager hält den Pool.
    // Für Partikel-Systeme wird ein separater ParticlePool verwendet (siehe unten).
    public class MonoBehaviourPool<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] protected T prefab;
        [SerializeField] protected int initialSize = 10;
        [SerializeField] protected int maxSize = 30;

        protected ObjectPool<T> Pool;

        protected virtual void Awake()
        {
            if (prefab != null)
                Pool = new ObjectPool<T>(prefab, transform, initialSize, maxSize);
        }

        public virtual T Get(Vector3 pos, Quaternion rot) => Pool?.Get(pos, rot);
        public virtual void Return(T obj)                  => Pool?.Return(obj);
    }

    // Spezieller Pool für ParticleSystem-Effekte
    public class ParticlePool : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particlePrefab;
        [SerializeField] private int poolSize = 10;

        private Queue<ParticleSystem> _available = new Queue<ParticleSystem>();

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var ps = Instantiate(particlePrefab, transform);
                ps.gameObject.SetActive(false);
                _available.Enqueue(ps);
            }
        }

        public ParticleSystem Play(Vector3 position)
        {
            ParticleSystem ps;

            if (_available.Count > 0)
            {
                ps = _available.Dequeue();
            }
            else
            {
                ps = Instantiate(particlePrefab, transform);
            }

            ps.transform.position = position;
            ps.gameObject.SetActive(true);
            ps.Play();

            // Automatisch zurückgeben wenn fertig
            StartCoroutine(ReturnWhenFinished(ps));
            return ps;
        }

        private System.Collections.IEnumerator ReturnWhenFinished(ParticleSystem ps)
        {
            yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetime.constantMax);
            ps.Stop();
            ps.gameObject.SetActive(false);
            _available.Enqueue(ps);
        }
    }
}
