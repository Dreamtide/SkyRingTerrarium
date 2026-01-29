using UnityEngine;
using System;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Defines a zero-gravity zone (Float Band) where objects can float freely.
    /// Objects entering this zone gradually lose gravity influence and can enter gravity selection mode.
    /// </summary>
    public class FloatBand : MonoBehaviour
    {
        public static event Action<GravityAffectedBody> OnBodyEnteredFloatBand;
        public static event Action<GravityAffectedBody> OnBodyExitedFloatBand;
        public static event Action<GravityAffectedBody> OnBodyFullyInFloatBand;

        [Header("Float Band Configuration")]
        [SerializeField] private float bandHeight = 10f;
        [SerializeField] private float bandAltitude = 50f;
        [SerializeField] private float transitionZone = 5f;

        [Header("Float Behavior")]
        [SerializeField] private float dampingStrength = 2f;
        [SerializeField] private float floatOscillationAmplitude = 0.5f;
        [SerializeField] private float floatOscillationFrequency = 1f;
        [SerializeField] private float velocityDamping = 0.98f;

        [Header("Gravity Selection")]
        [SerializeField] private bool autoEnterGravitySelection = false;
        [SerializeField] private float selectionModeDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color bandColor = new Color(0.5f, 0.8f, 1f, 0.3f);
        [SerializeField] private Color transitionColor = new Color(0.3f, 0.6f, 0.9f, 0.2f);

        public float BandHeight => bandHeight;
        public float BandAltitude => bandAltitude;
        public float TransitionZone => transitionZone;

        private float[] bodyTimeInBand;
        private GravityAffectedBody[] trackedBodies;
        private int trackedBodyCount;
        private const int MAX_TRACKED_BODIES = 32;

        // Global stability multiplier for external systems to affect float band behavior
        private static float globalStabilityMultiplier = 1f;

        /// <summary>
        /// Set a global stability multiplier that affects all float band behavior.
        /// Used by systems like weather to create more chaotic floating during storms.
        /// </summary>
        /// <param name="multiplier">Multiplier value (1.0 = normal, <1.0 = more stable, >1.0 = more chaotic)</param>
        public static void SetGlobalStabilityMultiplier(float multiplier)
        {
            globalStabilityMultiplier = Mathf.Max(0.1f, multiplier);
        }

        /// <summary>
        /// Get the current global stability multiplier
        /// </summary>
        public static float GlobalStabilityMultiplier => globalStabilityMultiplier;

        private void Awake()
        {
            trackedBodies = new GravityAffectedBody[MAX_TRACKED_BODIES];
            bodyTimeInBand = new float[MAX_TRACKED_BODIES];
            trackedBodyCount = 0;
        }

        private void FixedUpdate()
        {
            UpdateTrackedBodies();
        }

        private void UpdateTrackedBodies()
        {
            for (int i = trackedBodyCount - 1; i >= 0; i--)
            {
                GravityAffectedBody body = trackedBodies[i];
                if (body == null)
                {
                    RemoveTrackedBodyAt(i);
                    continue;
                }

                float floatFactor = CalculateFloatFactor(body.transform.position);
                
                if (floatFactor <= 0f)
                {
                    OnBodyExitedFloatBand?.Invoke(body);
                    RemoveTrackedBodyAt(i);
                    continue;
                }

                bodyTimeInBand[i] += Time.fixedDeltaTime;
                
                ApplyFloatEffect(body, floatFactor, bodyTimeInBand[i]);

                if (floatFactor >= 1f && bodyTimeInBand[i] >= selectionModeDelay)
                {
                    OnBodyFullyInFloatBand?.Invoke(body);
                    if (autoEnterGravitySelection)
                    {
                        // Could trigger gravity selection mode here
                    }
                }
            }
        }

        public float CalculateFloatFactor(Vector3 position)
        {
            if (GravitySystem.Instance == null) return 0f;

            float distanceFromSurface = GravitySystem.Instance.GetDistanceFromRingSurface(position);
            float lowerBound = bandAltitude - bandHeight * 0.5f;
            float upperBound = bandAltitude + bandHeight * 0.5f;

            if (distanceFromSurface < lowerBound - transitionZone || 
                distanceFromSurface > upperBound + transitionZone)
            {
                return 0f;
            }

            if (distanceFromSurface >= lowerBound && distanceFromSurface <= upperBound)
            {
                return 1f;
            }

            if (distanceFromSurface < lowerBound)
            {
                return 1f - (lowerBound - distanceFromSurface) / transitionZone;
            }
            else
            {
                return 1f - (distanceFromSurface - upperBound) / transitionZone;
            }
        }

        private void ApplyFloatEffect(GravityAffectedBody body, float floatFactor, float timeInBand)
        {
            Rigidbody rb = body.GetComponent<Rigidbody>();
            if (rb == null) return;

            // Apply stability multiplier to oscillation
            float adjustedAmplitude = floatOscillationAmplitude * globalStabilityMultiplier;
            
            // Gentle oscillation
            float oscillation = Mathf.Sin(timeInBand * floatOscillationFrequency * Mathf.PI * 2f) * adjustedAmplitude;
            Vector3 upDir = GravitySystem.Instance != null ? 
                GravitySystem.Instance.GetUpDirection(body.transform.position) : Vector3.up;
            
            rb.AddForce(upDir * oscillation * floatFactor, ForceMode.Acceleration);

            // Velocity damping (more damping with higher stability)
            float effectiveDamping = Mathf.Lerp(1f, velocityDamping, floatFactor / globalStabilityMultiplier);
            rb.linearVelocity *= effectiveDamping;

            // Counter gravity
            if (body.IsGravityEnabled)
            {
                Vector3 counterGravity = -Physics.gravity * floatFactor * (dampingStrength / globalStabilityMultiplier);
                rb.AddForce(counterGravity, ForceMode.Acceleration);
            }
        }

        public void OnBodyEnterZone(GravityAffectedBody body)
        {
            if (body == null || IsBodyTracked(body)) return;

            float floatFactor = CalculateFloatFactor(body.transform.position);
            if (floatFactor > 0f && trackedBodyCount < MAX_TRACKED_BODIES)
            {
                trackedBodies[trackedBodyCount] = body;
                bodyTimeInBand[trackedBodyCount] = 0f;
                trackedBodyCount++;
                OnBodyEnteredFloatBand?.Invoke(body);
            }
        }

        private bool IsBodyTracked(GravityAffectedBody body)
        {
            for (int i = 0; i < trackedBodyCount; i++)
            {
                if (trackedBodies[i] == body) return true;
            }
            return false;
        }

        private void RemoveTrackedBodyAt(int index)
        {
            if (index < 0 || index >= trackedBodyCount) return;

            trackedBodies[index] = trackedBodies[trackedBodyCount - 1];
            bodyTimeInBand[index] = bodyTimeInBand[trackedBodyCount - 1];
            trackedBodies[trackedBodyCount - 1] = null;
            trackedBodyCount--;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            float ringRadius = 100f;
            if (GravitySystem.Instance != null)
            {
                ringRadius = GravitySystem.Instance.RingRadius;
            }

            // Draw main band
            Gizmos.color = bandColor;
            DrawBandRing(ringRadius + bandAltitude, bandHeight);

            // Draw transition zones
            Gizmos.color = transitionColor;
            DrawBandRing(ringRadius + bandAltitude - bandHeight * 0.5f - transitionZone * 0.5f, transitionZone);
            DrawBandRing(ringRadius + bandAltitude + bandHeight * 0.5f + transitionZone * 0.5f, transitionZone);
        }

        private void DrawBandRing(float radius, float height)
        {
            int segments = 32;
            Vector3 center = GravitySystem.Instance != null ? GravitySystem.Instance.RingCenter : Vector3.zero;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * Mathf.PI * 2f;
                float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;

                Vector3 p1Low = center + new Vector3(Mathf.Cos(angle1) * (radius - height * 0.5f), 0f, Mathf.Sin(angle1) * (radius - height * 0.5f));
                Vector3 p2Low = center + new Vector3(Mathf.Cos(angle2) * (radius - height * 0.5f), 0f, Mathf.Sin(angle2) * (radius - height * 0.5f));
                Vector3 p1High = center + new Vector3(Mathf.Cos(angle1) * (radius + height * 0.5f), 0f, Mathf.Sin(angle1) * (radius + height * 0.5f));
                Vector3 p2High = center + new Vector3(Mathf.Cos(angle2) * (radius + height * 0.5f), 0f, Mathf.Sin(angle2) * (radius + height * 0.5f));

                Gizmos.DrawLine(p1Low, p2Low);
                Gizmos.DrawLine(p1High, p2High);
            }
        }
    }
}