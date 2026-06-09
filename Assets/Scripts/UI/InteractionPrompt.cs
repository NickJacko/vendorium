using System.Collections;
using TMPro;
using UnityEngine;

namespace Vendorium
{
    // Sitzt auf dem Interaction-Prompt UI-Element (unten in der Mitte).
    // Zeigt Text mit Fade-Animation an/ab.
    [RequireComponent(typeof(CanvasGroup))]
    public class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private float fadeSpeed = 5f;

        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;
        private bool _isVisible;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
        }

        public void Show(string text)
        {
            if (promptText != null)
                promptText.text = text;

            if (!_isVisible)
            {
                _isVisible = true;
                StartFade(1f);
            }
        }

        public void Hide()
        {
            if (_isVisible)
            {
                _isVisible = false;
                StartFade(0f);
            }
        }

        private void StartFade(float targetAlpha)
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
        }

        private IEnumerator FadeRoutine(float target)
        {
            while (!Mathf.Approximately(_canvasGroup.alpha, target))
            {
                _canvasGroup.alpha = Mathf.MoveTowards(
                    _canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
                yield return null;
            }
            _canvasGroup.alpha = target;
        }
    }
}
