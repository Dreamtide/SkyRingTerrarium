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

        [Header("Time of Day Thresholds (0-1)")]
        [SerializeField] private float dawnStart = 0.2f;
        [SerializeField] private float dayStart = 0.3f;
        [SerializeField] private float duskStart = 0.7f;
        [SerializeField] private float nightStart = 0.85f;

        [Header("Ambient Colors")]
        [SerializeField] private Color dawnColor = new Color(1f, 0.7f, 0.4f, 1f);
        [SerializeField] private Color dayColor = new Color(1f, 1f, 0.95f, 1f);
        [SerializeField] private Color duskColor = new Color(0.8f, 0.4f, 0.6f, 1f);
        [SerializeField] private Color nightColor = new Color(0.1f, 0.1f, 0.3f, 1f);

        [Header("Light Intensity")]
        [SerializeField] private float dawnIntensity = 0.5f;
        [SerializeField] private float dayIntensity = 1f;
        [SerializeField] private float duskIntensity = 0.4f;
        [SerializeField] private float nightIntensity = 0.1f;

        [Header("Season Modifiers")]
        [SerializeField] private float springDayLengthMod = 1f;
        [SerializeField] private float summerDayLengthMod = 1.3f;
        [SerializeField] private float autumnDayLengthMod = 1f;
        [SerializeField] private float winterDayLengthMod = 0.7f;

        [Header("References")]
        [SerializeField] private Light sunLight; // Use regular Light instead of Light2D for compatibility
        [SerializeField] private Transform sunPivot;
        #endregion

        #region Public Properties
        public float TimeOfDayNormalized => timeOfDayNormalized;
        public TimeOfDay CurrentTimeOfDayPhase => currentPhase;
        public Season CurrentSeason => currentSeason;
        public int CurrentDay => currentDay;
        public int CurrentYear => currentYear;
        public float DayProgress => timeOfDayNormalized;
        public bool IsDay => currentPhase == TimeOfDay.Day || currentPhase == TimeOfDay.Dawn;
        public bool IsNight => currentPhase == TimeOfDay.Night || currentPhase == TimeOfDay.Dusk;
        public Color CurrentAmbientColor => currentAmbientColor;
        public float CurrentLightIntensity => currentLightIntensity;
        public float SeasonProgress => (float)(currentDay % daysPerSeason) / daysPerSeason;
        #endregion

        #region Private Fields
        private float timeOfDayNormalized;
        private TimeOfDay currentPhase;
        private TimeOfDay previousPhase;
        private Season currentSeason;
        private Season previousSeason;
        private int currentDay;
        private int previousDay;
        private int currentYear;
        private int previousYear;
        private Color currentAmbientColor;
        private float currentLightIntensity;
        private float secondsPerGameDay;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CalculateSecondsPerDay();
            UpdateTimeOfDayPhase();
            UpdateSeason();
        }

        private void Update()
        {
            AdvanceTime(Time.deltaTime);
            UpdateVisuals();
        }

        private void OnValidate()
        {
            CalculateSecondsPerDay();
        }
        #endregion

        #region Time Progression
        private void CalculateSecondsPerDay()
        {
            float seasonMod = GetSeasonDayLengthModifier();
            secondsPerGameDay = realMinutesPerGameDay * 60f * seasonMod;
        }

        private float GetSeasonDayLengthModifier()
        {
            return currentSeason switch
            {
                Season.Spring => springDayLengthMod,
                Season.Summer => summerDayLengthMod,
                Season.Autumn => autumnDayLengthMod,
                Season.Winter => winterDayLengthMod,
                _ => 1f
            };
        }

        public void AdvanceTime(float deltaSeconds)
        {
            if (secondsPerGameDay <= 0) return;

            float dayDelta = deltaSeconds / secondsPerGameDay;
            timeOfDayNormalized += dayDelta;

            while (timeOfDayNormalized >= 1f)
            {
                timeOfDayNormalized -= 1f;
                AdvanceDay();
            }

            OnTimeOfDayChanged?.Invoke(timeOfDayNormalized);
            UpdateTimeOfDayPhase();
        }

        private void AdvanceDay()
        {
            currentDay++;

            if (currentDay != previousDay)
            {
                OnDayChanged?.Invoke(currentDay);
                previousDay = currentDay;
            }

            UpdateSeason();
        }

        private void UpdateTimeOfDayPhase()
        {
            if (timeOfDayNormalized < dawnStart)
                currentPhase = TimeOfDay.Night;
            else if (timeOfDayNormalized < dayStart)
                currentPhase = TimeOfDay.Dawn;
            else if (timeOfDayNormalized < duskStart)
                currentPhase = TimeOfDay.Day;
            else if (timeOfDayNormalized < nightStart)
                currentPhase = TimeOfDay.Dusk;
            else
                currentPhase = TimeOfDay.Night;

            if (currentPhase != previousPhase)
            {
                OnTimeOfDayPhaseChanged?.Invoke(currentPhase);
                previousPhase = currentPhase;
            }
        }

        private void UpdateSeason()
        {
            int totalSeasonDays = daysPerSeason * 4;
            int dayInYear = currentDay % totalSeasonDays;
            int seasonIndex = dayInYear / daysPerSeason;
            currentSeason = (Season)seasonIndex;

            int year = currentDay / totalSeasonDays;
            if (year != currentYear)
            {
                currentYear = year;
                if (currentYear != previousYear)
                {
                    OnYearChanged?.Invoke(currentYear);
                    previousYear = currentYear;
                }
            }

            if (currentSeason != previousSeason)
            {
                OnSeasonChanged?.Invoke(currentSeason);
                previousSeason = currentSeason;
                CalculateSecondsPerDay();
            }
        }
        #endregion

        #region Visual Updates
        private void UpdateVisuals()
        {
            UpdateAmbientColor();
            UpdateLightIntensity();
            UpdateSunRotation();
        }

        private void UpdateAmbientColor()
        {
            if (timeOfDayNormalized < dawnStart)
            {
                currentAmbientColor = nightColor;
            }
            else if (timeOfDayNormalized < dayStart)
            {
                float t = (timeOfDayNormalized - dawnStart) / (dayStart - dawnStart);
                currentAmbientColor = Color.Lerp(nightColor, dawnColor, t * 0.5f);
                currentAmbientColor = Color.Lerp(currentAmbientColor, dayColor, t * 0.5f);
            }
            else if (timeOfDayNormalized < duskStart)
            {
                float midDay = (dayStart + duskStart) / 2f;
                if (timeOfDayNormalized < midDay)
                {
                    float t = (timeOfDayNormalized - dayStart) / (midDay - dayStart);
                    currentAmbientColor = Color.Lerp(dawnColor, dayColor, t);
                }
                else
                {
                    float t = (timeOfDayNormalized - midDay) / (duskStart - midDay);
                    currentAmbientColor = Color.Lerp(dayColor, duskColor, t * 0.3f);
                }
            }
            else if (timeOfDayNormalized < nightStart)
            {
                float t = (timeOfDayNormalized - duskStart) / (nightStart - duskStart);
                currentAmbientColor = Color.Lerp(duskColor, nightColor, t);
            }
            else
            {
                currentAmbientColor = nightColor;
            }

            ApplySeasonColorTint();

            if (sunLight != null)
            {
                sunLight.color = currentAmbientColor;
            }
        }

        private void ApplySeasonColorTint()
        {
            Color seasonTint = currentSeason switch
            {
                Season.Spring => new Color(0.95f, 1f, 0.9f, 1f),
                Season.Summer => new Color(1f, 1f, 0.85f, 1f),
                Season.Autumn => new Color(1f, 0.9f, 0.8f, 1f),
                Season.Winter => new Color(0.9f, 0.95f, 1f, 1f),
                _ => Color.white
            };
            currentAmbientColor *= seasonTint;
        }

        private void UpdateLightIntensity()
        {
            if (timeOfDayNormalized < dawnStart)
            {
                currentLightIntensity = nightIntensity;
            }
            else if (timeOfDayNormalized < dayStart)
            {
                float t = (timeOfDayNormalized - dawnStart) / (dayStart - dawnStart);
                currentLightIntensity = Mathf.Lerp(nightIntensity, dawnIntensity, t);
            }
            else if (timeOfDayNormalized < duskStart)
            {
                float midDay = (dayStart + duskStart) / 2f;
                if (timeOfDayNormalized < midDay)
                {
                    float t = (timeOfDayNormalized - dayStart) / (midDay - dayStart);
                    currentLightIntensity = Mathf.Lerp(dawnIntensity, dayIntensity, t);
                }
                else
                {
                    float t = (timeOfDayNormalized - midDay) / (duskStart - midDay);
                    currentLightIntensity = Mathf.Lerp(dayIntensity, duskIntensity, t);
                }
            }
            else if (timeOfDayNormalized < nightStart)
            {
                float t = (timeOfDayNormalized - duskStart) / (nightStart - duskStart);
                currentLightIntensity = Mathf.Lerp(duskIntensity, nightIntensity, t);
            }
            else
            {
                currentLightIntensity = nightIntensity;
            }

            if (sunLight != null)
            {
                sunLight.intensity = currentLightIntensity;
            }
        }

        private void UpdateSunRotation()
        {
            if (sunPivot != null)
            {
                float angle = timeOfDayNormalized * 360f;
                sunPivot.localRotation = Quaternion.Euler(0, 0, -angle);
            }
        }
        #endregion

        #region Offline Progression
        public WorldTimeState SaveState()
        {
            return new WorldTimeState
            {
                timeOfDayNormalized = timeOfDayNormalized,
                currentDay = currentDay,
                currentYear = currentYear,
                lastSaveTime = DateTime.UtcNow.Ticks
            };
        }

        public void LoadState(WorldTimeState state, float maxOfflineHours = 24f)
        {
            long ticksElapsed = DateTime.UtcNow.Ticks - state.lastSaveTime;
            float secondsElapsed = ticksElapsed / (float)TimeSpan.TicksPerSecond;
            float maxOfflineSeconds = maxOfflineHours * 3600f;
            secondsElapsed = Mathf.Min(secondsElapsed, maxOfflineSeconds);

            currentDay = state.currentDay;
            currentYear = state.currentYear;
            timeOfDayNormalized = state.timeOfDayNormalized;

            UpdateSeason();
            CalculateSecondsPerDay();

            AdvanceTime(secondsElapsed);
        }

        public void SetTime(float normalizedTime, int day = -1, Season? season = null)
        {
            timeOfDayNormalized = Mathf.Clamp01(normalizedTime);
            if (day >= 0) currentDay = day;
            if (season.HasValue)
            {
                currentDay = (int)season.Value * daysPerSeason + (currentDay % daysPerSeason);
            }
            UpdateSeason();
            UpdateTimeOfDayPhase();
        }
        #endregion
    }

    [System.Serializable]
    public struct WorldTimeState
    {
        public float timeOfDayNormalized;
        public int currentDay;
        public int currentYear;
        public long lastSaveTime;
    }
}
