using UnityEngine;

namespace Vendorium
{
    // Sitzt auf CameraHolder. Liest PlayerController.IsMoving und animiert die Kamera.
    // Sanfter Effekt — kein Kopfschmerz-induzierende Übertreibung.
    public class HeadBobEffect : MonoBehaviour
    {
        [Header("Referenz")]
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerStats stats;

        [Header("Bob-Parameter (Überschreiben PlayerStats wenn gesetzt)")]
        [SerializeField] private float bobFrequency = 2f;
        [SerializeField] private float bobAmplitudeY = 0.05f;
        [SerializeField] private float bobAmplitudeX = 0.025f;
        [SerializeField] private float smoothSpeed = 10f;

        private Vector3 _defaultLocalPos;
        private float _bobTimer;
        private bool _useStats;

        private void Awake()
        {
            _defaultLocalPos = transform.localPosition;
            _useStats = stats != null;
        }

        private void Start()
        {
            if (player == null)
                player = GetComponentInParent<PlayerController>();
        }

        private void Update()
        {
            if (player == null) return;

            float freq = _useStats ? stats.BobFrequency : bobFrequency;
            float ampY = _useStats ? stats.BobAmplitude  : bobAmplitudeY;
            float ampX = ampY * 0.5f;

            Vector3 targetPos = _defaultLocalPos;

            if (player.IsMoving)
            {
                float speedMultiplier = player.IsRunning ? 1.4f : 1f;
                _bobTimer += Time.deltaTime * freq * speedMultiplier;

                targetPos = _defaultLocalPos + new Vector3(
                    Mathf.Cos(_bobTimer * 0.5f) * ampX,
                    Mathf.Sin(_bobTimer) * ampY,
                    0f
                );
            }
            else
            {
                _bobTimer = 0f;
            }

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                targetPos,
                Time.deltaTime * smoothSpeed
            );
        }
    }
}
