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

            Physics.gravity = Vector3.zero;
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
        /// Get the natural gravity direction at a given world position.
        /// Points toward the nearest point on the ring.
        /// </summary>
        public Vector3 GetGravityDirection(Vector3 worldPosition)
        {
            Vector3 nearestPointOnRing = GetNearestPointOnRing(worldPosition);
            Vector3 direction = (nearestPointOnRing - worldPosition).normalized;
            return direction;
        }

        /// <summary>
        /// Get the gravity strength at a given world position.
        /// </summary>
        public float GetGravityStrengthAt(Vector3 worldPosition)
        {
            float distance = GetDistanceFromRing(worldPosition);
            float normalizedDistance = Mathf.Clamp01(distance / maxGravityDistance);
            float falloffMultiplier = gravityFalloff.Evaluate(normalizedDistance);
            return gravityStrength * falloffMultiplier;
        }

        /// <summary>
        /// Get the nearest point on the ring surface from a world position.
        /// </summary>
        public Vector3 GetNearestPointOnRing(Vector3 worldPosition)
        {
            Vector3 toPosition = worldPosition - ringCenter;
            Vector3 projectedOnPlane = Vector3.ProjectOnPlane(toPosition, ringAxis);
            
            if (projectedOnPlane.sqrMagnitude < 0.001f)
            {
                projectedOnPlane = Vector3.right;
            }
            
            projectedOnPlane = projectedOnPlane.normalized * ringRadius;
            return ringCenter + projectedOnPlane;
        }

        /// <summary>
        /// Get the distance from a point to the ring surface.
        /// </summary>
        public float GetDistanceFromRing(Vector3 worldPosition)
        {
            Vector3 nearestPoint = GetNearestPointOnRing(worldPosition);
            return Vector3.Distance(worldPosition, nearestPoint);
        }

        /// <summary>
        /// Get the altitude above the ring surface (positive = outward from center).
        /// </summary>
        public float GetAltitude(Vector3 worldPosition)
        {
            Vector3 toPosition = worldPosition - ringCenter;
            Vector3 projectedOnPlane = Vector3.ProjectOnPlane(toPosition, ringAxis);
            
            float horizontalDistance = projectedOnPlane.magnitude;
            float verticalDistance = Vector3.Dot(toPosition, ringAxis);
            
            float surfaceRadius = ringRadius;
            float altitudeFromCenter = horizontalDistance - surfaceRadius;
            
            return Mathf.Sqrt(altitudeFromCenter * altitudeFromCenter + verticalDistance * verticalDistance) 
                   * Mathf.Sign(altitudeFromCenter);
        }

        /// <summary>
        /// Get the angular position around the ring (0-360 degrees).
        /// </summary>
        public float GetAngularPosition(Vector3 worldPosition)
        {
            Vector3 toPosition = worldPosition - ringCenter;
            Vector3 projectedOnPlane = Vector3.ProjectOnPlane(toPosition, ringAxis);
            
            Vector3 referenceRight = Vector3.Cross(ringAxis, Vector3.forward);
            if (referenceRight.sqrMagnitude < 0.001f)
            {
                referenceRight = Vector3.Cross(ringAxis, Vector3.right);
            }
            referenceRight.Normalize();
            
            float angle = Vector3.SignedAngle(referenceRight, projectedOnPlane.normalized, ringAxis);
            return (angle + 360f) % 360f;
        }

        /// <summary>
        /// Get a world position on the ring at a specific angle.
        /// </summary>
        public Vector3 GetPositionAtAngle(float angle, float altitude = 0f)
        {
            Vector3 referenceRight = Vector3.Cross(ringAxis, Vector3.forward);
            if (referenceRight.sqrMagnitude < 0.001f)
            {
                referenceRight = Vector3.Cross(ringAxis, Vector3.right);
            }
            referenceRight.Normalize();
            
            Vector3 direction = Quaternion.AngleAxis(angle, ringAxis) * referenceRight;
            return ringCenter + direction * (ringRadius + altitude);
        }

        /// <summary>
        /// Check if a position is inside the ring (closer to center than ring surface).
        /// </summary>
        public bool IsInsideRing(Vector3 worldPosition)
        {
            Vector3 toPosition = worldPosition - ringCenter;
            Vector3 projectedOnPlane = Vector3.ProjectOnPlane(toPosition, ringAxis);
            return projectedOnPlane.magnitude < ringRadius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Gizmos.color = Color.cyan;
            DrawRingGizmo(ringRadius, 64);
            
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
            DrawRingGizmo(ringRadius + maxGravityDistance * 0.5f, 32);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(ringCenter, ringCenter + ringAxis * ringRadius * 0.5f);
        }

        private void DrawRingGizmo(float radius, int segments)
        {
            Vector3 referenceRight = Vector3.Cross(ringAxis, Vector3.forward);
            if (referenceRight.sqrMagnitude < 0.001f)
            {
                referenceRight = Vector3.Cross(ringAxis, Vector3.right);
            }
            referenceRight.Normalize();

            Vector3 lastPoint = ringCenter + referenceRight * radius;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * 360f;
                Vector3 point = ringCenter + Quaternion.AngleAxis(angle, ringAxis) * (referenceRight * radius);
                Gizmos.DrawLine(lastPoint, point);
                lastPoint = point;
            }
        }
#endif
    }
}