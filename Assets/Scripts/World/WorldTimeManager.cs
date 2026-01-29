using UnityEngine;
using System;

namespace SkyRingTerrarium.World
{
    /// <summary>
    /// Manages the world's time progression including day/night cycle and seasons.
    /// Time flows continuously whether player watches or not.
    /// </summary>
    public class WorldTimeManager : MonoBehaviour
    {
        public static WorldTimeManager Instance { get; private set; }

        #region Events
        public static event Action<float> OnTimeOfDayChanged;
        public static event Action<TimeOfDay> OnTimeOfDayPhaseChanged;
        public static event Action<Season> OnSeasonChanged;
        public static event Action<int> OnDayChanged;
        public static event Action<int> OnYearChanged;
        #endregion

        #region Enums
        public enum TimeOfDay { Dawn, Day, Dusk, Night }
        public enum Season { Spring, Summer, Autumn, Winter }
        #endregion

        #region Serialized Fields
        [Header("Time Configuration")]
        [Tooltip("Real-time minutes for one full game day (24 hours)")]
        [SerializeField] private float realMinutesPerGameDay = 10f;

        [Tooltip("How many game days per season")]
        [SerializeField] private int daysPerSeason = 7;

        [Header("Time of Day Thresholds")]
        [SerializeField] private float dawnStart = 0.2f;
        [SerializeField] private float dayStart = 0.3f;
        [SerializeField] private float duskStart = 0.7f;
        [SerializeField] private float nightStart = 0.8f;

        [Header("Visual Settings")]
        [SerializeField] private Gradient skyColorGradient;
        [SerializeField] private Gradient ambientColorGradient;
        [SerializeField] private AnimationCurve lightIntensityCurve;

        [Header("Light References (Optional)")]
        [SerializeField] private Light directionalLight;
        // Note: Light2D removed - use URP/Built-in lights only

        [Header("Debug")]
        [SerializeField] private bool pauseTime = false;
        [SerializeField] private float debugTimeScale = 1f;
        #endregion

        #region Private Fields
        private float currentTimeOfDay = 0.25f; // Start at dawn
        private TimeOfDay currentPhase = TimeOfDay.Dawn;
        private Season currentSeason = Season.Spring;
        private int currentDay = 1;
        private int currentYear = 1;
        
        private float timeProgressionRate;
        #endregion

        #region Properties
        public float CurrentTimeOfDay => currentTimeOfDay;
        public TimeOfDay CurrentPhase => currentPhase;
        public Season CurrentSeason => currentSeason;
        public int CurrentDay => currentDay;
        public int CurrentYear => currentYear;
        public bool IsPaused => pauseTime;
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CalculateTimeProgressionRate();
            InitializeGradients();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!pauseTime)
            {
                ProgressTime();
                UpdateVisuals();
            }
        }

        private void CalculateTimeProgressionRate()
        {
            // Convert real minutes to seconds, then calculate rate for 0-1 cycle
            float realSecondsPerDay = realMinutesPerGameDay * 60f;
            timeProgressionRate = 1f / realSecondsPerDay;
        }

        private void InitializeGradients()
        {
            if (skyColorGradient == null || skyColorGradient.colorKeys.Length == 0)
            {
                skyColorGradient = new Gradient();
                skyColorGradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0f),
                        new GradientColorKey(new Color(0.9f, 0.6f, 0.4f), 0.25f),
                        new GradientColorKey(new Color(0.5f, 0.7f, 0.9f), 0.5f),
                        new GradientColorKey(new Color(0.9f, 0.5f, 0.3f), 0.75f),
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f)
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }

            if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length == 0)
            {
                ambientColorGradient = new Gradient();
                ambientColorGradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.3f), 0f),
                        new GradientColorKey(new Color(0.8f, 0.7f, 0.6f), 0.25f),
                        new GradientColorKey(new Color(1f, 1f, 1f), 0.5f),
                        new GradientColorKey(new Color(0.8f, 0.6f, 0.5f), 0.75f),
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.3f), 1f)
                    },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }

            if (lightIntensityCurve == null || lightIntensityCurve.keys.Length == 0)
            {
                lightIntensityCurve = new AnimationCurve(
                    new Keyframe(0f, 0.1f),
                    new Keyframe(0.25f, 0.5f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(0.75f, 0.5f),
                    new Keyframe(1f, 0.1f)
                );
            }
        }

        private void ProgressTime()
        {
            float previousTime = currentTimeOfDay;
            currentTimeOfDay += timeProgressionRate * Time.deltaTime * debugTimeScale;

            if (currentTimeOfDay >= 1f)
            {
                currentTimeOfDay -= 1f;
                AdvanceDay();
            }

            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
            UpdateTimePhase();
        }

        private void UpdateTimePhase()
        {
            TimeOfDay newPhase;
            
            if (currentTimeOfDay < dawnStart || currentTimeOfDay >= nightStart)
            {
                newPhase = TimeOfDay.Night;
            }
            else if (currentTimeOfDay < dayStart)
            {
                newPhase = TimeOfDay.Dawn;
            }
            else if (currentTimeOfDay < duskStart)
            {
                newPhase = TimeOfDay.Day;
            }
            else
            {
                newPhase = TimeOfDay.Dusk;
            }

            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnTimeOfDayPhaseChanged?.Invoke(currentPhase);
                Debug.Log($"[WorldTime] Phase changed to: {currentPhase}");
            }
        }

        private void AdvanceDay()
        {
            currentDay++;
            OnDayChanged?.Invoke(currentDay);

            if (currentDay > daysPerSeason)
            {
                currentDay = 1;
                AdvanceSeason();
            }
        }

        private void AdvanceSeason()
        {
            currentSeason = (Season)(((int)currentSeason + 1) % 4);
            OnSeasonChanged?.Invoke(currentSeason);
            Debug.Log($"[WorldTime] Season changed to: {currentSeason}");

            if (currentSeason == Season.Spring)
            {
                currentYear++;
                OnYearChanged?.Invoke(currentYear);
                Debug.Log($"[WorldTime] Year changed to: {currentYear}");
            }
        }

        private void UpdateVisuals()
        {
            // Update sky color
            Color skyColor = skyColorGradient.Evaluate(currentTimeOfDay);
            RenderSettings.skybox?.SetColor("_Tint", skyColor);

            // Update ambient color
            Color ambientColor = ambientColorGradient.Evaluate(currentTimeOfDay);
            RenderSettings.ambientLight = ambientColor;

            // Update directional light
            if (directionalLight != null)
            {
                directionalLight.intensity = lightIntensityCurve.Evaluate(currentTimeOfDay);
                directionalLight.color = ambientColor;

                // Rotate sun based on time
                float sunAngle = currentTimeOfDay * 360f - 90f;
                directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            }
        }

        #region Public API

        public void SetTime(float normalizedTime)
        {
            currentTimeOfDay = Mathf.Clamp01(normalizedTime);
            UpdateTimePhase();
            UpdateVisuals();
            OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
        }

        public void SetDay(int day)
        {
            currentDay = Mathf.Max(1, day);
            while (currentDay > daysPerSeason)
            {
                currentDay -= daysPerSeason;
                AdvanceSeason();
            }
            OnDayChanged?.Invoke(currentDay);
        }

        public void SetSeason(Season season)
        {
            currentSeason = season;
            OnSeasonChanged?.Invoke(currentSeason);
        }

        public void PauseTime(bool pause)
        {
            pauseTime = pause;
        }

        public void SetTimeScale(float scale)
        {
            debugTimeScale = Mathf.Max(0f, scale);
        }

        public string GetFormattedTime()
        {
            int hours = Mathf.FloorToInt(currentTimeOfDay * 24f);
            int minutes = Mathf.FloorToInt((currentTimeOfDay * 24f - hours) * 60f);
            return $"{hours:D2}:{minutes:D2}";
        }

        public string GetFormattedDate()
        {
            return $"Year {currentYear}, {currentSeason}, Day {currentDay}";
        }

        #endregion
    }
}