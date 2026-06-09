using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Tab-Menü für den Automaten-Shop. Öffnet sich mit Tab-Taste.
    // Zeigt alle kaufbaren Maschinen als Karten-Grid, startet nach Kauf den Placement-Mode.
    public class ShopScreen : MonoBehaviour
    {
        [Header("Datenbank")]
        [SerializeField] private MachineDatabase machineDatabase;

        [Header("Tab-Buttons")]
        [SerializeField] private Button tabSnacks;
        [SerializeField] private Button tabGetraenke;
        [SerializeField] private Button tabTech;
        [SerializeField] private Button tabPremium;

        [Header("Karten-Grid")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject machineCardPrefab;

        [Header("Guthaben")]
        [SerializeField] private TextMeshProUGUI balanceText;

        [Header("Info-Leiste (rechts)")]
        [SerializeField] private TextMeshProUGUI selectedNameText;
        [SerializeField] private TextMeshProUGUI selectedDescText;
        [SerializeField] private TextMeshProUGUI selectedPriceText;
        [SerializeField] private Button buyButton;

        private string _activeTab = "Snacks";
        private MachineData _selectedMachine;
        private List<GameObject> _spawnedCards = new List<GameObject>();

        private void Awake()
        {
            tabSnacks?.onClick.AddListener(() => SelectTab("Snacks"));
            tabGetraenke?.onClick.AddListener(() => SelectTab("Getraenke"));
            tabTech?.onClick.AddListener(() => SelectTab("Tech"));
            tabPremium?.onClick.AddListener(() => SelectTab("Premium"));
            buyButton?.onClick.AddListener(OnBuyClicked);

            gameObject.SetActive(false);
        }

        private void Start()
        {
            VendoriumEventManager.Instance.OnMoneyChanged += _ => RefreshBalance();
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance != null)
                VendoriumEventManager.Instance.OnMoneyChanged -= _ => RefreshBalance();
        }

        private void Update()
        {
            // Tab öffnet/schließt den Shop
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (gameObject.activeSelf) Close();
                else Open();
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            GameManager.Instance?.SetGameState(GameState.ShopMode);
            RefreshBalance();
            SelectTab(_activeTab);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            GameManager.Instance?.SetGameState(GameState.Playing);
        }

        private void SelectTab(string tab)
        {
            _activeTab = tab;
            _selectedMachine = null;
            RefreshGrid();
            RefreshInfoPanel(null);
        }

        private void RefreshGrid()
        {
            // Alte Karten entfernen
            foreach (var card in _spawnedCards)
                if (card != null) Destroy(card);
            _spawnedCards.Clear();

            if (machineDatabase == null || machineCardPrefab == null || cardContainer == null)
                return;

            foreach (var data in machineDatabase.AllMachines)
            {
                if (data == null) continue;
                if (!MatchesTab(data, _activeTab)) continue;

                var cardGO = Instantiate(machineCardPrefab, cardContainer);
                _spawnedCards.Add(cardGO);

                var card = cardGO.GetComponent<MachineCard>();
                if (card != null)
                    card.Setup(data, OnCardSelected);
                else
                    SetupCardFallback(cardGO, data);
            }
        }

        // Fallback wenn kein MachineCard-Script auf dem Prefab liegt
        private void SetupCardFallback(GameObject cardGO, MachineData data)
        {
            var texts = cardGO.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = data.MachineName;
            if (texts.Length > 1) texts[1].text = $"{data.PurchasePrice} €";

            var btn = cardGO.GetComponent<Button>();
            if (btn != null)
            {
                var captured = data;
                btn.onClick.AddListener(() => OnCardSelected(captured));
            }
        }

        private void OnCardSelected(MachineData data)
        {
            _selectedMachine = data;
            RefreshInfoPanel(data);
        }

        private void RefreshInfoPanel(MachineData data)
        {
            if (data == null)
            {
                if (selectedNameText != null) selectedNameText.text = "— Maschine auswählen —";
                if (selectedDescText != null) selectedDescText.text = "";
                if (selectedPriceText != null) selectedPriceText.text = "";
                if (buyButton != null) buyButton.interactable = false;
                return;
            }

            if (selectedNameText  != null) selectedNameText.text  = data.MachineName;
            if (selectedDescText  != null) selectedDescText.text  = data.Description;
            if (selectedPriceText != null) selectedPriceText.text = $"{data.PurchasePrice} €";

            bool canAfford = EconomyManager.Instance.GetCurrentMoney() >= data.PurchasePrice;
            if (buyButton != null) buyButton.interactable = canAfford;
        }

        private void OnBuyClicked()
        {
            if (_selectedMachine == null) return;

            bool bought = EconomyManager.Instance.SpendMoney(
                _selectedMachine.PurchasePrice,
                $"Kauf: {_selectedMachine.MachineName}"
            );

            if (!bought) return;

            Close();
            PlacementManager.Instance?.StartPlacement(_selectedMachine);
        }

        private void RefreshBalance()
        {
            if (balanceText != null)
                balanceText.text = $"Guthaben: {EconomyManager.Instance.GetCurrentMoney():F2} €";
        }

        private bool MatchesTab(MachineData data, string tab)
        {
            // Maschinen werden nach SynergyTags den Tabs zugeordnet
            if (data.SynergyTags == null || data.SynergyTags.Length == 0) return true;

            foreach (var tag in data.SynergyTags)
            {
                if (tab == "Snacks"    && (tag == "snack"))     return true;
                if (tab == "Getraenke" && (tag == "drink" || tag == "coffee")) return true;
                if (tab == "Tech"      && (tag == "tech"))      return true;
                if (tab == "Premium"   && (tag == "premium"))   return true;
            }
            return false;
        }
    }

    // Helper-Component für die Maschinen-Karte im Grid
    public class MachineCard : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button cardButton;

        private MachineData _data;
        private System.Action<MachineData> _onSelected;

        public void Setup(MachineData data, System.Action<MachineData> onSelected)
        {
            _data = data;
            _onSelected = onSelected;

            if (nameText  != null) nameText.text  = data.MachineName;
            if (priceText != null) priceText.text = $"{data.PurchasePrice} €";
            if (iconImage != null && data.Icon != null) iconImage.sprite = data.Icon;

            cardButton?.onClick.AddListener(() => _onSelected?.Invoke(_data));
        }
    }
}
