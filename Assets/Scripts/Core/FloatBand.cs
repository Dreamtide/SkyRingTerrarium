using UnityEngine;
using System;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Defines a zero-gravity zone (Float Band) where objects can float freely.
    /// Objects entering this zone gradually lose gravity influence.
    /// </summary>
    public class FloatBand : MonoBehaviour
    {
        public static event Action<GravityAffectedBody> OnBodyEnteredFloatBand;
        public static event Action<GravityAffectedBody> OnBodyExitedFloatBand;

        [Header("Float Band Configuration")]
        [SerializeField] private float bandHeight = 10f;
        [SerializeField] private float bandAltitude = 50f;
        [SerializeField] private float transitionZone = 5f;

        [Header("Float Behavior")]
        [SerializeField] private float dampingStrength = 2f;
        [SerializeField] private float floatOscillationAmplitude = 0.5f;
        [SerializeField] private float floatOscillationFrequency = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color bandColor = new Color(0.5f, 0.8f, 1f, 0.3f);

        public float BandHeight => bandHeight;
        public float BandAltitude => bandAltitude;

        private void FixedUpdate()
        {
            if (GravitySystem.Instance == null) return;

            var bodies = FindObjectsOfType<GravityAffectedBody>();
            foreach (var body in bodies)
            {
                ProcessFloatBandEffect(body);
            }
        }

        private void ProcessFloatBandEffect(GravityAffectedBody body)
        {
            float altitude = GetAltitudeAboveRing(body.transform.position);
            float floatFactor = CalculateFloatFactor(altitude);

            if (floatFactor > 0)
            {
                float originalScale = body.GravityScale;
                body.GravityScale = Mathf.Lerp(originalScale, 0f, floatFactor);

                if (floatFactor > 0.5f)
                {
                    ApplyFloatDamping(body);
                    ApplyFloatOscillation(body, floatFactor);
                }

                if (floatFactor >= 1f && !IsInFloatBand(body))
                {
                    SetInFloatBand(body, true);
                    OnBodyEnteredFloatBand?.Invoke(body);
                }
            }
            else
            {
                if (IsInFloatBand(body))
                {
                    SetInFloatBand(body, false);
                    OnBodyExitedFloatBand?.Invoke(body);
                }
                body.GravityScale = 1f;
            }
        }

        private float GetAltitudeAboveRing(Vector3 position)
        {
            if (GravitySystem.Instance == null) return 0f;
            return GravitySystem.Instance.GetDistanceFromRingSurface(position);
        }

        private float CalculateFloatFactor(float altitude)
        {
            float lowerBound = bandAltitude - transitionZone;
            float upperBound = bandAltitude + bandHeight + transitionZone;
            float bandStart = bandAltitude;
            float bandEnd = bandAltitude + bandHeight;

            if (altitude < lowerBound || altitude > upperBound)
                return 0f;

            if (altitude >= bandStart && altitude <= bandEnd)
                return 1f;

            if (altitude < bandStart)
                return Mathf.InverseLerp(lowerBound, bandStart, altitude);
            else
                return Mathf.InverseLerp(upperBound, bandEnd, altitude);
        }

        private void ApplyFloatDamping(GravityAffectedBody body)
        {
            Vector3 velocity = body.Rigidbody.velocity;
            Vector3 dampingForce = -velocity * dampingStrength;
            body.Rigidbody.AddForce(dampingForce, ForceMode.Acceleration);
        }

        private void ApplyFloatOscillation(GravityAffectedBody body, float floatFactor)
        {
            float oscillation = Mathf.Sin(Time.time * floatOscillationFrequency + body.GetInstanceID()) 
                              * floatOscillationAmplitude * floatFactor;
            
            Vector3 gravityDir = GravitySystem.Instance.CalculateGravityDirection(body.transform.position);
            body.Rigidbody.AddForce(-gravityDir * oscillation, ForceMode.Acceleration);
        }

        private bool IsInFloatBand(GravityAffectedBody body)
        {
            return body.gameObject.GetComponent<FloatBandMarker>() != null;
        }

        private void SetInFloatBand(GravityAffectedBody body, bool inBand)
        {
            var marker = body.gameObject.GetComponent<FloatBandMarker>();
            if (inBand && marker == null)
                body.gameObject.AddComponent<FloatBandMarker>();
            else if (!inBand && marker != null)
                Destroy(marker);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Gizmos.color = bandColor;
            
            Vector3 center = GravitySystem.Instance?.RingCenter ?? Vector3.zero;
            float radius = GravitySystem.Instance?.RingRadius ?? 100f;

            DrawBandVisualization(center, radius);
        }

        private void DrawBandVisualization(Vector3 center, float radius)
        {
            int segments = 32;
            float innerRadius = radius + bandAltitude;
            float outerRadius = radius + bandAltitude + bandHeight;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * 360f;
                float angle2 = ((i + 1) / (float)segments) * 360f;

                Vector3 inner1 = center + Quaternion.Euler(0, angle1, 0) * Vector3.right * innerRadius;
                Vector3 inner2 = center + Quaternion.Euler(0, angle2, 0) * Vector3.right * innerRadius;
                Vector3 outer1 = center + Quaternion.Euler(0, angle1, 0) * Vector3.right * outerRadius;
                Vector3 outer2 = center + Quaternion.Euler(0, angle2, 0) * Vector3.right * outerRadius;

                Gizmos.DrawLine(inner1, inner2);
                Gizmos.DrawLine(outer1, outer2);
                Gizmos.DrawLine(inner1, outer1);
            }
        }
    }

    /// <summary>
    /// Marker component to track objects currently in the float band.
    /// </summary>
    public class FloatBandMarker : MonoBehaviour { }
}
