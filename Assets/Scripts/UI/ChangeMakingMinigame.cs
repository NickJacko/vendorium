using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vendorium
{
    // Wechselgeld-Minigame: Spieler muss durch Klicken auf Münz/Schein-Buttons
    // den korrekten Wechselgeld-Betrag zusammenstellen. Zeitlimit: 10 Sekunden.
    // Richtig: +10% Bonus. Falsch/Timeout: kleiner Reputations-Malus.
    public class ChangeMakingMinigame : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI instructionText;   // "Wechselgeld: 2.70 €"
        [SerializeField] private TextMeshProUGUI collectedText;     // aktuell geklickte Summe
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Slider timerBar;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TextMeshProUGUI resultText;        // "Richtig!" / "Falsch!"

        [Header("Münzen & Scheine (Buttons in Reihe)")]
        [SerializeField] private List<CoinButton> coinButtons = new List<CoinButton>();

        [Header("Einstellungen")]
        [SerializeField] private float timeLimit = 10f;

        private CashTransaction _currentTransaction;
        private float _collectedAmount = 0f;
        private float _elapsed = 0f;
        private bool _isRunning = false;
        private Coroutine _timerRoutine;

        private void Awake()
        {
            confirmButton?.onClick.AddListener(OnConfirm);
            resetButton?.onClick.AddListener(OnReset);

            foreach (var cb in coinButtons)
                cb?.Init(OnCoinClicked);

            gameObject.SetActive(false);
        }

        public void StartMinigame(CashTransaction transaction)
        {
            _currentTransaction = transaction;
            _collectedAmount = 0f;
            _elapsed = 0f;
            _isRunning = true;

            gameObject.SetActive(true);

            if (instructionText != null)
                instructionText.text = $"Wechselgeld: {transaction.Change:F2} €";

            UpdateCollectedDisplay();

            if (resultText != null) resultText.gameObject.SetActive(false);
            if (confirmButton != null) confirmButton.interactable = true;

            if (_timerRoutine != null) StopCoroutine(_timerRoutine);
            _timerRoutine = StartCoroutine(TimerRoutine());
        }

        private void OnCoinClicked(float value)
        {
            if (!_isRunning) return;
            _collectedAmount += value;
            UpdateCollectedDisplay();
        }

        private void OnReset()
        {
            _collectedAmount = 0f;
            UpdateCollectedDisplay();
        }

        private void OnConfirm()
        {
            if (!_isRunning) return;
            _isRunning = false;
            StopTimer();

            // Toleranz ±0.01 €
            bool correct = Mathf.Abs(_collectedAmount - _currentTransaction.Change) < 0.015f;
            FinishMinigame(correct);
        }

        private IEnumerator TimerRoutine()
        {
            while (_elapsed < timeLimit && _isRunning)
            {
                _elapsed += Time.deltaTime;
                float progress = 1f - (_elapsed / timeLimit);

                if (timerBar != null) timerBar.value = progress;
                if (timerText != null) timerText.text = $"{timeLimit - _elapsed:F1}s";

                // Farbe wechselt zu Rot wenn Zeit knapp wird
                if (timerBar != null)
                {
                    var fill = timerBar.fillRect?.GetComponent<Image>();
                    if (fill != null)
                        fill.color = Color.Lerp(Color.red, Color.green, progress);
                }

                yield return null;
            }

            if (_isRunning)
            {
                // Zeit abgelaufen
                _isRunning = false;
                FinishMinigame(correct: false);
            }
        }

        private void StopTimer()
        {
            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
                _timerRoutine = null;
            }
        }

        private void FinishMinigame(bool correct)
        {
            if (resultText != null)
            {
                resultText.gameObject.SetActive(true);
                resultText.text = correct ? "Richtig! +10% Bonus" : "Falsch!";
                resultText.color = correct ? Color.green : Color.red;
            }

            if (confirmButton != null) confirmButton.interactable = false;

            CashRegisterManager.Instance?.FinalizeTransaction(_currentTransaction, correct);
            StartCoroutine(HideAfterDelay(1.5f));
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }

        private void UpdateCollectedDisplay()
        {
            if (collectedText != null)
                collectedText.text = $"{_collectedAmount:F2} €";
        }
    }

    // Helper-Component für einen einzelnen Münz/Schein-Button
    [System.Serializable]
    public class CoinButton
    {
        public Button Button;
        public float Value; // z.B. 0.01, 0.50, 1.0, 2.0, 5.0, 10.0, 20.0

        public void Init(System.Action<float> onClick)
        {
            if (Button == null) return;
            float captured = Value;
            Button.onClick.AddListener(() => onClick?.Invoke(captured));
        }
    }
}
