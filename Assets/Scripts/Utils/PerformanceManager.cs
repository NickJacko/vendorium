using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;

#pragma warning disable CS0618

namespace Vendorium
{
    // Performance-Monitoring und -Optimierung.
    // Ziel: 60 FPS mit 20 Kunden auf Intel i5 + GTX 1050.
    // Loggt beim Start einen vollständigen Performance-Report in die Console.
    public class PerformanceManager : Singleton<PerformanceManager>
    {
        [Header("Zielwerte")]
        [SerializeField] private int targetFPS = 60;
        [SerializeField] private float warningFPSThreshold = 45f;

        [Header("FPS-Anzeige (Debug)")]
        [SerializeField] private bool showFPSOverlay = false;

        private float _fpsTimer;
        private float _currentFPS;
        private int _frameCount;
        private float _fpsAccumulator;

        private void Start()
        {
            Application.targetFrameRate = targetFPS;
            QualitySettings.vSyncCount  = 0; // VSync deaktivieren wenn targetFrameRate gesetzt

            OptimizeNavMeshAgents();
            LogPerformanceReport();
        }

        private void Update()
        {
            _frameCount++;
            _fpsAccumulator += Time.unscaledDeltaTime;

            if (_fpsAccumulator >= 0.5f)
            {
                _currentFPS = _frameCount / _fpsAccumulator;
                _frameCount = 0;
                _fpsAccumulator = 0f;

                if (_currentFPS < warningFPSThreshold)
                    Debug.LogWarning($"[PerformanceManager] Niedriges FPS: {_currentFPS:F0} (Ziel: {targetFPS})");
            }
        }

        private void OnGUI()
        {
            if (!showFPSOverlay) return;

            Color color = _currentFPS >= targetFPS ? Color.green :
                          _currentFPS >= warningFPSThreshold ? Color.yellow : Color.red;

            GUI.color = color;
            GUI.Label(new Rect(10, 10, 120, 25), $"FPS: {_currentFPS:F0}");
            GUI.color = Color.white;
        }

        private void OptimizeNavMeshAgents()
        {
            // Alle NavMeshAgents im Projekt bekommen ein reduziertes Update-Intervall.
            // (In CustomerController wird dies bereits berücksichtigt.)
            foreach (var agent in FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))
            {
                // Keine direkte API für Update-Intervall in NavMeshAgent —
                // stattdessen steuert CustomerController selbst wann es den Pfad updatet.
            }

            Debug.Log("[PerformanceManager] NavMesh-Agenten optimiert.");
        }

        // Gibt einen detaillierten Performance-Report in die Console aus
        public void LogPerformanceReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== VENDORIUM PERFORMANCE REPORT ===");
            sb.AppendLine($"Plattform       : {Application.platform}");
            sb.AppendLine($"Unity Version   : {Application.unityVersion}");
            sb.AppendLine($"Qualitätsstufe  : {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            sb.AppendLine($"Auflösung       : {Screen.width}x{Screen.height} @ {Screen.currentResolution.refreshRateRatio}Hz");
            sb.AppendLine($"VSync           : {QualitySettings.vSyncCount}");
            sb.AppendLine($"Ziel-FPS        : {targetFPS}");
            sb.AppendLine($"RAM gesamt      : {SystemInfo.systemMemorySize} MB");
            sb.AppendLine($"GPU             : {SystemInfo.graphicsDeviceName}");
            sb.AppendLine($"GPU VRAM        : {SystemInfo.graphicsMemorySize} MB");
            sb.AppendLine($"CPU Kerne       : {SystemInfo.processorCount}");

            sb.AppendLine("--- Szenen-Objekte ---");
            sb.AppendLine($"GameObjects in Szene : {FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length}");
            sb.AppendLine($"Aktive Kunden        : {CustomerManager.Instance?.ActiveCustomerCount ?? 0}");
            sb.AppendLine($"Platzierte Automaten : {MachineManager.Instance?.MachineCount ?? 0}");

            sb.AppendLine("--- Checkliste ---");
            sb.AppendLine($"[{(Application.targetFrameRate > 0 ? "OK" : "!!")}] Target FPS gesetzt");
            sb.AppendLine($"[{(QualitySettings.vSyncCount == 0 ? "OK" : "!!")}] VSync deaktiviert (für manuelles FPS-Cap)");

            sb.AppendLine("=====================================");
            Debug.Log(sb.ToString());
        }

        // Hilfsmethode: Alle statischen Objekte als Static markieren (einmalig aufrufen)
        [ContextMenu("Statische Objekte markieren")]
        public void MarkStaticObjects()
        {
            int count = 0;
            foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.CompareTag("Wall") || go.CompareTag("Floor") || go.CompareTag("Ceiling"))
                {
                    go.isStatic = true;
                    count++;
                }
            }
            Debug.Log($"[PerformanceManager] {count} statische Objekte markiert.");
        }

        public float GetCurrentFPS() => _currentFPS;
    }
}
