using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    // Scannt alle platzierten Automaten, prüft Nachbarschaften (2.5m Radius),
    // berechnet aktive Synergien und zeichnet goldene Verbindungslinien.
    public class SynergyManager : Singleton<SynergyManager>
    {
        [Header("Synergie-Regeln")]
        [SerializeField] private List<SynergyRule> allRules = new List<SynergyRule>();

        [Header("Verbindungslinie")]
        [SerializeField] private Material lineMaterial;
        [SerializeField] private float synergyRadius = 2.5f;
        [SerializeField] private float lineWidth = 0.04f;

        // Aktive Synergien: Paar-Key → LineRenderer
        private Dictionary<string, SynergyConnection> _activeConnections =
            new Dictionary<string, SynergyConnection>();

        // Bereits entdeckte Synergien (persistiert via SaveManager)
        private HashSet<string> _discoveredSynergies = new HashSet<string>();

        private void Start()
        {
            VendoriumEventManager.Instance.OnMachinePlaced  += _ => ScheduleRescan();
            VendoriumEventManager.Instance.OnMachineRemoved += _ => ScheduleRescan();
            StartCoroutine(PeriodicRescan());
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance == null) return;
            VendoriumEventManager.Instance.OnMachinePlaced  -= _ => ScheduleRescan();
            VendoriumEventManager.Instance.OnMachineRemoved -= _ => ScheduleRescan();
        }

        private bool _rescanScheduled = false;

        private void ScheduleRescan()
        {
            if (!_rescanScheduled)
                StartCoroutine(RescanNextFrame());
        }

        private IEnumerator RescanNextFrame()
        {
            _rescanScheduled = true;
            yield return null;
            _rescanScheduled = false;
            RescanAllSynergies();
        }

        private IEnumerator PeriodicRescan()
        {
            while (true)
            {
                yield return new WaitForSeconds(5f);
                RescanAllSynergies();
            }
        }

        private void RescanAllSynergies()
        {
            var machines = MachineManager.Instance?.PlacedMachines;
            if (machines == null) return;

            // Alle alten Linien als "nicht gefunden" markieren
            var seen = new HashSet<string>();

            for (int i = 0; i < machines.Count; i++)
            {
                for (int j = i + 1; j < machines.Count; j++)
                {
                    var a = machines[i];
                    var b = machines[j];
                    if (a == null || b == null) continue;

                    float dist = Vector3.Distance(a.transform.position, b.transform.position);
                    if (dist > synergyRadius) continue;

                    SynergyRule rule = FindMatchingRule(a, b);
                    if (rule == null) continue;

                    string key = MakePairKey(a, b);
                    seen.Add(key);

                    // Multiplikator setzen
                    a.IncomeMultiplier = rule.IncomeMultiplier;
                    b.IncomeMultiplier = rule.IncomeMultiplier;

                    // Verbindungslinie erstellen oder aktualisieren
                    if (!_activeConnections.ContainsKey(key))
                    {
                        CreateConnection(key, a, b, rule);

                        // Erste Entdeckung?
                        if (!_discoveredSynergies.Contains(rule.RuleName))
                        {
                            _discoveredSynergies.Add(rule.RuleName);
                            VendoriumEventManager.Instance?.TriggerSynergyDiscovered(
                                a.Data?.MachineID ?? "", b.Data?.MachineID ?? "");
                            Debug.Log($"[SynergyManager] Neue Synergie entdeckt: {rule.RuleName}");
                        }
                    }
                }
            }

            // Nicht mehr aktive Verbindungen entfernen
            var toRemove = new List<string>();
            foreach (var kv in _activeConnections)
            {
                if (!seen.Contains(kv.Key))
                    toRemove.Add(kv.Key);
            }
            foreach (var key in toRemove)
                RemoveConnection(key);

            // Multiplikatoren nicht mehr aktiver Maschinen zurücksetzen
            if (machines != null)
                foreach (var m in machines)
                    if (m != null && !IsMachineInAnyConnection(m))
                        m.IncomeMultiplier = 1f;
        }

        private SynergyRule FindMatchingRule(VendingMachine a, VendingMachine b)
        {
            if (a.Data == null || b.Data == null) return null;

            foreach (var rule in allRules)
            {
                if (rule == null) continue;
                if (TagsMatch(a.Data.SynergyTags, rule.RequiredTagsA) &&
                    TagsMatch(b.Data.SynergyTags, rule.RequiredTagsB))
                    return rule;
                // Auch umgekehrt prüfen
                if (TagsMatch(a.Data.SynergyTags, rule.RequiredTagsB) &&
                    TagsMatch(b.Data.SynergyTags, rule.RequiredTagsA))
                    return rule;
            }
            return null;
        }

        private bool TagsMatch(string[] machineTags, string[] requiredTags)
        {
            if (machineTags == null || requiredTags == null || requiredTags.Length == 0) return false;
            foreach (var req in requiredTags)
                foreach (var tag in machineTags)
                    if (tag == req) return true;
            return false;
        }

        private void CreateConnection(string key, VendingMachine a, VendingMachine b, SynergyRule rule)
        {
            var go = new GameObject($"SynergyLine_{key}");
            go.transform.SetParent(transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = lineWidth;
            lr.endWidth   = lineWidth;
            lr.useWorldSpace = true;

            if (lineMaterial != null) lr.material = lineMaterial;
            lr.startColor = rule.LineColor;
            lr.endColor   = rule.LineColor;

            // Positionen auf Augenhöhe setzen
            lr.SetPosition(0, a.transform.position + Vector3.up * 1.5f);
            lr.SetPosition(1, b.transform.position + Vector3.up * 1.5f);

            _activeConnections[key] = new SynergyConnection
            {
                MachineA = a,
                MachineB = b,
                Rule = rule,
                LineRenderer = lr,
                LineObject = go
            };

            StartCoroutine(AnimateLine(lr));
        }

        private void RemoveConnection(string key)
        {
            if (!_activeConnections.TryGetValue(key, out var conn)) return;

            if (conn.LineObject != null)
                Destroy(conn.LineObject);

            _activeConnections.Remove(key);
        }

        // Gepunktete Animations-Effekt (Dash-Muster durch Material-Offset)
        private IEnumerator AnimateLine(LineRenderer lr)
        {
            if (lr == null || lr.material == null) yield break;

            float offset = 0f;
            while (lr != null)
            {
                offset += Time.deltaTime * 0.5f;
                lr.material.SetTextureOffset("_MainTex", new Vector2(offset, 0f));
                yield return null;
            }
        }

        private bool IsMachineInAnyConnection(VendingMachine m)
        {
            foreach (var kv in _activeConnections)
                if (kv.Value.MachineA == m || kv.Value.MachineB == m)
                    return true;
            return false;
        }

        private string MakePairKey(VendingMachine a, VendingMachine b)
        {
            uint idA = a.GetEntityId();
            uint idB = b.GetEntityId();
            return idA < idB ? $"{idA}_{idB}" : $"{idB}_{idA}";
        }

        public HashSet<string> GetDiscoveredSynergies() => _discoveredSynergies;

        public void LoadDiscoveredSynergies(List<string> synergies)
        {
            _discoveredSynergies.Clear();
            foreach (var s in synergies) _discoveredSynergies.Add(s);
        }
    }

    public class SynergyConnection
    {
        public VendingMachine MachineA;
        public VendingMachine MachineB;
        public SynergyRule Rule;
        public LineRenderer LineRenderer;
        public GameObject LineObject;
    }
}
