using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Verwaltet die Dialogue-UI: Portrait, Name, Typewriter-Text, Choices.
    public class DialogueSystem : Singleton<DialogueSystem>
    {
        [Header("UI-Referenzen")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Typewriter")]
        [SerializeField] private float typewriterSpeed = 40f; // Zeichen pro Sekunde
        [SerializeField] private AudioClip typingSound;
        [SerializeField] private AudioSource typingAudioSource;

        private DialogueData _currentDialogue;
        private int _currentLineIndex = 0;
        private bool _isTyping = false;
        private bool _skipRequested = false;
        private Coroutine _typewriterCoroutine;

        protected override void Awake()
        {
            base.Awake();
            continueButton?.onClick.AddListener(OnContinueClicked);

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        public void StartDialogue(DialogueData data)
        {
            if (data == null) return;

            _currentDialogue  = data;
            _currentLineIndex = 0;

            dialoguePanel?.SetActive(true);
            GameManager.Instance?.SetGameState(GameState.Dialogue);
            VendoriumEventManager.Instance?.TriggerDialogueStarted(data);

            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if (_currentDialogue == null ||
                _currentLineIndex >= _currentDialogue.Lines.Count)
            {
                ShowChoicesOrEnd();
                return;
            }

            var line = _currentDialogue.Lines[_currentLineIndex];

            // Charakter-Info
            if (characterNameText != null) characterNameText.text = _currentDialogue.CharacterName;
            if (portraitImage     != null)
            {
                portraitImage.sprite  = _currentDialogue.Portrait;
                portraitImage.enabled = _currentDialogue.Portrait != null;
            }

            // Voice-Clip abspielen
            if (line.VoiceClip != null && typingAudioSource != null)
                typingAudioSource.PlayOneShot(line.VoiceClip);

            // Typewriter starten
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(line.Text));

            // Choices ausblenden während Text tippt
            ClearChoices();
            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }

        private IEnumerator TypewriterRoutine(string fullText)
        {
            _isTyping = true;
            _skipRequested = false;

            if (dialogueText != null) dialogueText.text = "";

            float delay = 1f / typewriterSpeed;

            foreach (char c in fullText)
            {
                if (_skipRequested)
                {
                    if (dialogueText != null) dialogueText.text = fullText;
                    break;
                }

                if (dialogueText != null) dialogueText.text += c;

                // Tipp-Sound bei sichtbaren Zeichen
                if (c != ' ' && c != '\n' && typingAudioSource != null && typingSound != null)
                    typingAudioSource.PlayOneShot(typingSound, 0.3f);

                yield return new WaitForSeconds(delay);
            }

            _isTyping = false;
        }

        private void OnContinueClicked()
        {
            if (_isTyping)
            {
                // Erst Skip-Request, beim nächsten Klick weitermachen
                _skipRequested = true;
                return;
            }

            _currentLineIndex++;
            ShowCurrentLine();
        }

        private void ShowChoicesOrEnd()
        {
            if (_currentDialogue.Choices != null && _currentDialogue.Choices.Count > 0)
            {
                // Continue-Button ausblenden, Choices anzeigen
                if (continueButton != null) continueButton.gameObject.SetActive(false);
                ShowChoices();
            }
            else
            {
                EndDialogue();
            }
        }

        private void ShowChoices()
        {
            ClearChoices();
            if (choicesContainer == null || choiceButtonPrefab == null) return;

            foreach (var choice in _currentDialogue.Choices)
            {
                var btn = Instantiate(choiceButtonPrefab, choicesContainer);
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = choice.ChoiceText;

                var captured = choice;
                btn.GetComponent<Button>()?.onClick.AddListener(() => OnChoiceSelected(captured));
            }
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            if (!string.IsNullOrEmpty(choice.StoryFlagToSet))
                StoryManager.Instance?.SetFlag(choice.StoryFlagToSet);

            ClearChoices();

            if (choice.NextDialogue != null)
                StartDialogue(choice.NextDialogue);
            else
                EndDialogue();
        }

        private void ClearChoices()
        {
            if (choicesContainer == null) return;
            foreach (Transform child in choicesContainer)
                Destroy(child.gameObject);
        }

        private void EndDialogue()
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);

            dialoguePanel?.SetActive(false);
            _currentDialogue = null;

            VendoriumEventManager.Instance?.TriggerDialogueEnded();
            GameManager.Instance?.SetGameState(GameState.Playing);
        }
    }
}
