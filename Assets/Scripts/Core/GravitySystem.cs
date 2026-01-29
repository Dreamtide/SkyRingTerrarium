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
        /// Get the distance from a position to the ring surface
        /// </summary>
        public float GetDistanceFromRingSurface(Vector3 position)
        {
            Vector3 toCenter = position - ringCenter;
            
            // Project onto the ring plane (perpendicular to ring axis)
            Vector3 onPlane = toCenter - Vector3.Project(toCenter, ringAxis);
            float distanceFromAxis = onPlane.magnitude;
            
            // Distance from ring surface (positive = outside, negative = inside)
            return distanceFromAxis - ringRadius;
        }

        /// <summary>
        /// Calculate the gravity direction for a given position
        /// </summary>
        public Vector3 CalculateGravityDirection(Vector3 position)
        {
            Vector3 toCenter = position - ringCenter;
            
            // Project onto the ring plane
            Vector3 onPlane = toCenter - Vector3.Project(toCenter, ringAxis);
            
            if (onPlane.magnitude < 0.001f)
            {
                // Object is on the axis, use arbitrary direction
                return -ringAxis;
            }
            
            // For ring structure, gravity points toward the ring surface
            // (radially inward toward the ring torus)
            float distanceFromAxis = onPlane.magnitude;
            
            if (distanceFromAxis > ringRadius)
            {
                // Outside ring - gravity points toward ring center
                return -onPlane.normalized;
            }
            else
            {
                // Inside ring - gravity points outward to ring surface
                return onPlane.normalized;
            }
        }

        /// <summary>
        /// Calculate the full gravity vector (direction and magnitude) for a given position
        /// </summary>
        public Vector3 CalculateGravity(Vector3 position)
        {
            float distanceFromSurface = Mathf.Abs(GetDistanceFromRingSurface(position));
            
            if (distanceFromSurface > maxGravityDistance)
            {
                return Vector3.zero;
            }

            float normalizedDistance = distanceFromSurface / maxGravityDistance;
            float gravityMultiplier = gravityFalloff.Evaluate(normalizedDistance);
            
            Vector3 direction = CalculateGravityDirection(position);
            return direction * gravityStrength * gravityMultiplier;
        }

        /// <summary>
        /// Get the "up" direction for a given position (opposite of gravity)
        /// </summary>
        public Vector3 GetUpDirection(Vector3 position)
        {
            return -CalculateGravityDirection(position);
        }

        /// <summary>
        /// Find the nearest point on the ring surface
        /// </summary>
        public Vector3 GetNearestPointOnRing(Vector3 position)
        {
            Vector3 toCenter = position - ringCenter;
            Vector3 onPlane = toCenter - Vector3.Project(toCenter, ringAxis);
            
            if (onPlane.magnitude < 0.001f)
            {
                onPlane = Vector3.right;
            }
            
            return ringCenter + onPlane.normalized * ringRadius;
        }

        /// <summary>
        /// Get the angular position around the ring (0-360 degrees)
        /// </summary>
        public float GetAngularPosition(Vector3 position)
        {
            Vector3 toCenter = position - ringCenter;
            Vector3 onPlane = toCenter - Vector3.Project(toCenter, ringAxis);
            
            float angle = Mathf.Atan2(onPlane.z, onPlane.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            
            return angle;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            // Draw ring
            Gizmos.color = Color.green;
            DrawRingGizmo(ringRadius, 64);

            // Draw gravity influence boundary
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
            DrawRingGizmo(ringRadius + maxGravityDistance, 32);
            DrawRingGizmo(Mathf.Max(0, ringRadius - maxGravityDistance), 32);
        }

        private void DrawRingGizmo(float radius, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * Mathf.PI * 2f;
                float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;

                Vector3 p1 = ringCenter + new Vector3(Mathf.Cos(angle1) * radius, 0f, Mathf.Sin(angle1) * radius);
                Vector3 p2 = ringCenter + new Vector3(Mathf.Cos(angle2) * radius, 0f, Mathf.Sin(angle2) * radius);

                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}