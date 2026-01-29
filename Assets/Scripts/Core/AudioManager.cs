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

            InitializeAudioPools();
            InitializeSoundLibrary();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateMusicIntensity();
        }

        private void SubscribeToEvents()
        {
            // Subscribe to time changes
            WorldTimeManager.OnTimeOfDayPhaseChanged += HandleTimeOfDayChanged;
            
            // Subscribe to weather changes (static events)
            WeatherSystem.OnWeatherChanged += HandleWeatherChanged;
            
            // Subscribe to world events (static events)
            WorldEventManager.OnEventStarted += HandleEventStarted;
            WorldEventManager.OnEventEnded += HandleEventEnded;
        }

        private void UnsubscribeFromEvents()
        {
            WorldTimeManager.OnTimeOfDayPhaseChanged -= HandleTimeOfDayChanged;
            WeatherSystem.OnWeatherChanged -= HandleWeatherChanged;
            WorldEventManager.OnEventStarted -= HandleEventStarted;
            WorldEventManager.OnEventEnded -= HandleEventEnded;
        }

        private void InitializeAudioPools()
        {
            sfxPool = new List<AudioSource>();
            creatureSoundPool = new List<AudioSource>();

            GameObject sfxPoolParent = new GameObject("SFX Pool");
            sfxPoolParent.transform.SetParent(transform);
            for (int i = 0; i < sfxPoolSize; i++)
            {
                AudioSource source = sfxPoolParent.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool.Add(source);
            }

            GameObject creaturePoolParent = new GameObject("Creature Sound Pool");
            creaturePoolParent.transform.SetParent(transform);
            for (int i = 0; i < creaturePoolSize; i++)
            {
                AudioSource source = creaturePoolParent.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                creatureSoundPool.Add(source);
            }
        }

        private void InitializeSoundLibrary()
        {
            soundLibrary = new Dictionary<string, AudioClip>();
            // Clips would be loaded here in a full implementation
        }

        private void UpdateMusicIntensity()
        {
            if (Mathf.Abs(currentMusicIntensity - targetMusicIntensity) > 0.001f)
            {
                currentMusicIntensity = Mathf.Lerp(currentMusicIntensity, targetMusicIntensity, 
                    musicIntensityLerpSpeed * Time.deltaTime);
                OnMusicIntensityChanged?.Invoke(currentMusicIntensity);
            }
        }

        // Event handlers with correct signatures
        private void HandleTimeOfDayChanged(WorldTimeManager.TimeOfDay timeOfDay)
        {
            // Adjust ambient audio based on time of day
            switch (timeOfDay)
            {
                case WorldTimeManager.TimeOfDay.Dawn:
                    targetMusicIntensity = 0.3f;
                    break;
                case WorldTimeManager.TimeOfDay.Day:
                    targetMusicIntensity = 0.5f;
                    break;
                case WorldTimeManager.TimeOfDay.Dusk:
                    targetMusicIntensity = 0.4f;
                    break;
                case WorldTimeManager.TimeOfDay.Night:
                    targetMusicIntensity = 0.2f;
                    break;
            }
        }

        private void HandleWeatherChanged(WeatherSystem.WeatherState weatherState)
        {
            // Adjust weather audio
            switch (weatherState)
            {
                case WeatherSystem.WeatherState.Clear:
                    if (weatherSource != null) weatherSource.volume = 0.1f;
                    break;
                case WeatherSystem.WeatherState.Windy:
                    if (weatherSource != null) weatherSource.volume = 0.5f;
                    break;
                case WeatherSystem.WeatherState.Stormy:
                    if (weatherSource != null) weatherSource.volume = 1f;
                    targetMusicIntensity = Mathf.Max(targetMusicIntensity, 0.7f);
                    break;
                case WeatherSystem.WeatherState.Calm:
                    if (weatherSource != null) weatherSource.volume = 0.05f;
                    break;
                case WeatherSystem.WeatherState.Misty:
                    if (weatherSource != null) weatherSource.volume = 0.3f;
                    break;
            }
        }

        private void HandleEventStarted(WorldEvent worldEvent)
        {
            // Increase music intensity during events
            targetMusicIntensity = Mathf.Max(targetMusicIntensity, 0.8f);
            Debug.Log($"[Audio] Event started: {worldEvent.Type}");
        }

        private void HandleEventEnded(WorldEvent worldEvent)
        {
            // Return to normal intensity
            targetMusicIntensity = 0.5f;
            Debug.Log($"[Audio] Event ended: {worldEvent.Type}");
        }

        #region Public API

        public void PlaySound(string soundName)
        {
            if (soundLibrary.TryGetValue(soundName, out AudioClip clip))
            {
                AudioSource source = GetAvailableSource(sfxPool);
                if (source != null)
                {
                    source.clip = clip;
                    source.volume = sfxVolume * masterVolume;
                    source.Play();
                    OnSoundPlayed?.Invoke(soundName);
                }
            }
        }

        public void PlaySoundAtPosition(string soundName, Vector3 position)
        {
            if (soundLibrary.TryGetValue(soundName, out AudioClip clip))
            {
                AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume);
                OnSoundPlayed?.Invoke(soundName);
            }
        }

        public void PlayCreatureSound(AudioClip clip, Vector3 position, float volume = 1f)
        {
            AudioSource source = GetAvailableSource(creatureSoundPool);
            if (source != null)
            {
                source.transform.position = position;
                source.clip = clip;
                source.volume = volume * sfxVolume * masterVolume;
                source.Play();
            }
        }

        public void PlayUISound(string soundName)
        {
            if (uiSource != null && soundLibrary.TryGetValue(soundName, out AudioClip clip))
            {
                uiSource.PlayOneShot(clip, sfxVolume * masterVolume);
                OnSoundPlayed?.Invoke(soundName);
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateMixerVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            if (ambientSource != null)
            {
                ambientSource.volume = ambientVolume * masterVolume;
            }
        }

        #endregion

        private AudioSource GetAvailableSource(List<AudioSource> pool)
        {
            foreach (var source in pool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }
            return pool.Count > 0 ? pool[0] : null;
        }

        private void UpdateMixerVolumes()
        {
            if (masterMixer != null)
            {
                float dbVolume = masterVolume > 0 ? 20f * Mathf.Log10(masterVolume) : -80f;
                masterMixer.SetFloat("MasterVolume", dbVolume);
            }
        }
    }
}