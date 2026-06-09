using UnityEngine;

namespace Vendorium
{
    // Haupt-Orchestrator. Initialisiert alle Systeme, hält den GameState.
    // Wird als erstes GameObject in jeder Spielszene als DontDestroyOnLoad erhalten.
    public class GameManager : Singleton<GameManager>
    {
        [Header("Manager-Referenzen (werden automatisch gefunden oder erstellt)")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private CustomerManager customerManager;
        [SerializeField] private MachineManager machineManager;
        [SerializeField] private VendoriumEventManager eventManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private AudioManager audioManager;

        [Header("Startzustand")]
        [SerializeField] private GameState startState = GameState.MainMenu;
        [SerializeField] private bool autoLoadSlot0OnStart = false;

        private GameState _currentState = GameState.MainMenu;
        private GameState _previousState = GameState.MainMenu;

        public GameState CurrentState => _currentState;
        public GameState PreviousState => _previousState;

        // Bequeme statische Shortcuts auf die Sub-Manager
        public static EconomyManager Economy => Instance?.economyManager;
        public static CustomerManager Customers => Instance?.customerManager;
        public static MachineManager Machines => Instance?.machineManager;
        public static UIManager UI => Instance?.uiManager;
        public static SaveManager Save => Instance?.saveManager;
        public static AudioManager Audio => Instance?.audioManager;

        protected override void Awake()
        {
            base.Awake();
            InitializeManagers();
        }

        private void Start()
        {
            // Erst wenn alle Manager bereit sind, den Startzustand setzen
            SetGameState(startState);

            if (autoLoadSlot0OnStart && SaveManager.Instance != null)
                SaveManager.Instance.LoadGame(0);
        }

        private void Update()
        {
            HandleGlobalInput();
        }

        private void InitializeManagers()
        {
            // Manager automatisch finden oder als Kinder erstellen
            economyManager = GetOrCreate<EconomyManager>(economyManager, "EconomyManager");
            customerManager = GetOrCreate<CustomerManager>(customerManager, "CustomerManager");
            machineManager  = GetOrCreate<MachineManager>(machineManager, "MachineManager");
            eventManager    = GetOrCreate<VendoriumEventManager>(eventManager, "EventManager");
            uiManager       = GetOrCreate<UIManager>(uiManager, "UIManager");
            saveManager     = GetOrCreate<SaveManager>(saveManager, "SaveManager");
            audioManager    = GetOrCreate<AudioManager>(audioManager, "AudioManager");

            Debug.Log("[GameManager] Alle Manager initialisiert.");
        }

        private T GetOrCreate<T>(T existing, string goName) where T : MonoBehaviour
        {
            if (existing != null) return existing;

            T found = FindObjectOfType<T>();
            if (found != null) return found;

            var go = new GameObject(goName);
            go.transform.SetParent(transform);
            return go.AddComponent<T>();
        }

        public void SetGameState(GameState newState)
        {
            if (_currentState == newState) return;

            _previousState = _currentState;
            _currentState = newState;

            VendoriumEventManager.Instance?.TriggerGameStateChanged(_previousState, _currentState);
            HandleStateTransition(_previousState, _currentState);

            Debug.Log($"[GameManager] Zustand: {_previousState} → {_currentState}");
        }

        private void HandleStateTransition(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;

                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;

                case GameState.CashierMode:
                case GameState.ShopMode:
                case GameState.Dialogue:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }
        }

        private void HandleGlobalInput()
        {
            // Escape: Pause-Menü oder aus Sondermodi raus
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                switch (_currentState)
                {
                    case GameState.Playing:
                        SetGameState(GameState.Paused);
                        break;
                    case GameState.Paused:
                        SetGameState(GameState.Playing);
                        break;
                    case GameState.ShopMode:
                    case GameState.CashierMode:
                    case GameState.PlacementMode:
                        SetGameState(GameState.Playing);
                        break;
                }
            }
        }

        // --- Globale Spielaktionen ---

        public void StartNewGame()
        {
            Debug.Log("[GameManager] Neues Spiel gestartet.");
            SetGameState(GameState.Loading);
            // Hier Scene laden: SceneManager.LoadSceneAsync("GameScene");
            SetGameState(GameState.Playing);
        }

        public void PauseGame()  => SetGameState(GameState.Paused);
        public void ResumeGame() => SetGameState(GameState.Playing);

        public void QuitToMainMenu()
        {
            SaveManager.Instance?.SaveGame(0);
            SetGameState(GameState.MainMenu);
            // SceneManager.LoadScene("MainMenu");
        }

        public void QuitGame()
        {
            SaveManager.Instance?.SaveGame(0);
            Debug.Log("[GameManager] Spiel wird beendet.");
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
