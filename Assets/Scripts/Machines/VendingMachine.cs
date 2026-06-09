using System;
using System.Collections;
using UnityEngine;

namespace Vendorium
{
    // Vollständige Automat-Implementierung. Erbt von der Platzhalter-Klasse in VendoriumEventManager
    // und implementiert IInteractable.
    // Prefab-Aufbau:
    //   Root (VendingMachine.cs, Collider, Light)
    //     └── CustomerTrigger (Sphere Trigger, Radius 2m, MachineTriggerZone.cs)
    public class VendingMachine : MonoBehaviour, IInteractable
    {
        [Header("Daten")]
        [SerializeField] private MachineData machineData;

        [Header("Zustand")]
        [SerializeField] private MachineState currentState = MachineState.Active;
        [SerializeField] private int currentStock;
        [SerializeField] private int upgradeLevel = 0;

        [Header("Visuals")]
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private Light statusLight;
        [SerializeField] private ParticleSystem coinParticles;

        [Header("Materialien je Zustand")]
        [SerializeField] private Material materialActive;
        [SerializeField] private Material materialEmpty;
        [SerializeField] private Material materialBroken;

        // Laufzeitwerte
        private float _incomeMultiplier = 1f;  // wird vom SynergyManager gesetzt
        private int _customersInRange = 0;
        private Coroutine _incomeRoutine;

        public MachineData Data => machineData;
        public MachineState State => currentState;
        public int UpgradeLevel => upgradeLevel;
        public int CurrentStock => currentStock;
        public int MaxStock => machineData != null ? machineData.MaxStock : 20;
        public float IncomeMultiplier { get => _incomeMultiplier; set => _incomeMultiplier = value; }

        // Events für MachineSalesEffect und UI
        public event Action<float> OnSale;
        public event Action OnStockEmpty;
        public event Action<PlayerController> OnPlayerInteract;

        private void Start()
        {
            if (machineData == null)
            {
                Debug.LogWarning($"[VendingMachine] Kein MachineData zugewiesen auf {gameObject.name}.");
                return;
            }

            currentStock = machineData.MaxStock;
            MachineManager.Instance?.RegisterMachine(this);
            UpdateVisuals();
            StartIncomeLoop();
        }

        private void OnDestroy()
        {
            MachineManager.Instance?.UnregisterMachine(this);
            StopIncomeLoop();
        }

        // --- IInteractable ---

        public void Interact(PlayerController player)
        {
            OnPlayerInteract?.Invoke(player);
            UIManager.Instance?.ShowMachineInspect();

            // MachineInspectPanel holt sich die Referenz per FindObjectOfType oder Event
            VendoriumEventManager.Instance?.TriggerMachinePlaced(this); // Panel lauscht darauf
        }

        public string GetInteractionText()
        {
            if (machineData == null) return "[E] Automat";
            return currentState switch
            {
                MachineState.Empty    => $"[E] {machineData.MachineName} — LEER",
                MachineState.Broken   => $"[E] {machineData.MachineName} — DEFEKT",
                MachineState.Upgrading => $"[E] {machineData.MachineName} — UPGRADE...",
                _                     => $"[E] {machineData.MachineName} inspizieren"
            };
        }

        public bool CanInteract() => currentState != MachineState.Upgrading;

        // --- Einnahmen-Loop ---

        private void StartIncomeLoop()
        {
            StopIncomeLoop();
            if (machineData != null)
                _incomeRoutine = StartCoroutine(IncomeRoutine());
        }

        private void StopIncomeLoop()
        {
            if (_incomeRoutine != null)
            {
                StopCoroutine(_incomeRoutine);
                _incomeRoutine = null;
            }
        }

        private IEnumerator IncomeRoutine()
        {
            while (true)
            {
                float interval = machineData.GetIntervalAtLevel(upgradeLevel);
                yield return new WaitForSeconds(interval);

                if (currentState != MachineState.Active) continue;
                if (currentStock <= 0) continue;
                if (_customersInRange <= 0) continue;

                ProcessSale();
            }
        }

        private void ProcessSale()
        {
            float baseIncome  = machineData.GetIncomeAtLevel(upgradeLevel);
            float finalIncome = ApplyTraitEffect(baseIncome * _incomeMultiplier);

            if (finalIncome <= 0f) return; // Moody-Trait: kein Verkauf

            currentStock--;

            float amount = finalIncome;
            EconomyManager.Instance?.AddMoney((decimal)amount, machineData.MachineName, isPassive: true);

            OnSale?.Invoke(amount);
            VendoriumEventManager.Instance?.TriggerMachineSale(this, amount);

            coinParticles?.Play();

            if (currentStock <= 0)
                SetState(MachineState.Empty);
        }

        // Wendet Persönlichkeits-Trait auf Einnahmen an
        private float ApplyTraitEffect(float baseIncome)
        {
            if (upgradeLevel < 3 || machineData == null) return baseIncome;

            switch (machineData.PersonalityTrait)
            {
                case MachineTrait.Moody:
                    float roll = UnityEngine.Random.value;
                    if (roll < 0.1f)  return 0f;           // 10% kein Verkauf
                    if (roll > 0.9f)  return baseIncome * 2f; // 20% doppeltes Income
                    return baseIncome;

                case MachineTrait.Generous:
                    return baseIncome * 1.3f;

                case MachineTrait.Fast:
                    return baseIncome;  // Geschwindigkeit wird im GetIntervalAtLevel gehandhabt

                default:
                    return baseIncome;
            }
        }

        // --- Auffüllen & Upgraden ---

        public bool Restock(int amount = -1)
        {
            if (machineData == null) return false;

            int restockAmount = amount < 0 ? machineData.MaxStock : amount;
            int cost = 50; // Fest-Kosten, später konfigurierbar

            if (!EconomyManager.Instance.SpendMoney(cost, $"{machineData.MachineName} auffüllen"))
                return false;

            currentStock = Mathf.Min(currentStock + restockAmount, machineData.MaxStock);
            SetState(MachineState.Active);
            StartIncomeLoop();
            return true;
        }

        public bool Upgrade()
        {
            if (machineData == null) return false;
            if (upgradeLevel >= machineData.UpgradeLevels.Length) return false;

            int cost = machineData.GetUpgradeCost(upgradeLevel + 1);
            if (!EconomyManager.Instance.SpendMoney(cost, $"{machineData.MachineName} Upgrade L{upgradeLevel + 1}"))
                return false;

            SetState(MachineState.Upgrading);
            StartCoroutine(UpgradeAnimation());
            return true;
        }

        private IEnumerator UpgradeAnimation()
        {
            yield return new WaitForSeconds(2f);
            upgradeLevel++;
            SetState(MachineState.Active);
            VendoriumEventManager.Instance?.TriggerMachineUpgraded(this, upgradeLevel);
            StartIncomeLoop();
        }

        public bool IsMaxLevel() =>
            machineData == null || upgradeLevel >= machineData.UpgradeLevels.Length;

        // --- Zustands-Management ---

        private void SetState(MachineState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            UpdateVisuals();

            if (newState == MachineState.Empty)
            {
                StopIncomeLoop();
                OnStockEmpty?.Invoke();
                VendoriumEventManager.Instance?.TriggerMachineStockEmpty(this);
            }

            if (newState == MachineState.Broken)
            {
                StopIncomeLoop();
                VendoriumEventManager.Instance?.TriggerMachineBroken(this);
            }
        }

        private void UpdateVisuals()
        {
            if (statusLight != null)
            {
                statusLight.color = currentState switch
                {
                    MachineState.Active   => Color.green,
                    MachineState.Empty    => Color.red,
                    MachineState.Broken   => new Color(1f, 0.5f, 0f),
                    MachineState.Upgrading => Color.yellow,
                    _                     => Color.white
                };
                // Blinken wenn leer
                statusLight.enabled = currentState != MachineState.Upgrading;
            }

            Material mat = currentState switch
            {
                MachineState.Empty  => materialEmpty,
                MachineState.Broken => materialBroken,
                _                   => materialActive
            };

            if (mat == null) return;
            foreach (var rend in bodyRenderers)
                if (rend != null) rend.material = mat;
        }

        // Wird von MachineTriggerZone.cs aufgerufen
        public void OnCustomerEnterRange()  => _customersInRange++;
        public void OnCustomerLeaveRange()  => _customersInRange = Mathf.Max(0, _customersInRange - 1);

        // Für SaveManager
        public MachineSaveData GetSaveData() => new MachineSaveData
        {
            MachineId    = machineData != null ? machineData.MachineID : "",
            Position     = transform.position,
            Rotation     = transform.rotation,
            UpgradeLevel = upgradeLevel,
            CurrentStock = currentStock,
            State        = currentState
        };

        public void ApplySaveData(MachineSaveData data)
        {
            upgradeLevel = data.UpgradeLevel;
            currentStock = data.CurrentStock;
            currentState = data.State;
            UpdateVisuals();
            if (currentState == MachineState.Active && currentStock > 0)
                StartIncomeLoop();
        }
    }
}
