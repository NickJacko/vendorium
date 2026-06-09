using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    public class EconomyManager : Singleton<EconomyManager>
    {
        [Header("Startkapital")]
        [SerializeField] private decimal startMoney = 1000m;

        [Header("Zeitsystem")]
        // Ein Spieltag = 5 echte Minuten
        [SerializeField] private float realSecondsPerGameDay = 300f;

        // --- Interner Zustand ---
        private decimal _currentMoney;
        private decimal _todayIncome;
        private decimal _todayExpenses;
        private decimal _todayPassiveIncome;
        private decimal _todayActiveIncome;
        private int _todayCustomerCount;
        private int _todaySalesCount;

        private int _currentDay = 1;
        private float _dayTime = 0f; // 0.0 bis 1.0 (ein voller Tag)
        private TimeOfDay _currentTimeOfDay = TimeOfDay.Morgen;

        private List<TransactionRecord> _transactionLog = new List<TransactionRecord>();
        private List<DailyStats> _weekHistory = new List<DailyStats>();

        // Wochenverlauf (letzte 7 Tage)
        private decimal[] _weeklyRevenue = new decimal[7];

        public decimal CurrentMoney => _currentMoney;
        public int CurrentDay => _currentDay;
        public float DayTimeNormalized => _dayTime;
        public TimeOfDay CurrentTimeOfDay => _currentTimeOfDay;
        public decimal TodayIncome => _todayIncome;
        public decimal TodayExpenses => _todayExpenses;
        public IReadOnlyList<TransactionRecord> TransactionLog => _transactionLog;

        protected override void Awake()
        {
            base.Awake();
            _currentMoney = startMoney;
        }

        private void Start()
        {
            StartCoroutine(DayCycleRoutine());
        }

        // Geld hinzufügen (Einnahmen)
        public void AddMoney(decimal amount, string source, bool isPassive = false)
        {
            if (amount <= 0m) return;

            _currentMoney += amount;
            _todayIncome += amount;

            if (isPassive)
                _todayPassiveIncome += amount;
            else
                _todayActiveIncome += amount;

            _todaySalesCount++;

            LogTransaction(amount, source, TransactionType.Income);
            VendoriumEventManager.Instance.TriggerMoneyAdded(amount, source);
            VendoriumEventManager.Instance.TriggerMoneyChanged(_currentMoney);
        }

        // Geld ausgeben — gibt false zurück wenn nicht genug vorhanden
        public bool SpendMoney(decimal amount, string reason)
        {
            if (amount <= 0m) return true;

            if (_currentMoney < amount)
            {
                Debug.LogWarning($"[EconomyManager] Nicht genug Geld für '{reason}'. Benötigt: {amount:F2}, Vorhanden: {_currentMoney:F2}");
                return false;
            }

            _currentMoney -= amount;
            _todayExpenses += amount;

            LogTransaction(-amount, reason, TransactionType.Expense);
            VendoriumEventManager.Instance.TriggerMoneySpent(amount, reason);
            VendoriumEventManager.Instance.TriggerMoneyChanged(_currentMoney);

            if (_currentMoney <= 0m)
                VendoriumEventManager.Instance.TriggerBankruptcy();

            return true;
        }

        public decimal GetCurrentMoney() => _currentMoney;

        public decimal GetTodayIncome() => _todayIncome;

        public decimal GetTodayProfit() => _todayIncome - _todayExpenses;

        public decimal[] GetWeeklyRevenue() => _weeklyRevenue;

        // Setzt den Geldstand direkt (nur für SaveManager beim Laden)
        public void SetMoneyDirect(decimal amount)
        {
            _currentMoney = amount;
            VendoriumEventManager.Instance.TriggerMoneyChanged(_currentMoney);
        }

        public void RegisterCustomerVisit()
        {
            _todayCustomerCount++;
        }

        private void LogTransaction(decimal amount, string description, TransactionType type)
        {
            _transactionLog.Add(new TransactionRecord
            {
                Timestamp = DateTime.Now,
                Description = description,
                Amount = amount,
                Type = type,
                BalanceAfter = _currentMoney
            });

            // Maximal 50 Einträge im Log behalten
            if (_transactionLog.Count > 50)
                _transactionLog.RemoveAt(0);
        }

        private IEnumerator DayCycleRoutine()
        {
            while (true)
            {
                yield return null;
                _dayTime += Time.deltaTime / realSecondsPerGameDay;

                UpdateTimeOfDay();

                if (_dayTime >= 1f)
                {
                    _dayTime -= 1f;
                    EndDay();
                }
            }
        }

        private void UpdateTimeOfDay()
        {
            // Tageszeit 0-1 wird auf 0-24 Uhr gemappt
            float hour = _dayTime * 24f;
            TimeOfDay newTime;

            if (hour >= 6f && hour < 9f)        newTime = TimeOfDay.Frueh;
            else if (hour >= 9f && hour < 12f)   newTime = TimeOfDay.Morgen;
            else if (hour >= 12f && hour < 15f)  newTime = TimeOfDay.Mittag;
            else if (hour >= 15f && hour < 18f)  newTime = TimeOfDay.Nachmittag;
            else if (hour >= 18f && hour < 21f)  newTime = TimeOfDay.Abend;
            else                                  newTime = TimeOfDay.Nacht;

            if (newTime != _currentTimeOfDay)
            {
                _currentTimeOfDay = newTime;
                VendoriumEventManager.Instance.TriggerTimeOfDayChanged(_currentTimeOfDay);
            }
        }

        private void EndDay()
        {
            var stats = new DailyStats
            {
                Tag = _currentDay,
                Tagesumsatz = _todayIncome,
                Tagesausgaben = _todayExpenses,
                Tagesgewinn = _todayIncome - _todayExpenses,
                KundenAnzahl = _todayCustomerCount,
                VerkaufsAnzahl = _todaySalesCount,
                DurchschnittlicheZufriedenheit = CustomerManager.Instance != null
                    ? CustomerManager.Instance.GetAverageSatisfaction()
                    : 0f
            };

            _weekHistory.Add(stats);
            _weeklyRevenue[(_currentDay - 1) % 7] = _todayIncome;

            VendoriumEventManager.Instance.TriggerDailyReport(stats);

            // Tageswerte zurücksetzen
            _todayIncome = 0m;
            _todayExpenses = 0m;
            _todayPassiveIncome = 0m;
            _todayActiveIncome = 0m;
            _todayCustomerCount = 0;
            _todaySalesCount = 0;

            _currentDay++;
            VendoriumEventManager.Instance.TriggerNewDayStarted(_currentDay);
        }
    }

    [Serializable]
    public class TransactionRecord
    {
        public DateTime Timestamp;
        public string Description;
        public decimal Amount;
        public TransactionType Type;
        public decimal BalanceAfter;
    }

    public enum TransactionType
    {
        Income,
        Expense
    }
}
