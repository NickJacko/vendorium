using UnityEngine;

namespace Vendorium
{
    // Box-Trigger hinter der Kassen-Theke.
    // Spieler betritt Zone → E-Prompt erscheint → E drücken → Kassierer-Modus.
    public class CashierTrigger : MonoBehaviour, IInteractable
    {
        private bool _playerInZone = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                _playerInZone = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                _playerInZone = false;
        }

        public void Interact(PlayerController player)
        {
            CashRegisterManager.Instance?.EnterCashierMode();
        }

        public string GetInteractionText() => "[E] Kasse bedienen";

        public bool CanInteract() => _playerInZone &&
                                     CashRegisterManager.Instance != null &&
                                     !CashRegisterManager.Instance.IsActive;
    }
}
