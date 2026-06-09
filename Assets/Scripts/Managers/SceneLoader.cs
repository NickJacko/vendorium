using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Vendorium
{
    // Singleton für asynchrone Szenen-Wechsel mit schwarzem Fade-In/Out.
    // Verwendung: SceneLoader.Instance.LoadScene("GameScene");
    public class SceneLoader : Singleton<SceneLoader>
    {
        [Header("Fade-Overlay")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.4f;

        [Header("Lade-Screen")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Slider loadingBar;

        private bool _isLoading = false;

        protected override void Awake()
        {
            base.Awake();

            // Falls kein Fade-Overlay im Inspector: automatisch erstellen
            if (fadeCanvasGroup == null)
                CreateFadeOverlay();
        }

        // Szene mit Fade laden
        public void LoadScene(string sceneName)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        public void LoadScene(int buildIndex)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneRoutine(buildIndex.ToString(), buildIndex));
        }

        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private IEnumerator LoadSceneRoutine(string sceneName, int buildIndex = -1)
        {
            _isLoading = true;

            // Fade OUT (Bild wird schwarz)
            yield return StartCoroutine(Fade(0f, 1f));

            // Speichern vor Szenenwechsel
            SaveManager.Instance?.SaveGame(0);

            // Lade-Screen einblenden
            if (loadingScreen != null) loadingScreen.SetActive(true);

            // Async laden
            AsyncOperation op = buildIndex >= 0
                ? SceneManager.LoadSceneAsync(buildIndex)
                : SceneManager.LoadSceneAsync(sceneName);

            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                if (loadingBar != null)
                    loadingBar.value = Mathf.Clamp01(op.progress / 0.9f);
                yield return null;
            }

            if (loadingBar != null) loadingBar.value = 1f;
            yield return new WaitForSecondsRealtime(0.2f);

            op.allowSceneActivation = true;
            yield return op; // Warten bis Szene vollständig geladen

            // Lade-Screen ausblenden
            if (loadingScreen != null) loadingScreen.SetActive(false);

            // Fade IN (Bild wird sichtbar)
            yield return StartCoroutine(Fade(1f, 0f));

            _isLoading = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeCanvasGroup == null) yield break;

            fadeCanvasGroup.alpha = from;
            fadeCanvasGroup.blocksRaycasts = true;

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = to;
            fadeCanvasGroup.blocksRaycasts = to > 0.5f;
        }

        // Erzeugt ein einfaches schwarzes Overlay-Canvas zur Laufzeit
        private void CreateFadeOverlay()
        {
            var canvasGO = new GameObject("FadeOverlay");
            canvasGO.transform.SetParent(transform);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var panel = new GameObject("FadePanel");
            panel.transform.SetParent(canvasGO.transform, false);

            var img = panel.AddComponent<Image>();
            img.color = Color.black;

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            fadeCanvasGroup = canvasGO.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}
