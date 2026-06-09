using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Notification-Banner: fährt von oben herein, zeigt Event-Name + Icon für 5 Sekunden.
    public class EventNotificationPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI eventNameText;
        [SerializeField] private TextMeshProUGUI eventDescText;
        [SerializeField] private Image eventIcon;
        [SerializeField] private Image backgroundPanel;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private float displayDuration = 5f;
        [SerializeField] private float slideDistance = 120f;

        private static readonly Color ColorPositive = new Color(0.2f, 0.8f, 0.3f, 0.9f);
        private static readonly Color ColorNegative = new Color(0.9f, 0.2f, 0.2f, 0.9f);
        private static readonly Color ColorSpecial  = new Color(0.8f, 0.6f, 0.1f, 0.9f);
        private static readonly Color ColorNeutral  = new Color(0.4f, 0.5f, 0.6f, 0.9f);

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void Start()
        {
            VendoriumEventManager.Instance.OnGameEventStarted += ShowEvent;
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance != null)
                VendoriumEventManager.Instance.OnGameEventStarted -= ShowEvent;
        }

        private void ShowEvent(GameEventData data)
        {
            StopAllCoroutines();
            gameObject.SetActive(true);

            if (eventNameText != null) eventNameText.text = data.EventName;

            if (backgroundPanel != null)
            {
                backgroundPanel.color = data.Type switch
                {
                    EventType.Positive => ColorPositive,
                    EventType.Negative => ColorNegative,
                    EventType.Special  => ColorSpecial,
                    _                  => ColorNeutral
                };
            }

            StartCoroutine(AnimateIn());
        }

        private IEnumerator AnimateIn()
        {
            if (panelRect == null) yield break;

            // Von oben reinfahren
            Vector2 hiddenPos  = new Vector2(panelRect.anchoredPosition.x,  slideDistance);
            Vector2 visiblePos = new Vector2(panelRect.anchoredPosition.x,  0f);

            float t = 0f, dur = 0.3f;
            panelRect.anchoredPosition = hiddenPos;

            while (t < dur)
            {
                t += Time.deltaTime;
                panelRect.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, Mathf.SmoothStep(0f, 1f, t / dur));
                yield return null;
            }

            yield return new WaitForSeconds(displayDuration);

            // Wieder herausfahren
            t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                panelRect.anchoredPosition = Vector2.Lerp(visiblePos, hiddenPos, Mathf.SmoothStep(0f, 1f, t / dur));
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
