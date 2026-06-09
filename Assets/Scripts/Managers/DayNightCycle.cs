using UnityEngine;
using UnityEngine.Rendering;

namespace Vendorium
{
    // Tages-Nacht-Zyklus: dreht Directional Light, wechselt Sky-Farben,
    // schaltet Innenbeleuchtung an/aus, verändert Post Processing Bloom + Color Grading.
    [RequireComponent(typeof(Light))]
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Licht")]
        private Light _sunLight;

        [Header("Himmel-Farben je Tageszeit")]
        [SerializeField] private Gradient skyColorGradient;      // Oben (Zenith)
        [SerializeField] private Gradient horizonColorGradient;  // Horizont

        [Header("Sonnenlicht-Intensität (0–24h)")]
        [SerializeField] private AnimationCurve sunIntensityCurve = new AnimationCurve(
            new Keyframe(0f,  0.05f),
            new Keyframe(6f,  0.1f),
            new Keyframe(8f,  0.8f),
            new Keyframe(12f, 1.2f),
            new Keyframe(18f, 0.7f),
            new Keyframe(20f, 0.1f),
            new Keyframe(24f, 0.05f)
        );

        [Header("Laden-Lampen (gehen an wenn draußen dunkel)")]
        [SerializeField] private Light[] shopLights;
        [SerializeField] private float shopLightsThreshold = 0.3f; // Intensität unter der Lampen angehen

        [Header("Neon-Lichter (werden nachts heller)")]
        [SerializeField] private Light[] neonLights;
        [SerializeField] private float neonNightIntensity = 2.5f;
        [SerializeField] private float neonDayIntensity  = 0.5f;

        [Header("Post Processing Volume")]
        [SerializeField] private Volume postProcessVolume;

        // Sky-Hintergrundfarbe über Camera.backgroundColor (kein HDRI nötig)
        private Camera _mainCamera;

        private void Awake()
        {
            _sunLight    = GetComponent<Light>();
            _mainCamera  = Camera.main;
        }

        private void Start()
        {
            VendoriumEventManager.Instance.OnTimeOfDayChanged += OnTimeOfDayChanged;
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance != null)
                VendoriumEventManager.Instance.OnTimeOfDayChanged -= OnTimeOfDayChanged;
        }

        private void Update()
        {
            if (EconomyManager.Instance == null) return;

            float dayTime = EconomyManager.Instance.DayTimeNormalized; // 0–1
            float hour    = dayTime * 24f;

            UpdateSunRotation(dayTime);
            UpdateSunIntensity(hour);
            UpdateSkyColor(dayTime);
            UpdateShopLights();
            UpdateNeonLights(hour);
        }

        private void UpdateSunRotation(float dayTime)
        {
            // Sonne dreht von -90° (Sonnenuntergang) über 90° (Mittag) bis 270° (nächste Nacht)
            float angle = dayTime * 360f - 90f;
            _sunLight.transform.rotation = Quaternion.Euler(angle, 30f, 0f);
        }

        private void UpdateSunIntensity(float hour)
        {
            float t = Mathf.InverseLerp(0f, 24f, hour);
            _sunLight.intensity = sunIntensityCurve.Evaluate(hour);
        }

        private void UpdateSkyColor(float dayTime)
        {
            if (_mainCamera == null) return;

            Color skyColor     = skyColorGradient     != null ? skyColorGradient.Evaluate(dayTime)     : Color.black;
            Color horizonColor = horizonColorGradient != null ? horizonColorGradient.Evaluate(dayTime) : Color.gray;

            // Einfach: Camera-Hintergrundfarbe für Skybox-freie Projekte
            _mainCamera.backgroundColor = Color.Lerp(horizonColor, skyColor, 0.5f);
            RenderSettings.ambientLight = Color.Lerp(skyColor * 0.3f, skyColor, _sunLight.intensity / 1.2f);
        }

        private void UpdateShopLights()
        {
            bool shouldBeOn = _sunLight.intensity < shopLightsThreshold;
            foreach (var light in shopLights)
                if (light != null) light.enabled = shouldBeOn;
        }

        private void UpdateNeonLights(float hour)
        {
            bool isNight = hour >= 20f || hour < 7f;
            float targetIntensity = isNight ? neonNightIntensity : neonDayIntensity;

            foreach (var neon in neonLights)
            {
                if (neon == null) continue;
                neon.intensity = Mathf.MoveTowards(neon.intensity, targetIntensity, Time.deltaTime * 1.5f);
            }
        }

        private void OnTimeOfDayChanged(TimeOfDay time)
        {
            // Post Processing Volume Profile wechseln je nach Tageszeit
            // Wird implementiert wenn Post Processing Package vorhanden ist
            Debug.Log($"[DayNightCycle] Tageszeit: {time}");
        }
    }
}
