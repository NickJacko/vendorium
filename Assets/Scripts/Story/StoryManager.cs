using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    // Verwaltet Story-Fortschritt als Dictionary<string, bool> (StoryFlags).
    // Prüft nach jedem Spielereignis ob eine neue Story-Szene ausgelöst wird.
    public class StoryManager : Singleton<StoryManager>
    {
        [Header("Kapitel 1 — Dialoge")]
        [SerializeField] private DialogueData startingLetterDialogue;    // Onkel Viktors Brief
        [SerializeField] private DialogueData firstMachineDialogue;      // Oma Erika taucht auf
        [SerializeField] private DialogueData vossCallDialogue;          // Marco Voss ruft an (500€)

        private Dictionary<string, bool> _storyFlags = new Dictionary<string, bool>();
        private bool _gameStarted = false;

        private void Start()
        {
            VendoriumEventManager.Instance.OnMachinePlaced  += OnMachinePlaced;
            VendoriumEventManager.Instance.OnMoneyChanged   += OnMoneyChanged;
            VendoriumEventManager.Instance.OnNewDayStarted  += OnNewDayStarted;
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance == null) return;
            VendoriumEventManager.Instance.OnMachinePlaced -= OnMachinePlaced;
            VendoriumEventManager.Instance.OnMoneyChanged  -= OnMoneyChanged;
            VendoriumEventManager.Instance.OnNewDayStarted -= OnNewDayStarted;
        }

        // Wird von GameManager beim ersten Start aufgerufen
        public void TriggerGameStart()
        {
            if (_gameStarted) return;
            _gameStarted = true;

            if (!HasFlag("intro_letter_shown") && startingLetterDialogue != null)
                DialogueSystem.Instance?.StartDialogue(startingLetterDialogue);
        }

        public bool HasFlag(string flag) =>
            _storyFlags.TryGetValue(flag, out bool val) && val;

        public void SetFlag(string flag, bool value = true)
        {
            _storyFlags[flag] = value;
            VendoriumEventManager.Instance?.TriggerStoryFlagSet(flag);
        }

        public Dictionary<string, bool> GetAllFlags() => new Dictionary<string, bool>(_storyFlags);

        public void LoadFlags(Dictionary<string, bool> flags)
        {
            _storyFlags = flags ?? new Dictionary<string, bool>();
        }

        private void OnMachinePlaced(VendingMachine machine)
        {
            // Erster Automat → Oma Erika
            if (!HasFlag("first_machine_placed"))
            {
                SetFlag("first_machine_placed");
                if (firstMachineDialogue != null)
                    DialogueSystem.Instance?.StartDialogue(firstMachineDialogue);
            }
        }

        private void OnMoneyChanged(decimal money)
        {
            // 500€ → Marco Voss Anruf
            if (!HasFlag("voss_call_triggered") && money >= 500m)
            {
                SetFlag("voss_call_triggered");
                if (vossCallDialogue != null)
                    DialogueSystem.Instance?.StartDialogue(vossCallDialogue);
            }
        }

        private void OnNewDayStarted(int day)
        {
            // Erster Tag: Intro-Brief am Spielstart
            if (day == 1 && !HasFlag("intro_letter_shown"))
                TriggerGameStart();
        }
    }
}
