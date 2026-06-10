using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    // Zentraler Event-Bus nach Observer-Pattern.
    // Systeme subscriben auf Events ohne sich direkt zu kennen (lose Kopplung).
    public class VendoriumEventManager : Singleton<VendoriumEventManager>
    {
        // --- Wirtschaft ---
        public event Action<decimal> OnMoneyChanged;
        public event Action<decimal, string> OnMoneyAdded;       // Betrag, Quelle
        public event Action<decimal, string> OnMoneySpent;       // Betrag, Grund
        public event Action<DailyStats> OnDailyReport;
        public event Action OnBankruptcy;

        // --- Automaten ---
        public event Action<VendingMachine> OnMachinePlaced;
        public event Action<VendingMachine> OnMachineRemoved;
        public event Action<VendingMachine, float> OnMachineSale; // Maschine, Betrag
        public event Action<VendingMachine> OnMachineStockEmpty;
        public event Action<VendingMachine, int> OnMachineUpgraded; // Maschine, neues Level
        public event Action<VendingMachine> OnMachineBroken;

        // --- Kunden ---
        public event Action<CustomerController> OnCustomerEntered;
        public event Action<CustomerController> OnCustomerLeft;
        public event Action<CustomerController> OnCustomerServed;
        public event Action<CustomerController> OnCustomerBecameRegular;
        public event Action<float> OnSatisfactionChanged; // 0-100

        // --- Spielzustand ---
        public event Action<GameState, GameState> OnGameStateChanged; // alt, neu
        public event Action<int> OnNewDayStarted; // Tagnummer
        public event Action<TimeOfDay> OnTimeOfDayChanged;

        // --- Räume ---
        public event Action<string> OnRoomUnlocked; // RoomID

        // --- Story & Synergien ---
        public event Action<string> OnStoryFlagSet; // Flag-Name
        public event Action<string, string> OnSynergyDiscovered; // Maschine1-ID, Maschine2-ID
        public event Action<GameEventData> OnGameEventStarted;
        public event Action<GameEventData> OnGameEventEnded;

        // --- Dialogsystem ---
        public event Action<DialogueData> OnDialogueStarted;
        public event Action OnDialogueEnded;

        // ---- Öffentliche Auslöse-Methoden ----

        public void TriggerMoneyChanged(decimal amount) => OnMoneyChanged?.Invoke(amount);
        public void TriggerMoneyAdded(decimal amount, string source) => OnMoneyAdded?.Invoke(amount, source);
        public void TriggerMoneySpent(decimal amount, string reason) => OnMoneySpent?.Invoke(amount, reason);
        public void TriggerDailyReport(DailyStats stats) => OnDailyReport?.Invoke(stats);
        public void TriggerBankruptcy() => OnBankruptcy?.Invoke();

        public void TriggerMachinePlaced(VendingMachine machine) => OnMachinePlaced?.Invoke(machine);
        public void TriggerMachineRemoved(VendingMachine machine) => OnMachineRemoved?.Invoke(machine);
        public void TriggerMachineSale(VendingMachine machine, float amount) => OnMachineSale?.Invoke(machine, amount);
        public void TriggerMachineStockEmpty(VendingMachine machine) => OnMachineStockEmpty?.Invoke(machine);
        public void TriggerMachineUpgraded(VendingMachine machine, int level) => OnMachineUpgraded?.Invoke(machine, level);
        public void TriggerMachineBroken(VendingMachine machine) => OnMachineBroken?.Invoke(machine);

        public void TriggerCustomerEntered(CustomerController customer) => OnCustomerEntered?.Invoke(customer);
        public void TriggerCustomerLeft(CustomerController customer) => OnCustomerLeft?.Invoke(customer);
        public void TriggerCustomerServed(CustomerController customer) => OnCustomerServed?.Invoke(customer);
        public void TriggerCustomerBecameRegular(CustomerController customer) => OnCustomerBecameRegular?.Invoke(customer);
        public void TriggerSatisfactionChanged(float value) => OnSatisfactionChanged?.Invoke(value);

        public void TriggerGameStateChanged(GameState oldState, GameState newState) => OnGameStateChanged?.Invoke(oldState, newState);
        public void TriggerNewDayStarted(int day) => OnNewDayStarted?.Invoke(day);
        public void TriggerTimeOfDayChanged(TimeOfDay time) => OnTimeOfDayChanged?.Invoke(time);

        public void TriggerRoomUnlocked(string roomId) => OnRoomUnlocked?.Invoke(roomId);
        public void TriggerStoryFlagSet(string flag) => OnStoryFlagSet?.Invoke(flag);
        public void TriggerSynergyDiscovered(string id1, string id2) => OnSynergyDiscovered?.Invoke(id1, id2);
        public void TriggerGameEventStarted(GameEventData data) => OnGameEventStarted?.Invoke(data);
        public void TriggerGameEventEnded(GameEventData data) => OnGameEventEnded?.Invoke(data);

        public void TriggerDialogueStarted(DialogueData data) => OnDialogueStarted?.Invoke(data);
        public void TriggerDialogueEnded() => OnDialogueEnded?.Invoke();
    }

    // Datenklassen die Event-Parameter transportieren
    [Serializable]
    public class DailyStats
    {
        public int Tag;
        public decimal Tagesumsatz;
        public decimal Tagesausgaben;
        public decimal Tagesgewinn;
        public int KundenAnzahl;
        public int VerkaufsAnzahl;
        public float DurchschnittlicheZufriedenheit;
    }

    [Serializable]
    public class GameEventData
    {
        public string EventName;
        public EventType Type;
        public float Duration;
    }
}
