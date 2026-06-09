using UnityEngine;

namespace Vendorium
{
    // Verwaltet den Placement-Mode: Geist-Automat folgt dem Spieler,
    // Grid-Snapping, Kollisions-Check, Platzierung per E.
    // Wird vom ShopScreen aktiviert nachdem der Spieler einen Automaten gekauft hat.
    public class PlacementManager : Singleton<PlacementManager>
    {
        [Header("Einstellungen")]
        [SerializeField] private LayerMask placementLayerMask; // Floor-Layer
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private Camera playerCamera;

        [Header("Materialien")]
        [SerializeField] private Material ghostValidMaterial;   // Grün-transparent
        [SerializeField] private Material ghostInvalidMaterial; // Rot-transparent

        private bool _isActive = false;
        private GameObject _ghostObject;
        private MachineData _pendingMachineData;
        private bool _isValidPlacement = false;
        private Renderer[] _ghostRenderers;

        public bool IsActive => _isActive;

        private void Update()
        {
            if (!_isActive || _ghostObject == null) return;

            UpdateGhostPosition();
            CheckValidity();
            HandlePlacementInput();
        }

        // Wird von ShopScreen aufgerufen: Startet Placement-Mode
        public void StartPlacement(MachineData machineData)
        {
            if (machineData == null || machineData.Prefab == null)
            {
                Debug.LogWarning("[PlacementManager] MachineData oder Prefab fehlt.");
                return;
            }

            _pendingMachineData = machineData;
            CreateGhostObject();
            _isActive = true;

            GameManager.Instance?.SetGameState(GameState.PlacementMode);
            Debug.Log($"[PlacementManager] Placement-Mode aktiv für: {machineData.MachineName}");
        }

        // Abbricht den Placement-Mode (Escape)
        public void CancelPlacement()
        {
            CleanupGhost();
            _isActive = false;
            _pendingMachineData = null;
            GameManager.Instance?.SetGameState(GameState.Playing);
        }

        private void UpdateGhostPosition()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, 8f, placementLayerMask))
            {
                Vector3 snapped = SnapToGrid(hit.point);
                snapped.y = hit.point.y + GetPrefabBaseOffset();
                _ghostObject.transform.position = snapped;
            }
        }

        private void CheckValidity()
        {
            Vector3 pos = _ghostObject.transform.position;

            // Prüfe ob Position von einer anderen Maschine belegt ist
            bool occupied = MachineManager.Instance != null &&
                            MachineManager.Instance.IsPositionOccupied(pos, 0.7f);

            // Prüfe ob auf NavMesh (grobe Prüfung)
            bool onNavMesh = UnityEngine.AI.NavMesh.SamplePosition(pos, out _, 0.5f, UnityEngine.AI.NavMesh.AllAreas);

            _isValidPlacement = !occupied && onNavMesh;

            ApplyGhostMaterial(_isValidPlacement);
        }

        private void HandlePlacementInput()
        {
            // R-Taste: 90° rotieren
            if (Input.GetKeyDown(KeyCode.R))
                _ghostObject.transform.Rotate(Vector3.up, 90f);

            // E oder Linksklick: platzieren
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            {
                if (_isValidPlacement)
                    ConfirmPlacement();
            }

            // Escape: abbrechen
            if (Input.GetKeyDown(KeyCode.Escape))
                CancelPlacement();
        }

        private void ConfirmPlacement()
        {
            if (_pendingMachineData == null || _pendingMachineData.Prefab == null) return;

            Vector3 pos = _ghostObject.transform.position;
            Quaternion rot = _ghostObject.transform.rotation;

            // Richtiges Prefab spawnen
            GameObject placed = Instantiate(_pendingMachineData.Prefab, pos, rot);
            var machine = placed.GetComponent<VendingMachine>();

            if (machine != null)
            {
                // MachineData zuweisen via Reflection-fähige Methode (wird in VendingMachine ergänzt)
                StartCoroutine(SpawnAnimation(placed));
            }

            CleanupGhost();
            _isActive = false;
            _pendingMachineData = null;
            GameManager.Instance?.SetGameState(GameState.Playing);

            Debug.Log($"[PlacementManager] Maschine platziert bei {pos}.");
        }

        private System.Collections.IEnumerator SpawnAnimation(GameObject obj)
        {
            // Scale von 0 auf 1 über 0.3 Sekunden
            float duration = 0.3f;
            float elapsed  = 0f;
            obj.transform.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                obj.transform.localScale = Vector3.one * t;
                yield return null;
            }
            obj.transform.localScale = Vector3.one;
        }

        private void CreateGhostObject()
        {
            CleanupGhost();

            _ghostObject = Instantiate(_pendingMachineData.Prefab);
            _ghostObject.name = "Ghost_" + _pendingMachineData.MachineName;

            // Alle Collider und Scripts deaktivieren — Ghost ist nur visuell
            foreach (var col in _ghostObject.GetComponentsInChildren<Collider>())
                col.enabled = false;
            foreach (var script in _ghostObject.GetComponentsInChildren<MonoBehaviour>())
                if (!(script is Renderer) && !(script is MeshFilter))
                    script.enabled = false;

            _ghostRenderers = _ghostObject.GetComponentsInChildren<Renderer>();
            ApplyGhostMaterial(true);
        }

        private void ApplyGhostMaterial(bool valid)
        {
            if (_ghostRenderers == null) return;
            Material mat = valid ? ghostValidMaterial : ghostInvalidMaterial;
            if (mat == null) return;
            foreach (var rend in _ghostRenderers)
                rend.material = mat;
        }

        private void CleanupGhost()
        {
            if (_ghostObject != null)
            {
                Destroy(_ghostObject);
                _ghostObject = null;
            }
        }

        private Vector3 SnapToGrid(Vector3 pos)
        {
            return new Vector3(
                Mathf.Round(pos.x / gridSize) * gridSize,
                pos.y,
                Mathf.Round(pos.z / gridSize) * gridSize
            );
        }

        private float GetPrefabBaseOffset()
        {
            if (_pendingMachineData?.Prefab == null) return 0f;
            // Automaten stehen mit Unterkante auf dem Boden
            var bounds = _pendingMachineData.Prefab.GetComponentInChildren<Renderer>()?.bounds;
            return bounds.HasValue ? bounds.Value.extents.y : 0.75f;
        }
    }
}
