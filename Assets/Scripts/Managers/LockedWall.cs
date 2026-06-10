using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.AI.NavMesh;

namespace Vendorium
{
    // Gesperrte Wand zwischen zwei Räumen. Zeigt Schloss-Icon und Preis.
    // Spieler interagiert → Bestätigungs-Dialog → Wand sinkt in den Boden → NavMesh neu gebacken.
    public class LockedWall : MonoBehaviour, IInteractable
    {
        [Header("Raum")]
        [SerializeField] private string roomIdToUnlock;
        [SerializeField] private RoomData roomData;

        [Header("Abbau-Animation")]
        [SerializeField] private float sinkDuration = 1.5f;
        [SerializeField] private ParticleSystem dustParticles;

        private bool _isUnlocked = false;
        private bool _isAnimating = false;

        private void Start()
        {
            // Falls RoomData direkt oder per RoomManager gesetzt
            if (roomData == null && !string.IsNullOrEmpty(roomIdToUnlock))
                roomData = RoomManager.Instance?.GetRoomData(roomIdToUnlock);

            // Wand sichtbar machen wenn noch gesperrt
            _isUnlocked = RoomManager.Instance?.IsUnlocked(roomIdToUnlock) ?? false;
            if (_isUnlocked) gameObject.SetActive(false);
        }

        public void Interact(PlayerController player)
        {
            if (_isUnlocked || _isAnimating || roomData == null) return;

            decimal cost = roomData.UnlockCost;
            decimal money = EconomyManager.Instance.GetCurrentMoney();

            if (money < cost)
            {
                Debug.Log($"[LockedWall] Nicht genug Geld. Benötigt: {cost} €, Vorhanden: {money:F2} €");
                // TODO: UIManager.ShowNotification("Nicht genug Geld!")
                return;
            }

            // Direkt freischalten (Dialog kommt in Phase 6 mit dem Story-System)
            bool success = RoomManager.Instance.UnlockRoom(roomIdToUnlock);
            if (success)
                StartCoroutine(SinkAnimation());
        }

        public string GetInteractionText()
        {
            if (roomData == null) return "[E] Raum freischalten";
            decimal cost = roomData.UnlockCost;
            decimal money = EconomyManager.Instance?.GetCurrentMoney() ?? 0m;
            string affordable = money >= cost ? "" : " (zu teuer)";
            return $"[E] {roomData.RoomName} freischalten — {cost} €{affordable}";
        }

        public bool CanInteract() => !_isUnlocked && !_isAnimating;

        private IEnumerator SinkAnimation()
        {
            _isAnimating = true;

            dustParticles?.Play();

            Vector3 startPos = transform.position;
            Vector3 endPos   = startPos - Vector3.up * (transform.localScale.y + 0.5f);

            float elapsed = 0f;
            while (elapsed < sinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / sinkDuration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            // NavMesh neu backen damit Kunden den neuen Raum betreten können
            NavMeshSurface surface = FindAnyObjectByType<NavMeshSurface>();
            if (surface != null)
                surface.BuildNavMesh();
            else
                Debug.LogWarning("[LockedWall] Kein NavMeshSurface gefunden — NavMesh manuell neu backen!");

            _isUnlocked = true;
            gameObject.SetActive(false);
        }
    }
}
