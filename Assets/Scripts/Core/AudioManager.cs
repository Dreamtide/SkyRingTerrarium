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

        // Ambient loop reference
        private AudioClip currentAmbientLoop;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePools();
            soundLibrary = new Dictionary<string, AudioClip>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            SubscribeToWorldEvents();
        }

        private void Update()
        {
            UpdateMusicIntensity();
        }

        private void InitializePools()
        {
            sfxPool = new List<AudioSource>();
            creatureSoundPool = new List<AudioSource>();

            for (int i = 0; i < sfxPoolSize; i++)
            {
                var source = CreatePooledSource("SFX_" + i);
                sfxPool.Add(source);
            }

            for (int i = 0; i < creaturePoolSize; i++)
            {
                var source = CreatePooledSource("Creature_" + i);
                creatureSoundPool.Add(source);
            }
        }

        private AudioSource CreatePooledSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        private void SubscribeToWorldEvents()
        {
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.OnWeatherChanged += OnWeatherChanged;
            }
            
            WorldEventManager.OnEventStarted += OnWorldEventStarted;
            WorldEventManager.OnEventEnded += OnWorldEventEnded;
        }

        private void UpdateMusicIntensity()
        {
            if (Mathf.Abs(currentMusicIntensity - targetMusicIntensity) > 0.01f)
            {
                currentMusicIntensity = Mathf.Lerp(currentMusicIntensity, targetMusicIntensity, 
                    musicIntensityLerpSpeed * Time.deltaTime);
                OnMusicIntensityChanged?.Invoke(currentMusicIntensity);
            }
        }

        #region Public Audio Methods
        public void PlaySound(string soundId, Vector3 position = default)
        {
            if (soundLibrary.TryGetValue(soundId, out AudioClip clip))
            {
                var source = GetAvailableSFXSource();
                if (source != null)
                {
                    source.transform.position = position;
                    source.clip = clip;
                    source.volume = sfxVolume * masterVolume;
                    source.Play();
                    OnSoundPlayed?.Invoke(soundId);
                }
            }
        }

        public void PlayUISound(AudioClip clip)
        {
            if (uiSource != null && clip != null)
            {
                uiSource.PlayOneShot(clip, sfxVolume * masterVolume);
            }
        }

        public void PlayCreatureSound(AudioClip clip, Vector3 position, float volume = 1f)
        {
            var source = GetAvailableCreatureSource();
            if (source != null && clip != null)
            {
                source.transform.position = position;
                source.clip = clip;
                source.volume = volume * sfxVolume * masterVolume;
                source.Play();
            }
        }

        /// <summary>
        /// Sets the ambient loop audio clip.
        /// </summary>
        public void SetAmbientLoop(AudioClip clip)
        {
            currentAmbientLoop = clip;
            if (ambientSource != null)
            {
                ambientSource.clip = clip;
                ambientSource.loop = true;
                if (clip != null && !ambientSource.isPlaying)
                {
                    ambientSource.volume = ambientVolume * masterVolume;
                    ambientSource.Play();
                }
                else if (clip == null)
                {
                    ambientSource.Stop();
                }
            }
        }

        public void SetMusicIntensity(float intensity)
        {
            targetMusicIntensity = Mathf.Clamp01(intensity);
        }

        public void SetVolume(AudioChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);
            switch (channel)
            {
                case AudioChannel.Master:
                    masterVolume = volume;
                    break;
                case AudioChannel.Music:
                    musicVolume = volume;
                    if (musicSource != null) musicSource.volume = musicVolume * masterVolume;
                    break;
                case AudioChannel.SFX:
                    sfxVolume = volume;
                    break;
                case AudioChannel.Ambient:
                    ambientVolume = volume;
                    if (ambientSource != null) ambientSource.volume = ambientVolume * masterVolume;
                    break;
            }
        }

        public float GetVolume(AudioChannel channel)
        {
            return channel switch
            {
                AudioChannel.Master => masterVolume,
                AudioChannel.Music => musicVolume,
                AudioChannel.SFX => sfxVolume,
                AudioChannel.Ambient => ambientVolume,
                _ => 1f
            };
        }

        public void RegisterSound(string id, AudioClip clip)
        {
            if (!string.IsNullOrEmpty(id) && clip != null)
            {
                soundLibrary[id] = clip;
            }
        }
        #endregion

        #region Event Handlers
        private void OnWeatherChanged(WeatherSystem.WeatherState newWeather)
        {
            switch (newWeather)
            {
                case WeatherSystem.WeatherState.Stormy:
                    SetMusicIntensity(0.8f);
                    break;
                case WeatherSystem.WeatherState.Windy:
                    SetMusicIntensity(0.5f);
                    break;
                case WeatherSystem.WeatherState.Clear:
                case WeatherSystem.WeatherState.Calm:
                    SetMusicIntensity(0.3f);
                    break;
                case WeatherSystem.WeatherState.Misty:
                    SetMusicIntensity(0.4f);
                    break;
            }
        }

        private void OnWorldEventStarted(WorldEvent worldEvent)
        {
            switch (worldEvent.Type)
            {
                case WorldEventManager.WorldEventType.MeteorShower:
                case WorldEventManager.WorldEventType.SolarFlare:
                    SetMusicIntensity(0.9f);
                    break;
                case WorldEventManager.WorldEventType.AuroraWave:
                case WorldEventManager.WorldEventType.HarmonicResonance:
                    SetMusicIntensity(0.6f);
                    break;
            }
        }

        private void OnWorldEventEnded(WorldEvent worldEvent)
        {
            SetMusicIntensity(0.3f);
        }
        #endregion

        #region Helper Methods
        private AudioSource GetAvailableSFXSource()
        {
            foreach (var source in sfxPool)
            {
                if (!source.isPlaying)
                    return source;
            }
            return sfxPool.Count > 0 ? sfxPool[0] : null;
        }

        private AudioSource GetAvailableCreatureSource()
        {
            foreach (var source in creatureSoundPool)
            {
                if (!source.isPlaying)
                    return source;
            }
            return creatureSoundPool.Count > 0 ? creatureSoundPool[0] : null;
        }
        #endregion

        public enum AudioChannel
        {
            Master,
            Music,
            SFX,
            Ambient
        }
    }
}
