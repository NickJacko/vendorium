using System.Collections;
using UnityEngine;

namespace Vendorium
{
    // Visuelles Feedback bei jedem Verkauf: goldene Münz-Partikel + Verkaufs-Sound.
    // Sitzt auf demselben GameObject wie VendingMachine.cs.
    [RequireComponent(typeof(VendingMachine))]
    public class MachineSalesEffect : MonoBehaviour
    {
        [Header("Partikel")]
        [SerializeField] private ParticleSystem coinParticlesPrefab;
        [SerializeField] private Vector3 particleOffset = new Vector3(0f, 1.8f, 0.3f);

        [Header("Sound")]
        [SerializeField] private AudioClip saleSound;
        [SerializeField] private AudioClip emptySound;
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 0.8f;

        private VendingMachine _machine;
        private AudioSource _audioSource;
        private ParticleSystem _coinParticles;

        private void Awake()
        {
            _machine = GetComponent<VendingMachine>();
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // 3D-Sound
            _audioSource.maxDistance = 10f;
        }

        private void Start()
        {
            _machine.OnSale       += HandleSale;
            _machine.OnStockEmpty += HandleStockEmpty;

            SpawnCoinParticles();
        }

        private void OnDestroy()
        {
            if (_machine == null) return;
            _machine.OnSale       -= HandleSale;
            _machine.OnStockEmpty -= HandleStockEmpty;
        }

        private void HandleSale(float amount)
        {
            if (_coinParticles != null)
                _coinParticles.Play();

            if (saleSound != null)
                _audioSource.PlayOneShot(saleSound, soundVolume);

            // Kleines visuelles Popup: Betrag als FloatingText
            SpawnFloatingText($"+{amount:F2}€");
        }

        private void HandleStockEmpty()
        {
            if (emptySound != null)
                _audioSource.PlayOneShot(emptySound, soundVolume);
        }

        private void SpawnCoinParticles()
        {
            if (coinParticlesPrefab == null) return;

            _coinParticles = Instantiate(
                coinParticlesPrefab,
                transform.position + particleOffset,
                Quaternion.identity,
                transform
            );
            _coinParticles.Stop();
        }

        // Einfaches Floating-Text Popup (kein externes Asset nötig)
        private void SpawnFloatingText(string text)
        {
            StartCoroutine(FloatingTextRoutine(text));
        }

        private IEnumerator FloatingTextRoutine(string text)
        {
            // Canvas-basierter WorldSpace-Text wird in MachineInspectPanel behandelt.
            // Hier nur Debug-Log als Fallback bis UI gebaut ist.
            Debug.Log($"[{_machine.Data?.MachineName ?? gameObject.name}] Verkauf: {text}");
            yield break;
        }
    }
}
