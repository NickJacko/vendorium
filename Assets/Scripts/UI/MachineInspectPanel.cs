using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Öffnet sich wenn Spieler E bei einem Automaten drückt.
    // Spieler kann sich noch umsehen (Maus aktiv), WASD gesperrt, Cursor sichtbar.
    public class MachineInspectPanel : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI machineNameText;
        [SerializeField] private TextMeshProUGUI traitText;
        [SerializeField] private Image traitIcon;

        [Header("Stock")]
        [SerializeField] private Slider stockBar;
        [SerializeField] private TextMeshProUGUI stockText;

        [Header("Einnahmen")]
        [SerializeField] private TextMeshProUGUI incomePerSaleText;
        [SerializeField] private TextMeshProUGUI saleIntervalText;

        [Header("Upgrade")]
        [SerializeField] private TextMeshProUGUI upgradeLevelText;
        [SerializeField] private TextMeshProUGUI upgradeCostText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeButtonLabel;

        [Header("Auffüllen")]
        [SerializeField] private Button restockButton;
        [SerializeField] private TextMeshProUGUI restockCostText;

        [Header("Zustand")]
        [SerializeField] private TextMeshProUGUI stateText;

        [Header("Schließen")]
        [SerializeField] private Button closeButton;

        [Header("Animation")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private float slideInDuration = 0.25f;
        [SerializeField] private float slideOffscreenY = -600f;

        private VendingMachine _currentMachine;
        private CanvasGroup _canvasGroup;
        private Coroutine _slideCoroutine;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Buttons verdrahten
            upgradeButton?.onClick.AddListener(OnUpgradeClicked);
            restockButton?.onClick.AddListener(OnRestockClicked);
            closeButton?.onClick.AddListener(Close);

            gameObject.SetActive(false);
        }

        private void Start()
        {
            // Lauscht auf VendingMachine.Interact → UIManager.ShowMachineInspect ruft Open() auf
            // Das Panel registriert sich beim UIManager als Callback
        }

        private void Update()
        {
            if (_currentMachine == null) return;

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
                Close();

            // Countdown-Update
            UpdateIntervalDisplay();
        }

        public void Open(VendingMachine machine)
        {
            _currentMachine = machine;
            gameObject.SetActive(true);

            RefreshUI();
            SlideIn();

            // WASD sperren, Cursor freigeben, Maus bleibt aktiv
            GameManager.Instance?.SetGameState(GameState.ShopMode);
        }

        public void Close()
        {
            StartCoroutine(SlideOutAndHide());
            GameManager.Instance?.SetGameState(GameState.Playing);
        }

        private void RefreshUI()
        {
            if (_currentMachine == null || _currentMachine.Data == null) return;

            var data = _currentMachine.Data;
            int level = _currentMachine.UpgradeLevel;

            // Header
            if (machineNameText != null) machineNameText.text = data.MachineName;
            if (traitText != null)       traitText.text = GetTraitLabel(data.PersonalityTrait);

            // Stock
            int stock = _currentMachine.CurrentStock;
            int maxStock = _currentMachine.MaxStock;
            if (stockBar  != null) { stockBar.maxValue = maxStock; stockBar.value = stock; }
            if (stockText != null) stockText.text = $"{stock} / {maxStock}";

            // Einnahmen
            if (incomePerSaleText != null)
                incomePerSaleText.text = $"{data.GetIncomeAtLevel(level):F2} €";
            if (saleIntervalText != null)
                saleIntervalText.text = $"alle {data.GetIntervalAtLevel(level):F1}s";

            // Upgrade
            bool isMax = _currentMachine.IsMaxLevel();
            if (upgradeLevelText != null)
                upgradeLevelText.text = $"Level {level + 1} / {data.UpgradeLevels.Length + 1}";

            if (isMax)
            {
                if (upgradeCostText     != null) upgradeCostText.text = "MAX LEVEL";
                if (upgradeButtonLabel  != null) upgradeButtonLabel.text = "Maximiert";
                if (upgradeButton       != null) upgradeButton.interactable = false;
            }
            else
            {
                int cost = data.GetUpgradeCost(level + 1);
                if (upgradeCostText    != null) upgradeCostText.text = $"{cost} €";
                if (upgradeButtonLabel != null) upgradeButtonLabel.text = $"Upgraden ({cost}€)";
                bool canAfford = EconomyManager.Instance.GetCurrentMoney() >= cost;
                if (upgradeButton != null) upgradeButton.interactable = canAfford;
            }

            // Auffüllen-Button: nur anzeigen wenn leer
            bool isEmpty = _currentMachine.State == MachineState.Empty;
            if (restockButton != null) restockButton.gameObject.SetActive(isEmpty);
            if (restockCostText != null) restockCostText.text = "50 €";

            // Zustand
            if (stateText != null)
                stateText.text = GetStateLabel(_currentMachine.State);
        }

        private void UpdateIntervalDisplay()
        {
            // Für einen echten Countdown bräuchte VendingMachine einen Timer-Getter.
            // Placeholder: zeigt aktuelles Intervall statisch an.
        }

        private void OnUpgradeClicked()
        {
            if (_currentMachine == null) return;
            if (_currentMachine.Upgrade())
                RefreshUI();
        }

        private void OnRestockClicked()
        {
            if (_currentMachine == null) return;
            if (_currentMachine.Restock())
                RefreshUI();
        }

        private void SlideIn()
        {
            if (panelRect == null) return;
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideRoutine(slideOffscreenY, 0f));
        }

        private IEnumerator SlideOutAndHide()
        {
            if (panelRect != null)
                yield return StartCoroutine(SlideRoutine(0f, slideOffscreenY));
            gameObject.SetActive(false);
        }

        private IEnumerator SlideRoutine(float fromY, float toY)
        {
            float elapsed = 0f;
            Vector2 start = new Vector2(panelRect.anchoredPosition.x, fromY);
            Vector2 end   = new Vector2(panelRect.anchoredPosition.x, toY);

            while (elapsed < slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / slideInDuration);
                panelRect.anchoredPosition = Vector2.Lerp(start, end, t);
                yield return null;
            }
            panelRect.anchoredPosition = end;
        }

        private string GetTraitLabel(MachineTrait trait) => trait switch
        {
            MachineTrait.Reliable  => "Zuverlässig",
            MachineTrait.Moody     => "Launisch",
            MachineTrait.Generous  => "Großzügig",
            MachineTrait.Magnetic  => "Anziehend",
            MachineTrait.Fast      => "Schnell",
            _                      => "Unbekannt"
        };

        private string GetStateLabel(MachineState state) => state switch
        {
            MachineState.Active    => "Aktiv",
            MachineState.Empty     => "Leer — auffüllen!",
            MachineState.Broken    => "Defekt",
            MachineState.Upgrading => "Wird upgraded...",
            _                      => "Unbekannt"
        };
    }
}
