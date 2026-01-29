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
        #endregion

        #region Private Fields
        private float timeOfDayNormalized = 0.3f;
        private int currentDay = 1;
        private int currentYear = 1;
        private Season currentSeason = Season.Spring;
        private TimeOfDay currentTimePhase = TimeOfDay.Day;
        private float timeScale = 1f;
        private bool isPaused = false;
        #endregion

        #region Public Properties
        public float TimeOfDayNormalized => timeOfDayNormalized;
        public TimeOfDay CurrentTimeOfDayPhase => currentTimePhase;
        public Season CurrentSeason => currentSeason;
        public int CurrentDay => currentDay;
        public int CurrentYear => currentYear;
        public float TimeScale { get => timeScale; set => timeScale = Mathf.Max(0f, value); }
        public bool IsPaused { get => isPaused; set => isPaused = value; }
        public bool IsDay => currentTimePhase == TimeOfDay.Day || currentTimePhase == TimeOfDay.Dawn;
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
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
            if (!isPaused)
            {
                AdvanceTime(Time.deltaTime);
            }
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
                        new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f),
                        new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.5f),
                        new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.75f),
                        new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1f)
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

        private void AdvanceTime(float deltaTime)
        {
            float dayProgressionPerSecond = 1f / (realMinutesPerGameDay * 60f);
            float timeAdvance = dayProgressionPerSecond * deltaTime * timeScale;
            
            float previousTime = timeOfDayNormalized;
            timeOfDayNormalized += timeAdvance;
            
            if (timeOfDayNormalized >= 1f)
            {
                timeOfDayNormalized -= 1f;
                AdvanceDay();
            }
            
            OnTimeOfDayChanged?.Invoke(timeOfDayNormalized);
            
            UpdateTimePhase();
            UpdateVisuals();
        }

        private void UpdateTimePhase()
        {
            TimeOfDay newPhase;
            
            if (timeOfDayNormalized < dawnStart || timeOfDayNormalized >= nightStart)
                newPhase = TimeOfDay.Night;
            else if (timeOfDayNormalized < dayStart)
                newPhase = TimeOfDay.Dawn;
            else if (timeOfDayNormalized < duskStart)
                newPhase = TimeOfDay.Day;
            else
                newPhase = TimeOfDay.Dusk;
            
            if (newPhase != currentTimePhase)
            {
                currentTimePhase = newPhase;
                OnTimeOfDayPhaseChanged?.Invoke(currentTimePhase);
            }
        }

        private void UpdateVisuals()
        {
            if (directionalLight != null)
            {
                directionalLight.intensity = lightIntensityCurve.Evaluate(timeOfDayNormalized);
                directionalLight.color = skyColorGradient.Evaluate(timeOfDayNormalized);
            }
            
            if (ambientColorGradient != null)
            {
                RenderSettings.ambientLight = ambientColorGradient.Evaluate(timeOfDayNormalized);
            }
        }

        private void AdvanceDay()
        {
            currentDay++;
            OnDayChanged?.Invoke(currentDay);
            
            if (currentDay > daysPerSeason * 4)
            {
                currentDay = 1;
                currentYear++;
                OnYearChanged?.Invoke(currentYear);
            }
            
            UpdateSeason();
        }

        private void UpdateSeason()
        {
            int dayInYear = ((currentDay - 1) % (daysPerSeason * 4)) + 1;
            Season newSeason;
            
            if (dayInYear <= daysPerSeason)
                newSeason = Season.Spring;
            else if (dayInYear <= daysPerSeason * 2)
                newSeason = Season.Summer;
            else if (dayInYear <= daysPerSeason * 3)
                newSeason = Season.Autumn;
            else
                newSeason = Season.Winter;
            
            if (newSeason != currentSeason)
            {
                currentSeason = newSeason;
                OnSeasonChanged?.Invoke(currentSeason);
            }
        }

        #region Public Methods
        public void SetTime(float normalizedTime)
        {
            timeOfDayNormalized = Mathf.Clamp01(normalizedTime);
            UpdateTimePhase();
            UpdateVisuals();
            OnTimeOfDayChanged?.Invoke(timeOfDayNormalized);
        }

        public void SetTime(float normalizedTime, int day, Season season)
        {
            timeOfDayNormalized = Mathf.Clamp01(normalizedTime);
            currentDay = Mathf.Max(1, day);
            currentSeason = season;
            UpdateTimePhase();
            UpdateVisuals();
            OnTimeOfDayChanged?.Invoke(timeOfDayNormalized);
            OnDayChanged?.Invoke(currentDay);
            OnSeasonChanged?.Invoke(currentSeason);
        }

        public void SetDay(int day)
        {
            currentDay = Mathf.Max(1, day);
            UpdateSeason();
            OnDayChanged?.Invoke(currentDay);
        }

        public void SetSeason(Season season)
        {
            currentSeason = season;
            OnSeasonChanged?.Invoke(currentSeason);
        }

        public Color GetCurrentSkyColor()
        {
            return skyColorGradient.Evaluate(timeOfDayNormalized);
        }

        public float GetCurrentLightIntensity()
        {
            return lightIntensityCurve.Evaluate(timeOfDayNormalized);
        }

        public TimeState GetCurrentTimeState()
        {
            return new TimeState
            {
                timeOfDay = timeOfDayNormalized,
                day = currentDay,
                year = currentYear,
                season = currentSeason,
                phase = currentTimePhase
            };
        }

        public void LoadTimeState(TimeState state)
        {
            timeOfDayNormalized = state.timeOfDay;
            currentDay = state.day;
            currentYear = state.year;
            currentSeason = state.season;
            currentTimePhase = state.phase;
            UpdateVisuals();
        }
        #endregion
    }

    [Serializable]
    public struct TimeState
    {
        public float timeOfDay;
        public int day;
        public int year;
        public WorldTimeManager.Season season;
        public WorldTimeManager.TimeOfDay phase;
    }
}
