using System;
using SkyRingTerrarium.World;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Audio system with hooks for ambient, weather, creature, UI, and event sounds.
    /// Music intensity scales based on world state.
    /// Note: This is the infrastructure - actual audio clips are not included.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer masterMixer;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource weatherSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Pool Settings")]
        [SerializeField] private int sfxPoolSize = 10;
        [SerializeField] private int creaturePoolSize = 15;

        // Volume settings
        private float masterVolume = 1f;
        private float musicVolume = 0.7f;
        private float sfxVolume = 1f;
        private float ambientVolume = 0.8f;

        // Music intensity
        private float targetMusicIntensity = 0f;
        private float currentMusicIntensity = 0f;
        private float musicIntensityLerpSpeed = 0.5f;

        // Audio pools
        private List<AudioSource> sfxPool;
        private List<AudioSource> creatureSoundPool;

        // Events
        public event Action<string> OnSoundPlayed;
        public event Action<float> OnMusicIntensityChanged;

        // Sound hooks (to be populated with actual clips)
        private Dictionary<string, AudioClip> soundLibrary;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            InitializeSoundPools();
            InitializeSoundLibrary();
        }

        private void Start()
        {
            // Subscribe to world events
            WorldTimeManager timeManager = FindFirstObjectByType<WorldTimeManager>();
            if (timeManager != null)
            {
                timeManager.OnDayNightChanged += HandleDayNightChange;
            }

            WeatherSystem weather = FindFirstObjectByType<WeatherSystem>();
            if (weather != null)
            {
                weather.OnWeatherChanged += HandleWeatherChange;
            }

            WorldEventManager events = FindFirstObjectByType<WorldEventManager>();
            if (events != null)
            {
                events.OnEventStarted += HandleEventStarted;
                events.OnEventEnded += HandleEventEnded;
            }
        }

        private void Update()
        {
            UpdateMusicIntensity();
        }

        #region Initialization

        private void InitializeAudioSources()
        {
            // Create sources if not assigned
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (ambientSource == null)
            {
                GameObject ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }

            if (weatherSource == null)
            {
                GameObject weatherObj = new GameObject("WeatherSource");
                weatherObj.transform.SetParent(transform);
                weatherSource = weatherObj.AddComponent<AudioSource>();
                weatherSource.loop = true;
                weatherSource.playOnAwake = false;
            }

            if (uiSource == null)
            {
                GameObject uiObj = new GameObject("UISource");
                uiObj.transform.SetParent(transform);
                uiSource = uiObj.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
            }
        }

        private void InitializeSoundPools()
        {
            sfxPool = new List<AudioSource>();
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sfxObj = new GameObject($"SFXSource_{i}");
                sfxObj.transform.SetParent(transform);
                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool.Add(source);
            }

            creatureSoundPool = new List<AudioSource>();
            for (int i = 0; i < creaturePoolSize; i++)
            {
                GameObject creatureObj = new GameObject($"CreatureSource_{i}");
                creatureObj.transform.SetParent(transform);
                AudioSource source = creatureObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f; // 3D sound
                creatureSoundPool.Add(source);
            }
        }

        private void InitializeSoundLibrary()
        {
            soundLibrary = new Dictionary<string, AudioClip>();
            // Sound library would be populated from ScriptableObject or Resources
            // This is just the hook infrastructure
        }

        #endregion

        #region Volume Control

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (musicSource != null)
                musicSource.volume = masterVolume * musicVolume;
            if (ambientSource != null)
                ambientSource.volume = masterVolume * ambientVolume;
            if (weatherSource != null)
                weatherSource.volume = masterVolume * ambientVolume;
            if (uiSource != null)
                uiSource.volume = masterVolume * sfxVolume;
        }

        #endregion

        #region Sound Playback Hooks

        /// <summary>
        /// Play a UI sound effect
        /// </summary>
        public void PlayUISound(string soundId)
        {
            if (soundLibrary.TryGetValue(soundId, out AudioClip clip))
            {
                uiSource.PlayOneShot(clip, masterVolume * sfxVolume);
            }
            OnSoundPlayed?.Invoke($"ui_{soundId}");
            Debug.Log($"[Audio] UI Sound hook: {soundId}");
        }

        /// <summary>
        /// Play a one-shot SFX
        /// </summary>
        public void PlaySFX(string soundId, Vector3 position = default)
        {
            AudioSource source = GetAvailableSource(sfxPool);
            if (source != null && soundLibrary.TryGetValue(soundId, out AudioClip clip))
            {
                source.transform.position = position;
                source.clip = clip;
                source.volume = masterVolume * sfxVolume;
                source.Play();
            }
            OnSoundPlayed?.Invoke($"sfx_{soundId}");
            Debug.Log($"[Audio] SFX hook: {soundId}");
        }

        /// <summary>
        /// Play creature sound at position
        /// </summary>
        public void PlayCreatureSound(string creatureType, string soundType, Vector3 position)
        {
            string soundId = $"{creatureType}_{soundType}";
            AudioSource source = GetAvailableSource(creatureSoundPool);
            if (source != null && soundLibrary.TryGetValue(soundId, out AudioClip clip))
            {
                source.transform.position = position;
                source.clip = clip;
                source.volume = masterVolume * sfxVolume;
                source.Play();
            }
            OnSoundPlayed?.Invoke($"creature_{soundId}");
            Debug.Log($"[Audio] Creature sound hook: {soundId} at {position}");
        }

        /// <summary>
        /// Set ambient loop for day/night
        /// </summary>
        public void SetAmbientLoop(string ambientId)
        {
            if (soundLibrary.TryGetValue(ambientId, out AudioClip clip))
            {
                CrossfadeAudioSource(ambientSource, clip);
            }
            Debug.Log($"[Audio] Ambient hook: {ambientId}");
        }

        /// <summary>
        /// Set weather ambient loop
        /// </summary>
        public void SetWeatherLoop(string weatherId)
        {
            if (string.IsNullOrEmpty(weatherId))
            {
                FadeOutSource(weatherSource);
            }
            else if (soundLibrary.TryGetValue(weatherId, out AudioClip clip))
            {
                CrossfadeAudioSource(weatherSource, clip);
            }
            Debug.Log($"[Audio] Weather hook: {weatherId}");
        }

        /// <summary>
        /// Play event sound
        /// </summary>
        public void PlayEventSound(string eventType, bool isStart)
        {
            string soundId = $"event_{eventType}_{(isStart ? "start" : "end")}";
            PlaySFX(soundId);
            Debug.Log($"[Audio] Event hook: {soundId}");
        }

        /// <summary>
        /// Set music track
        /// </summary>
        public void SetMusicTrack(string trackId)
        {
            if (soundLibrary.TryGetValue(trackId, out AudioClip clip))
            {
                CrossfadeAudioSource(musicSource, clip);
            }
            Debug.Log($"[Audio] Music hook: {trackId}");
        }

        #endregion

        #region Music Intensity

        public void SetMusicIntensity(float intensity)
        {
            targetMusicIntensity = Mathf.Clamp01(intensity);
        }

        private void UpdateMusicIntensity()
        {
            if (Mathf.Abs(currentMusicIntensity - targetMusicIntensity) > 0.01f)
            {
                currentMusicIntensity = Mathf.Lerp(currentMusicIntensity, targetMusicIntensity,
                    Time.deltaTime * musicIntensityLerpSpeed);
                OnMusicIntensityChanged?.Invoke(currentMusicIntensity);

                // Could be used to blend between calm/intense music layers
                // or adjust music parameters
            }
        }

        public float GetMusicIntensity() => currentMusicIntensity;

        #endregion

        #region Event Handlers

        private void HandleDayNightChange(bool isDay)
        {
            SetAmbientLoop(isDay ? "ambient_day" : "ambient_night");
            SetMusicIntensity(isDay ? 0.3f : 0.1f);
        }

        private void HandleWeatherChange(WeatherSystem.WeatherState weather)
        {
            string weatherSound = weather switch
            {
                WeatherSystem.WeatherState.Clear => null,
                WeatherSystem.WeatherState.Windy => "weather_wind",
                WeatherSystem.WeatherState.Stormy => "weather_storm",
                WeatherSystem.WeatherState.Misty => "weather_motes",
                WeatherSystem.WeatherState.Calm => "weather_aurora",
                _ => null
            };
            SetWeatherLoop(weatherSound);
        }

        private void HandleEventStarted(WorldEventManager.WorldEventType eventType)
        {
            PlayEventSound(eventType.ToString(), true);
            SetMusicIntensity(0.8f);
        }

        private void HandleEventEnded(WorldEventManager.WorldEventType eventType)
        {
            PlayEventSound(eventType.ToString(), false);
            SetMusicIntensity(0.3f);
        }

        #endregion

        #region Utility

        private AudioSource GetAvailableSource(List<AudioSource> pool)
        {
            foreach (var source in pool)
            {
                if (!source.isPlaying)
                    return source;
            }
            return pool.Count > 0 ? pool[0] : null;
        }

        private void CrossfadeAudioSource(AudioSource source, AudioClip newClip)
        {
            // Simple crossfade - could be expanded with coroutine
            if (source.clip == newClip) return;
            source.clip = newClip;
            source.Play();
        }

        private void FadeOutSource(AudioSource source)
        {
            source.Stop();
        }

        #endregion

        private void OnDestroy()
        {
            WorldTimeManager timeManager = FindFirstObjectByType<WorldTimeManager>();
            if (timeManager != null)
            {
                timeManager.OnDayNightChanged -= HandleDayNightChange;
            }

            WeatherSystem weather = FindFirstObjectByType<WeatherSystem>();
            if (weather != null)
            {
                weather.OnWeatherChanged -= HandleWeatherChange;
            }

            WorldEventManager events = FindFirstObjectByType<WorldEventManager>();
            if (events != null)
            {
                events.OnEventStarted -= HandleEventStarted;
                events.OnEventEnded -= HandleEventEnded;
            }
        }
    }
}
