using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Audio-Daten")]
        [SerializeField] private AudioData audioData;

        [Header("Audio-Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;

        [Header("SFX Pool (10 AudioSources als Kinder)")]
        [SerializeField] private int sfxPoolSize = 10;

        // Lautstärken
        private float _masterVolume = 1f;
        private float _musicVolume  = 0.7f;
        private float _sfxVolume    = 1f;
        private float _ambientVolume = 0.5f;

        private List<AudioSource> _sfxPool = new List<AudioSource>();
        private Coroutine _musicFadeCoroutine;

        private const string PREF_MASTER  = "vol_master";
        private const string PREF_MUSIC   = "vol_music";
        private const string PREF_SFX     = "vol_sfx";
        private const string PREF_AMBIENT = "vol_ambient";

        protected override void Awake()
        {
            base.Awake();
            LoadVolumePrefs();
            BuildSFXPool();
            EnsureAudioSources();
        }

        private void Start()
        {
            VendoriumEventManager.Instance.OnGameStateChanged += OnGameStateChanged;
            VendoriumEventManager.Instance.OnMachineSale      += OnMachineSale;
            VendoriumEventManager.Instance.OnMachineStockEmpty += OnMachineStockEmpty;
            VendoriumEventManager.Instance.OnMachineUpgraded  += OnMachineUpgraded;
            VendoriumEventManager.Instance.OnRoomUnlocked     += OnRoomUnlocked;
            VendoriumEventManager.Instance.OnSynergyDiscovered += OnSynergyDiscovered;
            VendoriumEventManager.Instance.OnGameEventStarted += OnGameEventStarted;
            VendoriumEventManager.Instance.OnTimeOfDayChanged += OnTimeOfDayChanged;

            if (audioData != null)
                PlayMusic(audioData.ShopMusic);

            PlayAmbient(audioData?.ShopAmbient);
        }

        private void OnDestroy()
        {
            if (VendoriumEventManager.Instance == null) return;
            VendoriumEventManager.Instance.OnGameStateChanged  -= OnGameStateChanged;
            VendoriumEventManager.Instance.OnMachineSale       -= OnMachineSale;
            VendoriumEventManager.Instance.OnMachineStockEmpty -= OnMachineStockEmpty;
            VendoriumEventManager.Instance.OnMachineUpgraded   -= OnMachineUpgraded;
            VendoriumEventManager.Instance.OnRoomUnlocked      -= OnRoomUnlocked;
            VendoriumEventManager.Instance.OnSynergyDiscovered -= OnSynergyDiscovered;
            VendoriumEventManager.Instance.OnGameEventStarted  -= OnGameEventStarted;
            VendoriumEventManager.Instance.OnTimeOfDayChanged  -= OnTimeOfDayChanged;
        }

        // --- Musik ---

        public void PlayMusic(AudioClip clip, float fadeDuration = 1f)
        {
            if (clip == null) return;
            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(clip, fadeDuration));
        }

        public void StopMusic(float fadeDuration = 1f)
        {
            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(FadeOutRoutine(musicSource, fadeDuration));
        }

        private IEnumerator FadeMusicRoutine(AudioClip newClip, float duration)
        {
            // Alten Track ausblenden
            if (musicSource.isPlaying)
            {
                float startVol = musicSource.volume;
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
                    yield return null;
                }
            }

            musicSource.clip = newClip;
            musicSource.loop = true;
            musicSource.Play();

            // Neuen Track einblenden
            float t2 = 0f;
            while (t2 < duration)
            {
                t2 += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, _musicVolume * _masterVolume, t2 / duration);
                yield return null;
            }
        }

        private IEnumerator FadeOutRoutine(AudioSource source, float duration)
        {
            float startVol = source.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
            source.Stop();
        }

        // --- Ambient ---

        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null || ambientSource == null) return;
            ambientSource.clip = clip;
            ambientSource.loop = true;
            ambientSource.volume = _ambientVolume * _masterVolume;
            ambientSource.Play();
        }

        // --- SFX ---

        public void PlaySFX(string clipName)
        {
            if (audioData == null) return;
            AudioClip clip = audioData.GetSFXByName(clipName);
            if (clip != null) PlaySFXClip(clip);
        }

        public void PlaySFXClip(AudioClip clip)
        {
            if (clip == null) return;
            AudioSource source = GetFreeSFXSource();
            if (source == null) return;

            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume;
            source.pitch = Random.Range(0.95f, 1.05f); // Leichte Pitch-Variation
            source.Play();
        }

        private AudioSource GetFreeSFXSource()
        {
            foreach (var src in _sfxPool)
                if (!src.isPlaying) return src;

            // Alle beschäftigt → ersten wiederverwenden
            if (_sfxPool.Count > 0)
            {
                _sfxPool[0].Stop();
                return _sfxPool[0];
            }
            return null;
        }

        // --- Lautstärke ---

        public void SetMasterVolume(float v)
        {
            _masterVolume = Mathf.Clamp01(v);
            ApplyVolumes();
            PlayerPrefs.SetFloat(PREF_MASTER, _masterVolume);
        }

        public void SetMusicVolume(float v)
        {
            _musicVolume = Mathf.Clamp01(v);
            if (musicSource != null) musicSource.volume = _musicVolume * _masterVolume;
            PlayerPrefs.SetFloat(PREF_MUSIC, _musicVolume);
        }

        public void SetSFXVolume(float v)
        {
            _sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(PREF_SFX, _sfxVolume);
        }

        public void SetAmbientVolume(float v)
        {
            _ambientVolume = Mathf.Clamp01(v);
            if (ambientSource != null) ambientSource.volume = _ambientVolume * _masterVolume;
            PlayerPrefs.SetFloat(PREF_AMBIENT, _ambientVolume);
        }

        public float GetMasterVolume()  => _masterVolume;
        public float GetMusicVolume()   => _musicVolume;
        public float GetSFXVolume()     => _sfxVolume;
        public float GetAmbientVolume() => _ambientVolume;

        private void ApplyVolumes()
        {
            if (musicSource   != null) musicSource.volume   = _musicVolume   * _masterVolume;
            if (ambientSource != null) ambientSource.volume = _ambientVolume * _masterVolume;
        }

        private void LoadVolumePrefs()
        {
            _masterVolume  = PlayerPrefs.GetFloat(PREF_MASTER,  1f);
            _musicVolume   = PlayerPrefs.GetFloat(PREF_MUSIC,   0.7f);
            _sfxVolume     = PlayerPrefs.GetFloat(PREF_SFX,     1f);
            _ambientVolume = PlayerPrefs.GetFloat(PREF_AMBIENT, 0.5f);
        }

        private void BuildSFXPool()
        {
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var go = new GameObject($"SFX_Pool_{i:00}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Add(src);
            }
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                var go = new GameObject("Music_Source");
                go.transform.SetParent(transform);
                musicSource = go.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
            }

            if (ambientSource == null)
            {
                var go = new GameObject("Ambient_Source");
                go.transform.SetParent(transform);
                ambientSource = go.AddComponent<AudioSource>();
                ambientSource.playOnAwake = false;
            }
        }

        // --- Event-Reaktionen ---

        private void OnGameStateChanged(GameState old, GameState next)
        {
            if (next == GameState.Paused)
            {
                musicSource?.Pause();
                ambientSource?.Pause();
            }
            else if (old == GameState.Paused)
            {
                musicSource?.UnPause();
                ambientSource?.UnPause();
            }
        }

        private void OnMachineSale(VendingMachine m, float amount)
        {
            if (audioData?.MachineSale != null) PlaySFXClip(audioData.MachineSale);
        }

        private void OnMachineStockEmpty(VendingMachine m)
        {
            if (audioData?.MachineEmpty != null) PlaySFXClip(audioData.MachineEmpty);
        }

        private void OnMachineUpgraded(VendingMachine m, int level)
        {
            if (audioData?.MachineUpgrade != null) PlaySFXClip(audioData.MachineUpgrade);
        }

        private void OnRoomUnlocked(string roomId)
        {
            if (audioData?.RoomUnlock != null) PlaySFXClip(audioData.RoomUnlock);
        }

        private void OnSynergyDiscovered(string id1, string id2)
        {
            if (audioData?.SynergyDiscovered != null) PlaySFXClip(audioData.SynergyDiscovered);
        }

        private void OnGameEventStarted(GameEventData data)
        {
            if (audioData?.EventStart != null) PlaySFXClip(audioData.EventStart);
        }

        private void OnTimeOfDayChanged(TimeOfDay time)
        {
            if (audioData == null) return;

            switch (time)
            {
                case TimeOfDay.Nacht:
                    PlayMusic(audioData.NightMusic);
                    PlayAmbient(audioData.NightAmbient);
                    break;
                case TimeOfDay.Morgen:
                    PlayMusic(audioData.ShopMusic);
                    PlayAmbient(audioData.ShopAmbient);
                    break;
            }
        }
    }
}
