using UnityEngine;
using System.Collections.Generic;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Manages custom gravity for the Sky Ring structure.
    /// Objects experience gravity directed radially inward toward the ring center.
    /// Updated to support omnidirectional gravity system.
    /// </summary>
    public class GravitySystem : MonoBehaviour
    {
        public static GravitySystem Instance { get; private set; }

        [Header("Ring Configuration")]
        [SerializeField] private float ringRadius = 100f;
        [SerializeField] private Vector3 ringCenter = Vector3.zero;
        [SerializeField] private Vector3 ringAxis = Vector3.up;

        [Header("Gravity Settings")]
        [SerializeField] private float gravityStrength = 9.81f;
        [SerializeField] private float maxGravityDistance = 200f;
        [SerializeField] private AnimationCurve gravityFalloff;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        private List<GravityAffectedBody> affectedBodies = new List<GravityAffectedBody>();

        public float RingRadius => ringRadius;
        public Vector3 RingCenter => ringCenter;
        public Vector3 RingAxis => ringAxis;
        public float GravityStrength => gravityStrength;
        public float MaxGravityDistance => maxGravityDistance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (gravityFalloff == null || gravityFalloff.keys.Length == 0)
            {
                gravityFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterBody(GravityAffectedBody body)
        {
            if (!affectedBodies.Contains(body))
            {
                affectedBodies.Add(body);
            }
        }

        public void UnregisterBody(GravityAffectedBody body)
        {
            affectedBodies.Remove(body);
        }

        /// <summary>
        /// Calculates the gravity vector for a given world position.
        /// Returns the direction and magnitude of gravity at that point.
        /// </summary>
        public Vector3 CalculateGravity(Vector3 worldPosition)
        {
            Vector3 direction = GetGravityDirection(worldPosition);
            float distance = GetDistanceFromRingSurface(worldPosition);
            float normalizedDistance = Mathf.Clamp01(distance / maxGravityDistance);
            float falloff = gravityFalloff.Evaluate(normalizedDistance);
            
            return direction * gravityStrength * falloff;
        }

        /// <summary>
        /// Gets the gravity direction at a given world position.
        /// For a ring structure, gravity points toward the nearest point on the ring surface.
        /// </summary>
        public Vector3 GetGravityDirection(Vector3 worldPosition)
        {
            Vector3 toCenter = ringCenter - worldPosition;
            Vector3 projectedOnAxis = Vector3.Project(toCenter, ringAxis);
            Vector3 radialFromAxis = toCenter - projectedOnAxis;
            
            if (radialFromAxis.sqrMagnitude < 0.001f)
            {
                return -ringAxis;
            }
            
            Vector3 nearestPointOnRing = ringCenter - radialFromAxis.normalized * ringRadius;
            Vector3 gravityDirection = (nearestPointOnRing - worldPosition).normalized;
            
            return gravityDirection;
        }

        /// <summary>
        /// Gets the distance from the ring surface.
        /// </summary>
        public float GetDistanceFromRingSurface(Vector3 worldPosition)
        {
            Vector3 toCenter = ringCenter - worldPosition;
            Vector3 projectedOnAxis = Vector3.Project(toCenter, ringAxis);
            Vector3 radialFromAxis = toCenter - projectedOnAxis;
            
            float distanceFromAxis = radialFromAxis.magnitude;
            float heightAboveRingPlane = projectedOnAxis.magnitude;
            
            float radialDistance = Mathf.Abs(distanceFromAxis - ringRadius);
            
            return Mathf.Sqrt(radialDistance * radialDistance + heightAboveRingPlane * heightAboveRingPlane);
        }

        /// <summary>
        /// Gets the altitude above the ring surface.
        /// </summary>
        public float GetAltitude(Vector3 worldPosition)
        {
            return GetDistanceFromRingSurface(worldPosition);
        }

        /// <summary>
        /// Checks if a position is within the gravity influence zone.
        /// </summary>
        public bool IsInGravityZone(Vector3 worldPosition)
        {
            return GetDistanceFromRingSurface(worldPosition) <= maxGravityDistance;
        }

        /// <summary>
        /// Gets the orbital velocity required for a stable orbit at the given altitude.
        /// </summary>
        public float GetOrbitalVelocity(float altitude)
        {
            float effectiveRadius = ringRadius + altitude;
            return Mathf.Sqrt(gravityStrength * effectiveRadius);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Gizmos.color = Color.yellow;
            DrawRingGizmo(ringRadius, 64);
            
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            DrawRingGizmo(ringRadius - 5f, 64);
            DrawRingGizmo(ringRadius + 5f, 64);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            DrawRingGizmo(ringRadius + maxGravityDistance, 64);
        }

        private void DrawRingGizmo(float radius, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * Mathf.PI * 2f;
                float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;

                Vector3 localRight = Vector3.Cross(ringAxis, Vector3.forward);
                if (localRight.sqrMagnitude < 0.001f)
                {
                    localRight = Vector3.Cross(ringAxis, Vector3.right);
                }
                localRight.Normalize();
                Vector3 localForward = Vector3.Cross(localRight, ringAxis);

                Vector3 p1 = ringCenter + (localRight * Mathf.Cos(angle1) + localForward * Mathf.Sin(angle1)) * radius;
                Vector3 p2 = ringCenter + (localRight * Mathf.Cos(angle2) + localForward * Mathf.Sin(angle2)) * radius;

                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}
