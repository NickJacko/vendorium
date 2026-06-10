using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Vendorium
{
    // Vollständiger Kundencontroller mit NavMeshAgent.
    // States: Entering → Browsing → WalkingToMachine → WaitingInQueue → Buying → Leaving
    // Prefab: Capsule (Body) + Sphere (Kopf) + NavMeshAgent + AudioSource
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerController : MonoBehaviour
    {
        [Header("Daten")]
        [SerializeField] private CustomerData data;

        [Header("Emotionen (Partikel-Icons über Kopf)")]
        [SerializeField] private ParticleSystem emotionHappy;
        [SerializeField] private ParticleSystem emotionNeutral;
        [SerializeField] private ParticleSystem emotionAnnoyed;
        [SerializeField] private Transform emotionAnchor;

        // Laufzustand
        private CustomerState _state = CustomerState.Entering;
        private CustomerEmotion _emotion = CustomerEmotion.Neutral;
        private NavMeshAgent _agent;

        // Kauf-Planung
        private List<VendingMachine> _plannedMachines = new List<VendingMachine>();
        private VendingMachine _currentTargetMachine;
        private int _purchasesThisVisit = 0;
        private float _satisfactionScore = 70f;
        private float _waitTime = 0f;

        // Stammkunden-Tracking
        private string _customerId;
        private CustomerType _type;

        public CustomerData Data => data;
        public CustomerState State => _state;
        public float SatisfactionScore => _satisfactionScore;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _customerId = System.Guid.NewGuid().ToString();
        }

        private void Start()
        {
            if (data != null)
            {
                _agent.speed = data.WalkSpeed;
                _type = data.Type;
            }

            // NavMeshAgent-Performance: Update-Intervall reduzieren
            _agent.updatePosition = true;
            _agent.updateRotation = true;

            CustomerManager.Instance?.RecordCustomerVisit(_customerId, _type);
            StartCoroutine(BehaviourRoutine());
        }

        private IEnumerator BehaviourRoutine()
        {
            // 1. Eintreten: zum Ladeninneren laufen
            yield return StartCoroutine(EnterShop());

            // 2. Maschinen auswählen
            PlanVisit();

            // 3. Alle geplanten Maschinen abarbeiten
            while (_plannedMachines.Count > 0 && _purchasesThisVisit < GetMaxPurchases())
            {
                _currentTargetMachine = _plannedMachines[0];
                _plannedMachines.RemoveAt(0);

                if (_currentTargetMachine == null || _currentTargetMachine.State == MachineState.Broken)
                    continue;

                yield return StartCoroutine(WalkToMachine(_currentTargetMachine));
                yield return StartCoroutine(BuyFromMachine(_currentTargetMachine));
            }

            // 4. Verlassen
            yield return StartCoroutine(LeaveShop());
        }

        private IEnumerator EnterShop()
        {
            SetState(CustomerState.Entering);
            // Kurz warten bis NavMesh initialisiert ist
            yield return new WaitForSeconds(0.3f);
        }

        private void PlanVisit()
        {
            SetState(CustomerState.Browsing);
            _plannedMachines.Clear();

            var allMachines = MachineManager.Instance?.PlacedMachines;
            if (allMachines == null) return;

            // Maschinen nach Präferenz sortieren
            foreach (var machine in allMachines)
            {
                if (machine == null || machine.State != MachineState.Active) continue;
                if (!MatchesPreference(machine)) continue;
                if (UnityEngine.Random.value < GetPurchaseProbability())
                    _plannedMachines.Add(machine);
            }

            // Zufällige Reihenfolge
            Shuffle(_plannedMachines);
        }

        private IEnumerator WalkToMachine(VendingMachine machine)
        {
            SetState(CustomerState.WalkingToMachine);

            // Zielposition leicht vor der Maschine
            Vector3 target = machine.transform.position + machine.transform.forward * 1.2f;
            _agent.SetDestination(target);

            _waitTime = 0f;
            while (!HasReachedDestination())
            {
                _waitTime += Time.deltaTime;
                if (_waitTime > 20f) yield break; // Timeout
                yield return null;
            }
        }

        private IEnumerator BuyFromMachine(VendingMachine machine)
        {
            if (machine.State != MachineState.Active || machine.CurrentStock <= 0)
            {
                ModifySatisfaction(-5f); // Leere Maschine: leichte Enttäuschung
                SetEmotion(CustomerEmotion.Annoyed);
                yield break;
            }

            SetState(CustomerState.WaitingInQueue);

            // Kauf-Animation: kurz warten
            yield return new WaitForSeconds(UnityEngine.Random.Range(1.5f, 3.0f));

            SetState(CustomerState.Buying);
            _purchasesThisVisit++;

            // Zufriedenheit steigern
            ModifySatisfaction(10f);
            SetEmotion(CustomerEmotion.Happy);

            VendoriumEventManager.Instance?.TriggerCustomerServed(this);

            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator LeaveShop()
        {
            SetState(CustomerState.Leaving);

            // Zum Ausgang navigieren (Spawn-Position des CustomerSpawners)
            if (CustomerManager.Instance != null)
            {
                // Zum nächsten NavMesh-Rand navigieren (Ausgang)
                _agent.SetDestination(GetExitPosition());

                float timeout = 20f;
                while (!HasReachedDestination() && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
            }

            VendoriumEventManager.Instance?.TriggerCustomerLeft(this);
            CustomerManager.Instance?.ReturnToPool(this);
        }

        // --- Hilfsmethoden ---

        private bool MatchesPreference(VendingMachine machine)
        {
            if (data == null || data.PreferredMachineTags == null || data.PreferredMachineTags.Length == 0)
                return true;

            if (machine.Data == null || machine.Data.SynergyTags == null) return false;

            foreach (var preferredTag in data.PreferredMachineTags)
                foreach (var machineTag in machine.Data.SynergyTags)
                    if (preferredTag == machineTag) return true;

            return false;
        }

        private float GetPurchaseProbability() => data != null ? data.PurchaseProbability : 0.7f;
        private int GetMaxPurchases() => data != null ? data.MaxPurchasesPerVisit : 2;

        private bool HasReachedDestination()
        {
            if (!_agent.isOnNavMesh) return true;
            if (_agent.pathPending) return false;
            return _agent.remainingDistance <= _agent.stoppingDistance + 0.1f;
        }

        private Vector3 GetExitPosition()
        {
            // Exitposition = leicht vor dem Eingang (vorne mittig, außerhalb des Ladens)
            return new Vector3(0f, 0f, -10f);
        }

        private void SetState(CustomerState newState)
        {
            _state = newState;
        }

        private void ModifySatisfaction(float delta)
        {
            _satisfactionScore = Mathf.Clamp(_satisfactionScore + delta, 0f, 100f);
            CustomerManager.Instance?.UpdateSatisfaction(delta * 0.01f);

            if (_satisfactionScore > 70f)      SetEmotion(CustomerEmotion.Happy);
            else if (_satisfactionScore > 40f) SetEmotion(CustomerEmotion.Neutral);
            else                               SetEmotion(CustomerEmotion.Annoyed);
        }

        private void SetEmotion(CustomerEmotion emotion)
        {
            if (_emotion == emotion) return;
            _emotion = emotion;

            emotionHappy?.Stop();
            emotionNeutral?.Stop();
            emotionAnnoyed?.Stop();

            switch (emotion)
            {
                case CustomerEmotion.Happy:    emotionHappy?.Play();    break;
                case CustomerEmotion.Neutral:  emotionNeutral?.Play();  break;
                case CustomerEmotion.Annoyed:  emotionAnnoyed?.Play();  break;
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Wird aufgerufen wenn Objekt aus Pool reaktiviert wird
        private void OnEnable()
        {
            _purchasesThisVisit = 0;
            _satisfactionScore  = 70f;
            _state              = CustomerState.Entering;
            _emotion            = CustomerEmotion.Neutral;
            _plannedMachines.Clear();
        }
    }
}
