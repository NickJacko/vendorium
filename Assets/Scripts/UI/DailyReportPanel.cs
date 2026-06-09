using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Tagesbericht-Panel — erscheint automatisch am Ende jedes Spieltages.
    // Zeigt Umsatz, Gewinn, Kunden, Zufriedenheit und einen Wochenverlauf-Balken.
    public class DailyReportPanel : MonoBehaviour
    {
        [Header("Tages-Header")]
        [SerializeField] private TextMeshProUGUI dayTitleText;      // "Tag 3 abgeschlossen"
        [SerializeField] private TextMeshProUGUI subtitleText;      // Wochentag-Name

        [Header("Statistiken")]
        [SerializeField] private TextMeshProUGUI revenueText;
        [SerializeField] private TextMeshProUGUI expensesText;
        [SerializeField] private TextMeshProUGUI profitText;
        [SerializeField] private TextMeshProUGUI customerCountText;
        [SerializeField] private TextMeshProUGUI satisfactionText;
        [SerializeField] private Slider satisfactionBar;

        [Header("Wochenverlauf (7 Balken)")]
        [SerializeField] private Image[] weekBars = new Image[7];
        [SerializeField] private TextMeshProUGUI[] weekBarLabels = new TextMeshProUGUI[7];

        [Header("Weiter-Button")]
        [SerializeField] private Button continueButton;

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeInDuration = 0.5f;

        private static readonly string[] WOCHENTAGE =
            { "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag", "Sonntag" };

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            continueButton?.onClick.AddListener(OnContinueClicked);
            gameObject.SetActive(false);
        }

        private void Start()
        {
            VendoriumEventManager.Instance.OnDailyReport += ShowReport;
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance != null)
                VendoriumEventManager.Instance.OnDailyReport -= ShowReport;
        }

        private void ShowReport(DailyStats stats)
        {
            gameObject.SetActive(true);
            PopulateUI(stats);
            StartCoroutine(FadeIn());
        }

        private void PopulateUI(DailyStats stats)
        {
            int dayIndex = (stats.Tag - 1) % 7;
            string wochentag = WOCHENTAGE[dayIndex];

            if (dayTitleText  != null) dayTitleText.text  = $"Tag {stats.Tag} abgeschlossen";
            if (subtitleText  != null) subtitleText.text  = wochentag;

            if (revenueText  != null) revenueText.text  = $"{stats.Tagesumsatz:F2} €";
            if (expensesText != null) expensesText.text = $"−{stats.Tagesausgaben:F2} €";

            // Gewinn-Farbe: grün wenn positiv, rot wenn negativ
            if (profitText != null)
            {
                profitText.text  = $"{stats.Tagesgewinn:F2} €";
                profitText.color = stats.Tagesgewinn >= 0m ? Color.green : Color.red;
            }

            if (customerCountText != null) customerCountText.text = $"{stats.KundenAnzahl} Kunden";

            float satisfaction = stats.DurchschnittlicheZufriedenheit;
            if (satisfactionText != null) satisfactionText.text = $"{satisfaction:F0} %";
            if (satisfactionBar  != null) satisfactionBar.value = satisfaction / 100f;

            RefreshWeekBars();
        }

        private void RefreshWeekBars()
        {
            var weekData = EconomyManager.Instance?.GetWeeklyRevenue();
            if (weekData == null || weekBars == null) return;

            // Maxwert für Skalierung finden
            decimal maxRevenue = 1m;
            foreach (decimal d in weekData)
                if (d > maxRevenue) maxRevenue = d;

            for (int i = 0; i < weekBars.Length && i < weekData.Length; i++)
            {
                float height = (float)(weekData[i] / maxRevenue);

                if (weekBars[i] != null)
                {
                    var rect = weekBars[i].rectTransform;
                    rect.localScale = new Vector3(1f, height, 1f);
                    weekBars[i].color = i == (EconomyManager.Instance.CurrentDay - 1) % 7
                        ? Color.yellow : new Color(0.3f, 0.7f, 1f);
                }

                if (weekBarLabels != null && i < weekBarLabels.Length && weekBarLabels[i] != null)
                    weekBarLabels[i].text = WOCHENTAGE[i][..2]; // "Mo", "Di", usw.
            }
        }

        private IEnumerator FadeIn()
        {
            canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = t / fadeInDuration;
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private void OnContinueClicked()
        {
            gameObject.SetActive(false);
            UIManager.Instance?.ShowHUD();
        }
    }
}
