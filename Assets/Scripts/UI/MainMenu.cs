using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Haupt-Menü-Controller.
    // Buttons: Neues Spiel, Fortsetzen (deaktiviert wenn kein Save), Einstellungen, Credits, Beenden.
    public class MainMenu : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Panels")]
        [SerializeField] private SaveSlotUI saveSlotUI;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Animierter Hintergrund")]
        [SerializeField] private Animator backgroundAnimator;

        [Header("Logo")]
        [SerializeField] private CanvasGroup logoGroup;
        [SerializeField] private float logoFadeInDelay = 0.3f;

        private void Awake()
        {
            newGameButton?.onClick.AddListener(OnNewGame);
            continueButton?.onClick.AddListener(OnContinue);
            settingsButton?.onClick.AddListener(OnSettings);
            creditsButton?.onClick.AddListener(OnCredits);
            quitButton?.onClick.AddListener(OnQuit);
        }

        private void Start()
        {
            // "Fortsetzen" nur aktiv wenn ein Speicherstand existiert
            bool hasSave = SaveManager.Instance != null && SaveManager.Instance.SlotExists(0);
            if (continueButton != null)
                continueButton.interactable = hasSave;

            // Hintergrund-Animation starten
            backgroundAnimator?.SetTrigger("Start");

            // Logo einblenden
            if (logoGroup != null)
                StartCoroutine(FadeInLogo());

            // Hover-Sounds auf alle Buttons
            RegisterHoverSounds();
        }

        private void OnNewGame()
        {
            PlayClickSound();
            // Save-Slot auswählen lassen
            if (saveSlotUI != null)
                saveSlotUI.Open(onSlotSelected: slot =>
                {
                    SaveManager.Instance?.DeleteSave(slot);
                    SceneLoader.Instance?.LoadScene("GameScene");
                });
            else
                SceneLoader.Instance?.LoadScene("GameScene");
        }

        private void OnContinue()
        {
            PlayClickSound();
            if (saveSlotUI != null)
                saveSlotUI.Open(onSlotSelected: slot =>
                {
                    if (SaveManager.Instance != null && SaveManager.Instance.SlotExists(slot))
                        SceneLoader.Instance?.LoadScene("GameScene");
                });
            else
                SceneLoader.Instance?.LoadScene("GameScene");
        }

        private void OnSettings()
        {
            PlayClickSound();
            settingsPanel?.Open();
        }

        private void OnCredits()
        {
            PlayClickSound();
            creditsPanel?.SetActive(true);
        }

        private void OnQuit()
        {
            PlayClickSound();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void PlayClickSound()
        {
            AudioManager.Instance?.PlaySFX("button_click");
        }

        private void RegisterHoverSounds()
        {
            Button[] allButtons = { newGameButton, continueButton, settingsButton, creditsButton, quitButton };
            foreach (var btn in allButtons)
            {
                if (btn == null) continue;
                var trigger = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var entry = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
                };
                entry.callback.AddListener(_ => AudioManager.Instance?.PlaySFX("button_hover"));
                trigger.triggers.Add(entry);
            }
        }

        private System.Collections.IEnumerator FadeInLogo()
        {
            yield return new UnityEngine.WaitForSeconds(logoFadeInDelay);
            float t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                if (logoGroup != null) logoGroup.alpha = t / 0.8f;
                yield return null;
            }
            if (logoGroup != null) logoGroup.alpha = 1f;
        }
    }
}
