using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Zeigt 3 Save-Slots zur Auswahl. Wird vom Haupt-Menü und Pause-Menü verwendet.
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("Slot-Panels (3 Stück)")]
        [SerializeField] private List<SaveSlotButton> slotButtons = new List<SaveSlotButton>();

        [Header("Schließen")]
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject panel;

        private Action<int> _onSlotSelected;

        private void Awake()
        {
            closeButton?.onClick.AddListener(Close);
            panel?.SetActive(false);

            for (int i = 0; i < slotButtons.Count; i++)
            {
                int captured = i;
                slotButtons[i]?.Init(captured, () => SelectSlot(captured));
            }
        }

        public void Open(Action<int> onSlotSelected)
        {
            _onSlotSelected = onSlotSelected;
            panel?.SetActive(true);
            RefreshSlots();
        }

        public void Close()
        {
            panel?.SetActive(false);
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < slotButtons.Count; i++)
            {
                var info = SaveManager.Instance?.GetSlotInfo(i);
                slotButtons[i]?.Refresh(info);
            }
        }

        private void SelectSlot(int slot)
        {
            Close();
            _onSlotSelected?.Invoke(slot);
        }
    }

    // Helper-Component für einen einzelnen Slot-Button
    [Serializable]
    public class SaveSlotButton
    {
        public Button Button;
        public TextMeshProUGUI SlotLabel;       // "Slot 1"
        public TextMeshProUGUI InfoText;        // "Tag 5 — 1250 € — 3 Automaten"
        public TextMeshProUGUI DateText;        // "09.06.2026 14:32"
        public GameObject EmptyIndicator;       // "Leer" Label
        public GameObject DeleteButton;

        public void Init(int slot, Action onClick)
        {
            if (SlotLabel != null) SlotLabel.text = $"Slot {slot + 1}";
            Button?.onClick.AddListener(() => onClick?.Invoke());
        }

        public void Refresh(SaveSlotInfo info)
        {
            bool hasData = info != null;

            if (EmptyIndicator != null) EmptyIndicator.SetActive(!hasData);
            if (DeleteButton   != null) DeleteButton.SetActive(hasData);

            if (!hasData)
            {
                if (InfoText != null) InfoText.text = "Leerer Slot";
                if (DateText != null) DateText.text = "";
                return;
            }

            if (InfoText != null)
                InfoText.text = $"Tag {info.Tag}  ·  {info.Geld:F0} €  ·  {info.AutomatenAnzahl} Automaten";

            if (DateText != null)
                DateText.text = info.Speicherdatum.ToString("dd.MM.yyyy HH:mm");
        }
    }
}
