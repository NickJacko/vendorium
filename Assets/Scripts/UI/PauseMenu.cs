using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Pause-Menü (Escape im Spiel).
    // Buttons: Weiterspielen, Einstellungen, Hauptmenü, Beenden.
    // Time.timeScale wird auf 0 gesetzt — alle Coroutines mit WaitForSecondsRealtime nutzen.
    public class PauseMenu : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        [Header("Panels")]
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private SaveSlotUI saveSlotUI;

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeInDuration = 0.15f;

        private void Awake()
        {
            resumeButton?.onClick.AddListener(OnResume);
            settingsButton?.onClick.AddListener(OnSettings);
            mainMenuButton?.onClick.AddListener(OnMainMenu);
            quitButton?.onClick.AddListener(OnQuit);

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            gameObject.SetActive(false);
        }

        private void Start()
        {
            VendoriumEventManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance != null)
                VendoriumEventManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.Paused)
                Show();
            else if (oldState == GameState.Paused)
                Hide();
        }

        private void Show()
        {
            gameObject.SetActive(true);
            StartCoroutine(FadeIn());
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnResume()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            GameManager.Instance?.ResumeGame();
        }

        private void OnSettings()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            settingsPanel?.Open();
        }

        private void OnMainMenu()
        {
            AudioManager.Instance?.PlaySFX("button_click");

            // Speichern vor dem Verlassen
            if (saveSlotUI != null)
            {
                saveSlotUI.Open(slot =>
                {
                    SaveManager.Instance?.SaveGame(slot);
                    Time.timeScale = 1f;
                    SceneLoader.Instance?.LoadScene("MainMenu");
                });
            }
            else
            {
                SaveManager.Instance?.SaveGame(0);
                Time.timeScale = 1f;
                SceneLoader.Instance?.LoadScene("MainMenu");
            }
        }

        private void OnQuit()
        {
            AudioManager.Instance?.PlaySFX("button_click");
            SaveManager.Instance?.SaveGame(0);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private System.Collections.IEnumerator FadeIn()
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
    }
}
