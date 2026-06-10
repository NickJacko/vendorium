using UnityEngine;

namespace Vendorium
{
    // Sitzt auf MainCamera. Raycast nach vorne, findet IInteractable.
    // Zeigt InteractionPrompt UI an und löst Interact() aus.
    [RequireComponent(typeof(Camera))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Einstellungen")]
        [SerializeField] private PlayerStats stats;
        [SerializeField] private LayerMask interactableLayers;

        [Header("Outline-Effekt")]
        [SerializeField] private Material outlineMaterial;
        private Renderer _lastOutlinedRenderer;
        private Material _lastOriginalMaterial;

        private PlayerController _player;
        private IInteractable _currentTarget;
        private InteractionPrompt _prompt;

        private void Start()
        {
            _player = GetComponentInParent<PlayerController>();
            _prompt = FindAnyObjectByType<InteractionPrompt>();
        }

        private void Update()
        {
            ScanForInteractable();

            if (_currentTarget != null && Input.GetKeyDown(KeyCode.E))
                TryInteract();
        }

        private void ScanForInteractable()
        {
            float range = stats != null ? stats.InteractionRange : 2.5f;
            LayerMask mask = stats != null ? stats.InteractableLayers : interactableLayers;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, range, mask))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null && interactable.CanInteract())
                {
                    if (interactable != _currentTarget)
                    {
                        RemoveOutline();
                        _currentTarget = interactable;
                        _prompt?.Show(_currentTarget.GetInteractionText());
                        ApplyOutline(hit.collider.GetComponent<Renderer>());
                    }
                    return;
                }
            }

            // Kein Treffer → Ziel leeren
            if (_currentTarget != null)
            {
                _currentTarget = null;
                _prompt?.Hide();
                RemoveOutline();
            }
        }

        private void TryInteract()
        {
            if (_currentTarget == null || !_currentTarget.CanInteract()) return;

            _currentTarget.Interact(_player);
            _prompt?.Hide();
        }

        private void ApplyOutline(Renderer rend)
        {
            if (rend == null || outlineMaterial == null) return;

            _lastOutlinedRenderer = rend;
            _lastOriginalMaterial = rend.material;
            rend.material = outlineMaterial;
        }

        private void RemoveOutline()
        {
            if (_lastOutlinedRenderer != null && _lastOriginalMaterial != null)
            {
                _lastOutlinedRenderer.material = _lastOriginalMaterial;
                _lastOutlinedRenderer = null;
                _lastOriginalMaterial = null;
            }
        }
    }
}
