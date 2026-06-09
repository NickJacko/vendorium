using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Haupt-HUD oben rechts: Geld, Tageszeit, Tagnummer.
    // Geldanzeige animiert wenn Betrag steigt oder sinkt.
    public class HUD : MonoBehaviour
    {
        [Header("Geld")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private Color moneyIncreaseColor = new Color(0.2f, 1f, 0.4f);
        [SerializeField] private Color moneyDecreaseColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color moneyNormalColor   = Color.white;
        [SerializeField] private float flashDuration = 0.4f;

        [Header("Tageszeit & Tag")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI timeOfDayText;
        [SerializeField] private Image timeIcon;
        [SerializeField] private Sprite iconMorning;
        [SerializeField] private Sprite iconDay;
        [SerializeField] private Sprite iconEvening;
        [SerializeField] private Sprite iconNight;

        [Header("Tagesumsatz (Mini)")]
        [SerializeField] private TextMeshProUGUI todayIncomeText;

        private decimal _displayedMoney = 0m;
        private Coroutine _flashCoroutine;

        private void Start()
        {
            VendoriumEventManager.Instance.OnMoneyChanged   += OnMoneyChanged;
            VendoriumEventManager.Instance.OnMoneyAdded     += OnMoneyAdded;
            VendoriumEventManager.Instance.OnMoneySpent     += OnMoneySpent;
            VendoriumEventManager.Instance.OnNewDayStarted  += OnNewDayStarted;
            VendoriumEventManager.Instance.OnTimeOfDayChanged += OnTimeOfDayChanged;

            // Initialwerte setzen
            if (EconomyManager.Instance != null)
            {
                _displayedMoney = EconomyManager.Instance.GetCurrentMoney();
                RefreshMoneyText();
                RefreshDayText(EconomyManager.Instance.CurrentDay);
                RefreshTimeOfDay(EconomyManager.Instance.CurrentTimeOfDay);
            }
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance == null) return;
            VendoriumEventManager.Instance.OnMoneyChanged    -= OnMoneyChanged;
            VendoriumEventManager.Instance.OnMoneyAdded      -= OnMoneyAdded;
            VendoriumEventManager.Instance.OnMoneySpent      -= OnMoneySpent;
            VendoriumEventManager.Instance.OnNewDayStarted   -= OnNewDayStarted;
            VendoriumEventManager.Instance.OnTimeOfDayChanged -= OnTimeOfDayChanged;
        }

        private void Update()
        {
            // Tagesumsatz jede Sekunde aktualisieren
            if (todayIncomeText != null && EconomyManager.Instance != null)
                todayIncomeText.text = $"Heute: {EconomyManager.Instance.TodayIncome:F2} €";
        }

        private void OnMoneyChanged(decimal newAmount)
        {
            _displayedMoney = newAmount;
            RefreshMoneyText();
        }

        private void OnMoneyAdded(decimal amount, string source)
        {
            FlashMoney(moneyIncreaseColor);
        }

        private void OnMoneySpent(decimal amount, string reason)
        {
            FlashMoney(moneyDecreaseColor);
        }

        private void OnNewDayStarted(int day)
        {
            RefreshDayText(day);
        }

        private void OnTimeOfDayChanged(TimeOfDay time)
        {
            RefreshTimeOfDay(time);
        }

        private void RefreshMoneyText()
        {
            if (moneyText != null)
                moneyText.text = $"{_displayedMoney:F2} €";
        }

        private void RefreshDayText(int day)
        {
            if (dayText != null)
                dayText.text = $"Tag {day}";
        }

        private void RefreshTimeOfDay(TimeOfDay time)
        {
            if (timeOfDayText != null)
                timeOfDayText.text = GetTimeLabel(time);

            if (timeIcon != null)
                timeIcon.sprite = GetTimeIcon(time);
        }

        private void FlashMoney(Color flashColor)
        {
            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine(flashColor));
        }

        private IEnumerator FlashRoutine(Color targetColor)
        {
            if (moneyText == null) yield break;

            float half = flashDuration * 0.5f;
            float t = 0f;

            // Einblenden der Flash-Farbe
            while (t < half)
            {
                t += Time.deltaTime;
                moneyText.color = Color.Lerp(moneyNormalColor, targetColor, t / half);
                yield return null;
            }

            t = 0f;
            // Zurück zu Normal
            while (t < half)
            {
                t += Time.deltaTime;
                moneyText.color = Color.Lerp(targetColor, moneyNormalColor, t / half);
                yield return null;
            }

            moneyText.color = moneyNormalColor;
        }

        private string GetTimeLabel(TimeOfDay time) => time switch
        {
            TimeOfDay.Frueh       => "Frühmorgens",
            TimeOfDay.Morgen      => "Vormittag",
            TimeOfDay.Mittag      => "Mittag",
            TimeOfDay.Nachmittag  => "Nachmittag",
            TimeOfDay.Abend       => "Abend",
            TimeOfDay.Nacht       => "Nacht",
            _                     => ""
        };

        private Sprite GetTimeIcon(TimeOfDay time) => time switch
        {
            TimeOfDay.Frueh      => iconMorning,
            TimeOfDay.Morgen     => iconDay,
            TimeOfDay.Mittag     => iconDay,
            TimeOfDay.Nachmittag => iconDay,
            TimeOfDay.Abend      => iconEvening,
            TimeOfDay.Nacht      => iconNight,
            _                    => null
        };
    }
}
