using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Performance profiling system with custom markers for key systems.
    /// Helps identify bottlenecks during development.
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        public static PerformanceProfiler Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool logToConsole = false;
        [SerializeField] private float logInterval = 5f;

        [Header("Thresholds (ms)")]
        [SerializeField] private float warningThreshold = 16.67f; // 60 FPS
        [SerializeField] private float criticalThreshold = 33.33f; // 30 FPS

        // Performance data
        private Dictionary<string, ProfileData> profileData;
        private float lastLogTime;

        // Custom samplers for Unity Profiler
        private Dictionary<string, CustomSampler> customSamplers;

        // Frame timing
        private float[] frameTimings;
        private int frameTimingIndex;
        private const int FRAME_TIMING_SAMPLES = 120;

        // Events
        public event Action<string, float> OnThresholdExceeded;
        public event Action<PerformanceReport> OnReportGenerated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            profileData = new Dictionary<string, ProfileData>();
            customSamplers = new Dictionary<string, CustomSampler>();
            frameTimings = new float[FRAME_TIMING_SAMPLES];

            InitializeProfileMarkers();
        }

        private void InitializeProfileMarkers()
        {
            // Create custom samplers for key systems
            CreateSampler("Gravity.Update");
            CreateSampler("Ecosystem.Update");
            CreateSampler("Weather.Update");
            CreateSampler("WorldTime.Update");
            CreateSampler("Events.Update");
            CreateSampler("Creatures.AI");
            CreateSampler("Creatures.Spawn");
            CreateSampler("Resources.Update");
            CreateSampler("FloatBand.Calculate");
            CreateSampler("Camera.Update");
            CreateSampler("UI.Update");
            CreateSampler("Save.Write");
            CreateSampler("Save.Read");
            CreateSampler("Audio.Update");
        }

        private void CreateSampler(string name)
        {
            customSamplers[name] = CustomSampler.Create(name);
            profileData[name] = new ProfileData { Name = name };
        }

        private void Update()
        {
            if (!enableProfiling) return;

            // Track frame timing
            frameTimings[frameTimingIndex] = Time.unscaledDeltaTime * 1000f;
            frameTimingIndex = (frameTimingIndex + 1) % FRAME_TIMING_SAMPLES;

            // Periodic logging
            if (logToConsole && Time.time - lastLogTime > logInterval)
            {
                LogPerformanceReport();
                lastLogTime = Time.time;
            }
        }

        #region Public API

        public ProfileScope BeginSample(string markerName)
        {
            if (!enableProfiling) return new ProfileScope(null, null, null);

            if (customSamplers.TryGetValue(markerName, out CustomSampler sampler))
            {
                sampler.Begin();
                return new ProfileScope(sampler, profileData[markerName], this);
            }

            return new ProfileScope(null, null, null);
        }

        public void EndSample(string markerName, float durationMs)
        {
            if (!enableProfiling) return;

            if (profileData.TryGetValue(markerName, out ProfileData data))
            {
                data.RecordSample(durationMs);

                if (durationMs > criticalThreshold)
                {
                    OnThresholdExceeded?.Invoke(markerName, durationMs);
                    if (logToConsole)
                    {
                        Debug.LogWarning($"[Perf] CRITICAL: {markerName} took {durationMs:F2}ms");
                    }
                }
                else if (durationMs > warningThreshold)
                {
                    if (logToConsole)
                    {
                        Debug.Log($"[Perf] Warning: {markerName} took {durationMs:F2}ms");
                    }
                }
            }
        }

        public PerformanceReport GenerateReport()
        {
            PerformanceReport report = new PerformanceReport();

            // Frame timing stats
            float totalFrameTime = 0f;
            float maxFrameTime = 0f;
            float minFrameTime = float.MaxValue;

            for (int i = 0; i < FRAME_TIMING_SAMPLES; i++)
            {
                float time = frameTimings[i];
                if (time > 0)
                {
                    totalFrameTime += time;
                    maxFrameTime = Mathf.Max(maxFrameTime, time);
                    minFrameTime = Mathf.Min(minFrameTime, time);
                }
            }

            report.AverageFrameTime = totalFrameTime / FRAME_TIMING_SAMPLES;
            report.MaxFrameTime = maxFrameTime;
            report.MinFrameTime = minFrameTime > 0 ? minFrameTime : 0;
            report.AverageFPS = 1000f / report.AverageFrameTime;

            // System profiling data
            report.SystemStats = new List<SystemPerformanceStat>();
            foreach (var kvp in profileData)
            {
                ProfileData data = kvp.Value;
                report.SystemStats.Add(new SystemPerformanceStat
                {
                    SystemName = data.Name,
                    AverageMs = data.AverageMs,
                    MaxMs = data.MaxMs,
                    SampleCount = data.SampleCount
                });
            }

            // Memory stats
            report.TotalMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            report.UsedHeapMB = Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);

            OnReportGenerated?.Invoke(report);
            return report;
        }

        public void ResetStats()
        {
            foreach (var data in profileData.Values)
            {
                data.Reset();
            }
            frameTimingIndex = 0;
            Array.Clear(frameTimings, 0, FRAME_TIMING_SAMPLES);
        }

        #endregion

        private void LogPerformanceReport()
        {
            PerformanceReport report = GenerateReport();

            Debug.Log($"[Perf Report] FPS: {report.AverageFPS:F1} (Frame: {report.AverageFrameTime:F2}ms, Max: {report.MaxFrameTime:F2}ms)");
            Debug.Log($"[Perf Report] Memory: {report.TotalMemoryMB:F1}MB total, {report.UsedHeapMB:F1}MB heap");

            // Log slowest systems
            report.SystemStats.Sort((a, b) => b.AverageMs.CompareTo(a.AverageMs));
            for (int i = 0; i < Mathf.Min(5, report.SystemStats.Count); i++)
            {
                var stat = report.SystemStats[i];
                if (stat.SampleCount > 0)
                {
                    Debug.Log($"[Perf Report]   {stat.SystemName}: {stat.AverageMs:F2}ms avg ({stat.SampleCount} samples)");
                }
            }
        }
    }

    #region Data Structures

    public struct ProfileScope : IDisposable
    {
        private CustomSampler sampler;
        private ProfileData data;
        private PerformanceProfiler profiler;
        private Stopwatch stopwatch;

        public ProfileScope(CustomSampler sampler, ProfileData data, PerformanceProfiler profiler)
        {
            this.sampler = sampler;
            this.data = data;
            this.profiler = profiler;
            this.stopwatch = sampler != null ? Stopwatch.StartNew() : null;
        }

        public void Dispose()
        {
            if (sampler != null && stopwatch != null)
            {
                stopwatch.Stop();
                sampler.End();
                profiler?.EndSample(data.Name, (float)stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }

    public class ProfileData
    {
        public string Name;
        public float TotalMs;
        public float MaxMs;
        public int SampleCount;

        public float AverageMs => SampleCount > 0 ? TotalMs / SampleCount : 0;

        public void RecordSample(float durationMs)
        {
            TotalMs += durationMs;
            MaxMs = Mathf.Max(MaxMs, durationMs);
            SampleCount++;
        }

        public void Reset()
        {
            TotalMs = 0;
            MaxMs = 0;
            SampleCount = 0;
        }
    }

    [Serializable]
    public class PerformanceReport
    {
        public float AverageFrameTime;
        public float MaxFrameTime;
        public float MinFrameTime;
        public float AverageFPS;
        public float TotalMemoryMB;
        public float UsedHeapMB;
        public List<SystemPerformanceStat> SystemStats;
    }

    [Serializable]
    public class SystemPerformanceStat
    {
        public string SystemName;
        public float AverageMs;
        public float MaxMs;
        public int SampleCount;
    }

    #endregion
}
