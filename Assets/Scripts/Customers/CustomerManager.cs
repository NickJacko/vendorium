using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    public class CustomerManager : Singleton<CustomerManager>
    {
        [Header("Spawn-Einstellungen")]
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private int maxCustomers = 15;
        [SerializeField] private float baseSpawnInterval = 8f;

        [Header("Zufriedenheit")]
        [Range(0f, 100f)]
        [SerializeField] private float averageSatisfaction = 70f;

        // Spawn-Rate Multiplikatoren je nach Tageszeit (Morgen-Peak für Büroangestellte)
        private readonly Dictionary<TimeOfDay, float> _spawnRateMultipliers = new Dictionary<TimeOfDay, float>
        {
            { TimeOfDay.Frueh,       0.8f },
            { TimeOfDay.Morgen,      1.5f },
            { TimeOfDay.Mittag,      1.3f },
            { TimeOfDay.Nachmittag,  1.0f },
            { TimeOfDay.Abend,       0.9f },
            { TimeOfDay.Nacht,       0.4f }
        };

        private List<CustomerController> _activeCustomers = new List<CustomerController>();
        private List<CustomerController> _customerPool = new List<CustomerController>();

        // Stammkunden: Kunden die mehr als 3x gekommen sind
        private Dictionary<string, RegularCustomerData> _regularCustomers = new Dictionary<string, RegularCustomerData>();

        // Mundpropaganda-Bonus: wird wöchentlich angewendet
        private float _reputationSpawnModifier = 1.0f;

        public int ActiveCustomerCount => _activeCustomers.Count;
        public float ReputationModifier => _reputationSpawnModifier;

        private void Start()
        {
            VendoriumEventManager.Instance.OnTimeOfDayChanged += OnTimeOfDayChanged;
            VendoriumEventManager.Instance.OnCustomerLeft += OnCustomerLeft;
            VendoriumEventManager.Instance.OnNewDayStarted += OnNewDayStarted;

            StartCoroutine(SpawnRoutine());
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance == null) return;
            VendoriumEventManager.Instance.OnTimeOfDayChanged -= OnTimeOfDayChanged;
            VendoriumEventManager.Instance.OnCustomerLeft -= OnCustomerLeft;
            VendoriumEventManager.Instance.OnNewDayStarted -= OnNewDayStarted;
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                float currentMultiplier = GetCurrentSpawnMultiplier();
                float interval = baseSpawnInterval / (currentMultiplier * _reputationSpawnModifier);

                yield return new WaitForSeconds(interval);

                if (_activeCustomers.Count < maxCustomers && spawnPoint != null)
                    SpawnCustomer();
            }
        }

        private void SpawnCustomer()
        {
            if (customerPrefab == null)
            {
                Debug.LogWarning("[CustomerManager] Kein customerPrefab zugewiesen.");
                return;
            }

            // Object Pool: zuerst aus Pool holen, sonst neu erstellen
            CustomerController customer = GetFromPool();

            if (customer == null)
            {
                var go = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
                customer = go.GetComponent<CustomerController>();
            }
            else
            {
                customer.transform.position = spawnPoint.position;
                customer.transform.rotation = spawnPoint.rotation;
                customer.gameObject.SetActive(true);
            }

            if (customer != null)
            {
                _activeCustomers.Add(customer);
                EconomyManager.Instance.RegisterCustomerVisit();
                VendoriumEventManager.Instance.TriggerCustomerEntered(customer);
            }
        }

        // Gibt einen Kunden an den Pool zurück (statt Destroy für Performance)
        public void ReturnToPool(CustomerController customer)
        {
            if (customer == null) return;

            _activeCustomers.Remove(customer);
            customer.gameObject.SetActive(false);
            _customerPool.Add(customer);

            VendoriumEventManager.Instance.TriggerCustomerLeft(customer);
        }

        private CustomerController GetFromPool()
        {
            for (int i = _customerPool.Count - 1; i >= 0; i--)
            {
                var c = _customerPool[i];
                if (c != null)
                {
                    _customerPool.RemoveAt(i);
                    return c;
                }
            }
            return null;
        }

        public float GetAverageSatisfaction() => averageSatisfaction;

        public void UpdateSatisfaction(float delta)
        {
            averageSatisfaction = Mathf.Clamp(averageSatisfaction + delta, 0f, 100f);
            VendoriumEventManager.Instance.TriggerSatisfactionChanged(averageSatisfaction);
        }

        // Mundpropaganda: tägliche Anpassung der Spawn-Rate basierend auf Zufriedenheit
        private void ApplyReputationEffect()
        {
            if (averageSatisfaction > 80f)
                _reputationSpawnModifier = Mathf.Min(_reputationSpawnModifier + 0.05f, 2.0f);
            else if (averageSatisfaction < 30f)
                _reputationSpawnModifier = Mathf.Max(_reputationSpawnModifier - 0.1f, 0.3f);
        }

        // Stammkunden-System: Kunden der selben ID die 3+ mal kommen
        public void RecordCustomerVisit(string customerId, CustomerType type)
        {
            if (!_regularCustomers.ContainsKey(customerId))
            {
                _regularCustomers[customerId] = new RegularCustomerData
                {
                    CustomerId = customerId,
                    Type = type,
                    VisitCount = 0,
                    Name = GenerateRandomName()
                };
            }

            _regularCustomers[customerId].VisitCount++;

            if (_regularCustomers[customerId].VisitCount == 3)
                Debug.Log($"[CustomerManager] Stammkunde '{_regularCustomers[customerId].Name}' registriert!");
        }

        public bool IsRegularCustomer(string customerId) =>
            _regularCustomers.TryGetValue(customerId, out var data) && data.VisitCount >= 3;

        private float GetCurrentSpawnMultiplier()
        {
            var tod = EconomyManager.Instance != null
                ? EconomyManager.Instance.CurrentTimeOfDay
                : TimeOfDay.Morgen;

            return _spawnRateMultipliers.TryGetValue(tod, out float m) ? m : 1.0f;
        }

        private string GenerateRandomName()
        {
            string[] vornamen = { "Klaus", "Erika", "Hans", "Maria", "Peter", "Anna", "Werner", "Gabi" };
            string[] nachnamen = { "Müller", "Schmidt", "Weber", "Fischer", "Meyer", "Wagner" };
            return $"{vornamen[Random.Range(0, vornamen.Length)]} {nachnamen[Random.Range(0, nachnamen.Length)]}";
        }

        private void OnTimeOfDayChanged(TimeOfDay time) { }

        private void OnCustomerLeft(CustomerController customer)
        {
            _activeCustomers.Remove(customer);
        }

        private void OnNewDayStarted(int day)
        {
            ApplyReputationEffect();
        }
    }

    [System.Serializable]
    public class RegularCustomerData
    {
        public string CustomerId;
        public string Name;
        public CustomerType Type;
        public int VisitCount;
        public List<string> PreferredMachineTags = new List<string>();
    }
}
