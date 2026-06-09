using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    // Verwaltet den Kassierer-Modus. Der Spieler betritt die Zone hinter der Theke,
    // drückt E → Kassierer-Modus aktiviert sich (feste Kamera, Warteschlange sichtbar).
    public class CashRegisterManager : Singleton<CashRegisterManager>
    {
        [Header("Kamera-Position im Kassierer-Modus")]
        [SerializeField] private Transform cashierCameraPosition;
        [SerializeField] private Camera playerCamera;

        [Header("Einstellungen")]
        [SerializeField] private float baseTransactionTime = 2f;
        // 30% der Transaktionen lösen das Wechselgeld-Minigame aus
        [SerializeField, Range(0f, 1f)] private float changeGameChance = 0.3f;

        private Queue<CustomerController> _queue = new Queue<CustomerController>();
        private CustomerController _currentCustomer;
        private bool _isActive = false;
        private int _todayServedCount = 0;
        private decimal _todayActiveIncome = 0m;

        private Vector3 _savedCameraPos;
        private Quaternion _savedCameraRot;

        public bool IsActive => _isActive;
        public int QueueLength => _queue.Count;
        public CustomerController CurrentCustomer => _currentCustomer;
        public int TodayServedCount => _todayServedCount;

        private void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            VendoriumEventManager.Instance.OnCustomerEntered += OnCustomerEntered;
            VendoriumEventManager.Instance.OnNewDayStarted   += _ => ResetDailyStats();
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance == null) return;
            VendoriumEventManager.Instance.OnCustomerEntered -= OnCustomerEntered;
        }

        // Spieler betritt die Kassierer-Zone (von CashierTrigger aufgerufen)
        public void EnterCashierMode()
        {
            if (_isActive) return;

            _isActive = true;
            GameManager.Instance?.SetGameState(GameState.CashierMode);

            // Kamera auf feste Position bewegen
            if (playerCamera != null && cashierCameraPosition != null)
            {
                _savedCameraPos = playerCamera.transform.position;
                _savedCameraRot = playerCamera.transform.rotation;
                playerCamera.transform.position = cashierCameraPosition.position;
                playerCamera.transform.rotation = cashierCameraPosition.rotation;
            }

            UIManager.Instance?.ShowCashRegister();
            NextCustomer();

            Debug.Log("[CashRegister] Kassierer-Modus aktiviert.");
        }

        public void ExitCashierMode()
        {
            if (!_isActive) return;
            _isActive = false;

            // Kamera zurücksetzen
            if (playerCamera != null)
            {
                playerCamera.transform.position = _savedCameraPos;
                playerCamera.transform.rotation = _savedCameraRot;
            }

            UIManager.Instance?.CloseCurrent();
            GameManager.Instance?.SetGameState(GameState.Playing);
        }

        // Wird vom UI-Button "Kassieren" aufgerufen
        public CashTransaction ProcessCurrentCustomer()
        {
            if (_currentCustomer == null) return null;

            float price = CalculatePrice(_currentCustomer);
            bool triggerMinigame = Random.value < changeGameChance;

            var transaction = new CashTransaction
            {
                Customer = _currentCustomer,
                Price    = price,
                Payment  = GetPaymentAmount(price),
                RequiresChangeGame = triggerMinigame
            };

            if (!triggerMinigame)
                FinalizeTransaction(transaction, correct: true);

            return transaction;
        }

        // Wird vom ChangeMakingMinigame aufgerufen wenn Spieler fertig ist
        public void FinalizeTransaction(CashTransaction t, bool correct)
        {
            float bonus = correct ? t.Price * 0.1f : 0f;
            float total = t.Price + bonus;

            EconomyManager.Instance?.AddMoney((decimal)total, "Kasse", isPassive: false);
            _todayActiveIncome += (decimal)total;
            _todayServedCount++;

            VendoriumEventManager.Instance?.TriggerCustomerServed(_currentCustomer);
            CustomerManager.Instance?.ReturnToPool(_currentCustomer);
            _currentCustomer = null;

            NextCustomer();
        }

        private void NextCustomer()
        {
            _currentCustomer = _queue.Count > 0 ? _queue.Dequeue() : null;
        }

        // Kunden die im Laden kaufen wollen stellen sich in die Kassen-Schlange
        private void OnCustomerEntered(CustomerController customer)
        {
            // Nur ein Teil der Kunden geht zur Kasse (20%)
            if (Random.value < 0.2f)
                _queue.Enqueue(customer);
        }

        private float CalculatePrice(CustomerController customer)
        {
            // Einfache Preisberechnung: 1-5 € je nach Kundentyp
            return Random.Range(1.0f, 5.0f);
        }

        private float GetPaymentAmount(float price)
        {
            // Kunde zahlt mit dem nächsthöheren Schein
            if (price <= 5f)  return 5f;
            if (price <= 10f) return 10f;
            return 20f;
        }

        private void ResetDailyStats()
        {
            _todayServedCount = 0;
            _todayActiveIncome = 0m;
        }
    }

    [System.Serializable]
    public class CashTransaction
    {
        public CustomerController Customer;
        public float Price;
        public float Payment;
        public float Change => Payment - Price;
        public bool RequiresChangeGame;
    }
}
