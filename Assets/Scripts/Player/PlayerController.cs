using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    // Setzt CharacterController voraus (kein Rigidbody).
    // Prefab-Struktur: Player(Root) → CameraHolder → MainCamera
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private PlayerStats stats;

        [Header("Referenzen")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private AudioSource audioSource;

        [Header("Fußschritte")]
        [SerializeField] private List<AudioClip> footstepClips = new List<AudioClip>();
        [SerializeField] private float footstepInterval = 0.45f;

        private CharacterController _cc;
        private Vector3 _velocity;
        private float _verticalLook;
        private bool _isGrounded;
        private bool _isMoving;

        private float _footstepTimer;
        private bool _inputEnabled = true;

        // Wird von HeadBobEffect gelesen
        public bool IsMoving => _isMoving && _isGrounded;
        public bool IsRunning => _isMoving && Input.GetKey(KeyCode.LeftShift);
        public float MoveSpeed => IsRunning
            ? (stats != null ? stats.RunSpeed : 7f)
            : (stats != null ? stats.WalkSpeed : 4f);

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Auf GameState-Änderungen reagieren
            VendoriumEventManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance != null)
                VendoriumEventManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (!_inputEnabled) return;

            HandleGravityAndGrounded();
            HandleMovement();
            HandleMouseLook();
            HandleFootsteps();
            HandleJump();
        }

        private void HandleGravityAndGrounded()
        {
            _isGrounded = _cc.isGrounded;

            if (_isGrounded && _velocity.y < 0f)
                _velocity.y = -2f; // Kleiner negativer Wert damit isGrounded stabil bleibt

            _velocity.y += Physics.gravity.y * Time.deltaTime;
            _cc.Move(_velocity * Time.deltaTime);
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical   = Input.GetAxis("Vertical");

            Vector3 move = transform.right * horizontal + transform.forward * vertical;
            move = Vector3.ClampMagnitude(move, 1f);

            _isMoving = move.magnitude > 0.1f;

            _cc.Move(move * MoveSpeed * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            float sens = stats != null ? stats.MouseSensitivity : 2f;
            float mouseX = Input.GetAxis("Mouse X") * sens;
            float mouseY = Input.GetAxis("Mouse Y") * sens;

            // Horizontale Rotation: gesamtes Player-Objekt drehen
            transform.Rotate(Vector3.up * mouseX);

            // Vertikale Rotation: nur CameraHolder, geclampt
            _verticalLook -= mouseY;
            float minAngle = stats != null ? stats.MinVerticalAngle : -80f;
            float maxAngle = stats != null ? stats.MaxVerticalAngle :  80f;
            _verticalLook = Mathf.Clamp(_verticalLook, minAngle, maxAngle);

            if (cameraHolder != null)
                cameraHolder.localRotation = Quaternion.Euler(_verticalLook, 0f, 0f);
        }

        private void HandleJump()
        {
            if (!_isGrounded) return;
            if (!Input.GetButtonDown("Jump")) return;

            float jumpHeight = stats != null ? stats.JumpHeight : 1.2f;
            // v = sqrt(2 * g * h)
            _velocity.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
        }

        private void HandleFootsteps()
        {
            if (!_isMoving || !_isGrounded || footstepClips.Count == 0) return;

            float interval = IsRunning ? footstepInterval * 0.65f : footstepInterval;
            _footstepTimer += Time.deltaTime;

            if (_footstepTimer >= interval)
            {
                _footstepTimer = 0f;
                AudioClip clip = footstepClips[Random.Range(0, footstepClips.Count)];
                audioSource.PlayOneShot(clip, 0.6f);
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _velocity = Vector3.zero;
                _isMoving = false;
            }
        }

        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            bool playable = newState == GameState.Playing;
            SetInputEnabled(playable);

            if (newState == GameState.Playing)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
