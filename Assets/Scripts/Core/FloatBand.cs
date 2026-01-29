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

        private void Awake()
        {
            bodyTimeInBand = new float[MAX_TRACKED_BODIES];
            trackedBodies = new GravityAffectedBody[MAX_TRACKED_BODIES];
        }

        private void FixedUpdate()
        {
            if (GravitySystem.Instance == null) return;

            var bodies = FindObjectsByType<GravityAffectedBody>(FindObjectsSortMode.None);
            foreach (var body in bodies)
            {
                ProcessFloatBandEffect(body);
            }
        }

        private void ProcessFloatBandEffect(GravityAffectedBody body)
        {
            float altitude = GetAltitudeAboveRing(body.transform.position);
            float floatFactor = CalculateFloatFactor(altitude);

            bool wasInBand = body.IsInFloatBand;
            bool isFullyInBand = floatFactor >= 1f;
            bool isInTransition = floatFactor > 0f && floatFactor < 1f;
            bool isInBandZone = floatFactor > 0f;

            body.SetFloatBandBlend(floatFactor);

            if (isInBandZone && !wasInBand)
            {
                body.IsInFloatBand = true;
                OnBodyEnteredFloatBand?.Invoke(body);
                TrackBody(body);
            }
            else if (!isInBandZone && wasInBand)
            {
                body.IsInFloatBand = false;
                OnBodyExitedFloatBand?.Invoke(body);
                UntrackBody(body);
            }

            if (isFullyInBand)
            {
                ApplyFloatBandPhysics(body, floatFactor);
                
                int index = GetTrackedIndex(body);
                if (index >= 0)
                {
                    float prevTime = bodyTimeInBand[index];
                    bodyTimeInBand[index] += Time.fixedDeltaTime;
                    
                    if (prevTime < selectionModeDelay && bodyTimeInBand[index] >= selectionModeDelay)
                    {
                        OnBodyFullyInFloatBand?.Invoke(body);
                        
                        if (autoEnterGravitySelection && OmnidirectionalGravity.Instance != null)
                        {
                            OmnidirectionalGravity.Instance.TryEnterSelectionMode();
                        }
                    }
                }
            }
            else if (isInTransition)
            {
                ApplyTransitionPhysics(body, floatFactor);
            }
        }

        private void TrackBody(GravityAffectedBody body)
        {
            for (int i = 0; i < MAX_TRACKED_BODIES; i++)
            {
                if (trackedBodies[i] == null)
                {
                    trackedBodies[i] = body;
                    bodyTimeInBand[i] = 0f;
                    trackedBodyCount++;
                    return;
                }
            }
        }

        private void UntrackBody(GravityAffectedBody body)
        {
            for (int i = 0; i < MAX_TRACKED_BODIES; i++)
            {
                if (trackedBodies[i] == body)
                {
                    trackedBodies[i] = null;
                    bodyTimeInBand[i] = 0f;
                    trackedBodyCount--;
                    return;
                }
            }
        }

        private int GetTrackedIndex(GravityAffectedBody body)
        {
            for (int i = 0; i < MAX_TRACKED_BODIES; i++)
            {
                if (trackedBodies[i] == body)
                {
                    return i;
                }
            }
            return -1;
        }

        private void ApplyFloatBandPhysics(GravityAffectedBody body, float floatFactor)
        {
            Rigidbody rb = body.Rigidbody;
            if (rb == null) return;

            rb.linearVelocity *= velocityDamping;

            float oscillation = Mathf.Sin(Time.time * floatOscillationFrequency * Mathf.PI * 2f) 
                              * floatOscillationAmplitude;
            
            Vector3 up = -body.CurrentGravityDirection;
            rb.AddForce(up * oscillation, ForceMode.Acceleration);

            if (rb.linearVelocity.magnitude > 0.1f)
            {
                rb.AddForce(-rb.linearVelocity * dampingStrength, ForceMode.Acceleration);
            }
        }

        private void ApplyTransitionPhysics(GravityAffectedBody body, float floatFactor)
        {
            Rigidbody rb = body.Rigidbody;
            if (rb == null) return;

            float transitionDamping = Mathf.Lerp(1f, velocityDamping, floatFactor);
            rb.linearVelocity *= transitionDamping;
        }

        private float GetAltitudeAboveRing(Vector3 position)
        {
            if (GravitySystem.Instance == null) return 0f;
            return GravitySystem.Instance.GetAltitude(position);
        }

        private float CalculateFloatFactor(float altitude)
        {
            float lowerBound = bandAltitude - transitionZone;
            float upperBound = bandAltitude + bandHeight + transitionZone;
            float coreUpper = bandAltitude + bandHeight;

            if (altitude < lowerBound || altitude > upperBound)
            {
                return 0f;
            }

            if (altitude >= bandAltitude && altitude <= coreUpper)
            {
                return 1f;
            }

            if (altitude < bandAltitude)
            {
                return Mathf.InverseLerp(lowerBound, bandAltitude, altitude);
            }
            else
            {
                return Mathf.InverseLerp(upperBound, coreUpper, altitude);
            }
        }

        /// <summary>
        /// Check if a position is within the float band.
        /// </summary>
        public bool IsInFloatBand(Vector3 position)
        {
            float altitude = GetAltitudeAboveRing(position);
            return CalculateFloatFactor(altitude) > 0f;
        }

        /// <summary>
        /// Get the float factor at a specific position.
        /// </summary>
        public float GetFloatFactorAt(Vector3 position)
        {
            float altitude = GetAltitudeAboveRing(position);
            return CalculateFloatFactor(altitude);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            if (GravitySystem.Instance == null && !Application.isPlaying) return;

            float radius = 100f;
            Vector3 center = Vector3.zero;
            Vector3 axis = Vector3.up;

            if (GravitySystem.Instance != null)
            {
                radius = GravitySystem.Instance.RingRadius;
                center = GravitySystem.Instance.RingCenter;
                axis = GravitySystem.Instance.RingAxis;
            }

            float innerRadius = radius + bandAltitude;
            float outerRadius = radius + bandAltitude + bandHeight;
            float transitionInner = radius + bandAltitude - transitionZone;
            float transitionOuter = radius + bandAltitude + bandHeight + transitionZone;

            Gizmos.color = bandColor;
            DrawRingGizmo(center, axis, innerRadius, 48);
            DrawRingGizmo(center, axis, outerRadius, 48);

            Gizmos.color = transitionColor;
            DrawRingGizmo(center, axis, transitionInner, 32);
            DrawRingGizmo(center, axis, transitionOuter, 32);
        }

        private void DrawRingGizmo(Vector3 center, Vector3 axis, float radius, int segments)
        {
            Vector3 referenceRight = Vector3.Cross(axis, Vector3.forward);
            if (referenceRight.sqrMagnitude < 0.001f)
            {
                referenceRight = Vector3.Cross(axis, Vector3.right);
            }
            referenceRight.Normalize();

            Vector3 lastPoint = center + referenceRight * radius;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * 360f;
                Vector3 point = center + Quaternion.AngleAxis(angle, axis) * (referenceRight * radius);
                Gizmos.DrawLine(lastPoint, point);
                lastPoint = point;
            }
        }
#endif
    }
}