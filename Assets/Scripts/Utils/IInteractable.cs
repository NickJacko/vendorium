using UnityEngine;

namespace Vendorium
{
    // Jedes interaktive Objekt in der Welt implementiert dieses Interface.
    // PlayerInteraction.cs castet per Raycast darauf.
    public interface IInteractable
    {
        void Interact(PlayerController player);
        string GetInteractionText();
        bool CanInteract();
    }
}
