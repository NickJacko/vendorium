using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // UI-Panel für den Kassierer-Modus.
    // Zeigt Warteschlange, aktuellen Kunden, Preis, Kassieren-Button.
    public class CashRegisterUI : MonoBehaviour
    {
        [Header("Warteschlange")]
        [SerializeField] private TextMeshProUGUI queueText;

        [Header("Aktueller Kunde")]
        [SerializeField] private TextMeshProUGUI customerNameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI paymentText;
        [SerializeField] private TextMeshProUGUI changeText;

        [Header("Tagesbilanz")]
        [SerializeField] private TextMeshProUGUI todayIncomeText;
        [SerializeField] private TextMeshProUGUI servedCountText;

        [Header("Buttons")]
        [SerializeField] private Button kassierenButton;
        [SerializeField] private Button exitButton;

        [Header("Wechselgeld-Minigame")]
        [SerializeField] private ChangeMakingMinigame changeMinigame;

        private void Awake()
        {
            kassierenButton?.onClick.AddListener(OnKassierenClicked);
            exitButton?.onClick.AddListener(OnExitClicked);
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;

            RefreshQueue();
            RefreshCustomerInfo();
            RefreshDailyStats();

            if (Input.GetKeyDown(KeyCode.E) && kassierenButton != null && kassierenButton.interactable)
                OnKassierenClicked();

            if (Input.GetKeyDown(KeyCode.Escape))
                OnExitClicked();
        }

        private void RefreshQueue()
        {
            if (queueText == null || CashRegisterManager.Instance == null) return;
            int q = CashRegisterManager.Instance.QueueLength;
            queueText.text = q == 0 ? "Warteschlange leer" : $"Warteschlange: {q} Kunde(n)";
        }

        private void RefreshCustomerInfo()
        {
            var customer = CashRegisterManager.Instance?.CurrentCustomer;
            bool hasCustomer = customer != null;

            if (kassierenButton != null)
                kassierenButton.interactable = hasCustomer;

            if (!hasCustomer)
            {
                if (customerNameText != null) customerNameText.text = "— Kein Kunde —";
                if (priceText        != null) priceText.text        = "";
                if (paymentText      != null) paymentText.text      = "";
                if (changeText       != null) changeText.text       = "";
                return;
            }

            if (customerNameText != null)
                customerNameText.text = customer.Data?.CustomerTypeName ?? "Kunde";
        }

        private void RefreshDailyStats()
        {
            if (CashRegisterManager.Instance == null) return;

            if (todayIncomeText != null)
                todayIncomeText.text = $"Heute kassiert: {EconomyManager.Instance?.TodayIncome:F2} €";

            if (servedCountText != null)
                servedCountText.text = $"Bedient: {CashRegisterManager.Instance.TodayServedCount}";
        }

        private void OnKassierenClicked()
        {
            var transaction = CashRegisterManager.Instance?.ProcessCurrentCustomer();
            if (transaction == null) return;

            if (transaction.RequiresChangeGame && changeMinigame != null)
            {
                // Minigame aktivieren — es ruft FinalizeTransaction auf wenn fertig
                changeMinigame.StartMinigame(transaction);
            }
            else
            {
                // Direkt abschließen
                if (priceText   != null) priceText.text   = $"{transaction.Price:F2} €";
                if (paymentText != null) paymentText.text = $"{transaction.Payment:F2} €";
                if (changeText  != null) changeText.text  = $"Wechselgeld: {transaction.Change:F2} €";
            }
        }

        private void OnExitClicked()
        {
            CashRegisterManager.Instance?.ExitCashierMode();
        }
    }
}
