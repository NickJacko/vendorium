using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("Haupt-Screens (im Inspector zuweisen)")]
        [SerializeField] private GameObject hudScreen;
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject pauseMenuScreen;
        [SerializeField] private GameObject shopScreen;
        [SerializeField] private GameObject machineInspectPanel;
        [SerializeField] private GameObject cashRegisterPanel;
        [SerializeField] private GameObject dailyReportPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private GameObject eventNotificationPanel;
        [SerializeField] private GameObject loadingScreen;

        private UIScreen _currentScreen = UIScreen.None;
        private Stack<UIScreen> _screenHistory = new Stack<UIScreen>();

        public UIScreen CurrentScreen => _currentScreen;

        private void Start()
        {
            // Alle Screens initial ausblenden
            HideAll();

            VendoriumEventManager.Instance.OnGameStateChanged += OnGameStateChanged;
            VendoriumEventManager.Instance.OnDailyReport += OnDailyReport;
            VendoriumEventManager.Instance.OnGameEventStarted += OnGameEventStarted;
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance == null) return;
            VendoriumEventManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            VendoriumEventManager.Instance.OnDailyReport -= OnDailyReport;
            VendoriumEventManager.Instance.OnGameEventStarted -= OnGameEventStarted;
        }

        public void ShowScreen(UIScreen screen, bool addToHistory = true)
        {
            if (_currentScreen != UIScreen.None && addToHistory)
                _screenHistory.Push(_currentScreen);

            HideAll();
            _currentScreen = screen;
            SetScreenActive(screen, true);

            // Cursor-Steuerung je nach Screen
            bool needsCursor = screen != UIScreen.HUD;
            Cursor.lockState = needsCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = needsCursor;
        }

        public void GoBack()
        {
            if (_screenHistory.Count > 0)
                ShowScreen(_screenHistory.Pop(), false);
            else
                ShowScreen(UIScreen.HUD, false);
        }

        public void HideAll()
        {
            SetScreenActive(UIScreen.HUD, false);
            SetScreenActive(UIScreen.MainMenu, false);
            SetScreenActive(UIScreen.PauseMenu, false);
            SetScreenActive(UIScreen.ShopScreen, false);
            SetScreenActive(UIScreen.MachineInspect, false);
            SetScreenActive(UIScreen.CashRegister, false);
            SetScreenActive(UIScreen.DailyReport, false);
            SetScreenActive(UIScreen.Settings, false);
            SetScreenActive(UIScreen.Dialogue, false);
            SetScreenActive(UIScreen.EventNotification, false);
        }

        private void SetScreenActive(UIScreen screen, bool active)
        {
            GameObject target = GetScreenObject(screen);
            if (target != null)
                target.SetActive(active);
        }

        private GameObject GetScreenObject(UIScreen screen)
        {
            return screen switch
            {
                UIScreen.HUD => hudScreen,
                UIScreen.MainMenu => mainMenuScreen,
                UIScreen.PauseMenu => pauseMenuScreen,
                UIScreen.ShopScreen => shopScreen,
                UIScreen.MachineInspect => machineInspectPanel,
                UIScreen.CashRegister => cashRegisterPanel,
                UIScreen.DailyReport => dailyReportPanel,
                UIScreen.Settings => settingsPanel,
                UIScreen.Dialogue => dialoguePanel,
                UIScreen.EventNotification => eventNotificationPanel,
                _ => null
            };
        }

        public void ShowLoading(bool show)
        {
            if (loadingScreen != null)
                loadingScreen.SetActive(show);
        }

        // Bequemlichkeitsmethoden für häufig genutzte Screens
        public void ShowHUD() => ShowScreen(UIScreen.HUD, false);
        public void ShowPauseMenu() => ShowScreen(UIScreen.PauseMenu);
        public void ShowShop() => ShowScreen(UIScreen.ShopScreen);
        public void ShowMachineInspect() => ShowScreen(UIScreen.MachineInspect);
        public void ShowCashRegister() => ShowScreen(UIScreen.CashRegister);
        public void CloseCurrent() => GoBack();

        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            switch (newState)
            {
                case GameState.Playing:
                    ShowScreen(UIScreen.HUD, false);
                    break;
                case GameState.Paused:
                    ShowScreen(UIScreen.PauseMenu);
                    break;
                case GameState.MainMenu:
                    ShowScreen(UIScreen.MainMenu, false);
                    break;
            }
        }

        private void OnDailyReport(DailyStats stats)
        {
            ShowScreen(UIScreen.DailyReport);
            // DailyReportPanel.cs befüllt sich selbst über den Event
        }

        private void OnGameEventStarted(GameEventData data)
        {
            // Event-Benachrichtigung kurz einblenden, dann wieder ausblenden
            if (eventNotificationPanel != null)
            {
                eventNotificationPanel.SetActive(true);
                // EventNotificationPanel.cs steuert die Dauer selbst
            }
        }
    }
}
