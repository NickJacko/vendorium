using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#pragma warning disable CS0618

namespace Vendorium
{
    public class MachineManager : Singleton<MachineManager>
    {
        [Header("Automat-Datenbank")]
        [SerializeField] private MachineDatabase machineDatabase;

        // Alle aktuell platzierten Automaten
        private List<VendingMachine> _placedMachines = new List<VendingMachine>();

        // Lookup per Instance-ID für schnellen Zugriff
        private Dictionary<int, VendingMachine> _machineById = new Dictionary<int, VendingMachine>();

        public MachineDatabase Database => machineDatabase;
        public IReadOnlyList<VendingMachine> PlacedMachines => _placedMachines;
        public int MachineCount => _placedMachines.Count;

        private void Start()
        {
            // Auf Events lauschen
            VendoriumEventManager.Instance.OnMachinePlaced += OnMachinePlaced;
            VendoriumEventManager.Instance.OnMachineRemoved += OnMachineRemoved;
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance == null) return;
            VendoriumEventManager.Instance.OnMachinePlaced -= OnMachinePlaced;
            VendoriumEventManager.Instance.OnMachineRemoved -= OnMachineRemoved;
        }

        // Registriert eine neu platzierte Maschine
        public void RegisterMachine(VendingMachine machine)
        {
            if (machine == null || _placedMachines.Contains(machine)) return;

            _placedMachines.Add(machine);
            _machineById[machine.GetEntityId()] = machine;

            VendoriumEventManager.Instance.TriggerMachinePlaced(machine);
            Debug.Log($"[MachineManager] Maschine registriert. Gesamt: {_placedMachines.Count}");
        }

        // Entfernt eine Maschine (z.B. verkauft oder zerstört)
        public void UnregisterMachine(VendingMachine machine)
        {
            if (machine == null) return;

            _placedMachines.Remove(machine);
            _machineById.Remove(machine.GetEntityId());

            VendoriumEventManager.Instance.TriggerMachineRemoved(machine);
        }

        // Gibt alle Maschinen eines bestimmten Typs zurück (per Tag)
        public List<VendingMachine> GetMachinesByTag(string tag)
        {
            return _placedMachines
                .Where(m => m != null)
                .ToList();
            // Wird nach vollständiger VendingMachine-Implementierung nach Tag gefiltert
        }

        // Berechnet die Gesamteinnahmen aller Automaten (Simulation für UI)
        public float GetTotalIncomePerMinute()
        {
            // Wird nach vollständiger VendingMachine-Implementierung berechnet
            return 0f;
        }

        // Prüft ob an einer Position bereits eine Maschine steht (für Placement)
        public bool IsPositionOccupied(Vector3 position, float radius = 0.6f)
        {
            foreach (var machine in _placedMachines)
            {
                if (machine == null) continue;
                if (Vector3.Distance(machine.transform.position, position) < radius)
                    return true;
            }
            return false;
        }

        // Gibt Maschinen zurück die sich in einem Radius befinden (für Synergie-Prüfung)
        public List<VendingMachine> GetMachinesInRadius(Vector3 center, float radius)
        {
            return _placedMachines
                .Where(m => m != null && Vector3.Distance(m.transform.position, center) <= radius)
                .ToList();
        }

        // Bereinigt null-Referenzen (falls Maschinen ohne UnregisterMachine zerstört wurden)
        public void CleanupNullMachines()
        {
            int removed = _placedMachines.RemoveAll(m => m == null);
            if (removed > 0)
                Debug.LogWarning($"[MachineManager] {removed} null-Referenzen bereinigt.");
        }

        private void OnMachinePlaced(VendingMachine machine) { }
        private void OnMachineRemoved(VendingMachine machine) { }

        // Gibt MachineData per ID aus der Datenbank zurück
        public MachineData GetMachineDataById(string machineId)
        {
            if (machineDatabase == null) return null;
            return machineDatabase.GetById(machineId);
        }
    }
}
