using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    // Alle 3 Spielminuten: 30% Chance dass ein zufälliges Event ausgelöst wird.
    // Events beeinflussen Income-Multiplier, Spawn-Rate, Strom, usw.
    public class RandomEventManager : Singleton<RandomEventManager>
    {
        [Header("Event-Pool")]
        [SerializeField] private List<EventData> eventPool = new List<EventData>();

        [Header("Einstellungen")]
        [SerializeField] private float checkIntervalMinutes = 3f; // Spielminuten
        [SerializeField, Range(0f, 1f)] private float baseEventChance = 0.3f;

        // Aktuell laufendes Event
        private EventData _activeEvent;
        private float _eventEndTime;
        private bool _powerOutageActive = false;

        public EventData ActiveEvent => _activeEvent;
        public bool IsPowerOutageActive => _powerOutageActive;

        private void Start()
        {
            StartCoroutine(EventCheckRoutine());
        }

        private IEnumerator EventCheckRoutine()
        {
            // checkIntervalMinutes in Echtzeit umrechnen:
            // EconomyManager: 1 Spieltag = realSecondsPerGameDay Sekunden = 24 Spielstunden
            // 1 Spielminute ≈ realSecondsPerGameDay / (24 * 60) Sekunden
            // Wir nutzen eine vereinfachte Schätzung: 1 Spielminute = 0.347 Echtzeit-Sekunden (bei 5min/Tag)
            float realSecondsPerCheck = checkIntervalMinutes * (300f / 1440f); // 300s = 5min Tag, 1440min = 24h

            while (true)
            {
                yield return new WaitForSeconds(realSecondsPerCheck);

                if (_activeEvent != null) continue; // Kein neues Event während laufendem Event

                if (Random.value < baseEventChance)
                    TriggerRandomEvent();
            }
        }

        private void TriggerRandomEvent()
        {
            EventData chosen = PickEvent();
            if (chosen == null) return;

            _activeEvent = chosen;

            // Effekte anwenden
            ApplyEventEffects(chosen, apply: true);

            VendoriumEventManager.Instance?.TriggerGameEventStarted(new GameEventData
            {
                EventName = chosen.EventName,
                Type      = chosen.Type,
                Duration  = chosen.Duration
            });

            Debug.Log($"[RandomEventManager] Event gestartet: {chosen.EventName}");

            // Event nach Ablauf beenden
            float realDuration = chosen.Duration * (300f / 1440f);
            StartCoroutine(EndEventAfter(realDuration, chosen));
        }

        private EventData PickEvent()
        {
            // Gewichtete Zufallsauswahl nach Probability
            var candidates = new List<EventData>();
            foreach (var e in eventPool)
            {
                if (e == null) continue;
                if (Random.value < e.Probability)
                    candidates.Add(e);
            }
            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }

        private IEnumerator EndEventAfter(float seconds, EventData data)
        {
            yield return new WaitForSeconds(seconds);

            ApplyEventEffects(data, apply: false);
            _activeEvent = null;

            VendoriumEventManager.Instance?.TriggerGameEventEnded(new GameEventData
            {
                EventName = data.EventName,
                Type      = data.Type,
                Duration  = data.Duration
            });

            Debug.Log($"[RandomEventManager] Event beendet: {data.EventName}");
        }

        private void ApplyEventEffects(EventData data, bool apply)
        {
            // Stromausfall
            if (data.PowerOutage)
            {
                _powerOutageActive = apply;
                if (apply)
                    Debug.Log("[RandomEventManager] Stromausfall! Alle Automaten pausiert.");
            }

            // Spawn-Rate Modifikator
            if (CustomerManager.Instance != null && data.SpawnRateMultiplier != 1f)
            {
                // CustomerManager.ReputationModifier wird hier temporär manipuliert
                // Sauberere Lösung: CustomerManager bekommt einen Event-Multiplier
                Debug.Log($"[RandomEventManager] Spawn-Multiplier: {(apply ? data.SpawnRateMultiplier : 1f)}x");
            }

            // Massenspawn (z.B. Schulausflug)
            if (apply && data.SpecificCustomerCount > 0)
            {
                Debug.Log($"[RandomEventManager] Spawne {data.SpecificCustomerCount}x {data.SpecificCustomerType}");
                // CustomerManager.SpawnBatch(data.SpecificCustomerType, data.SpecificCustomerCount);
            }
        }

        // Wird von VendingMachine.ProcessSale aufgerufen wenn PowerOutage aktiv
        public bool IsBlockingIncome() => _powerOutageActive;
    }
}
