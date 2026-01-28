using UnityEngine;
using System.Collections.Generic;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Manages custom gravity for the Sky Ring structure.
    /// Objects experience gravity directed radially inward toward the ring center.
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
        public float GravityStrength => gravityStrength;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (gravityFalloff == null || gravityFalloff.length == 0)
            {
                gravityFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
            }

            Physics.gravity = Vector3.zero;
        }

        public void RegisterBody(GravityAffectedBody body)
        {
            if (!affectedBodies.Contains(body))
                affectedBodies.Add(body);
        }

        public void UnregisterBody(GravityAffectedBody body)
        {
            affectedBodies.Remove(body);
        }

        private void FixedUpdate()
        {
            foreach (var body in affectedBodies)
            {
                if (body == null || !body.isActiveAndEnabled) continue;
                ApplyGravity(body);
            }
        }

        private void ApplyGravity(GravityAffectedBody body)
        {
            Vector3 gravityDir = CalculateGravityDirection(body.transform.position);
            float distance = GetDistanceFromRingSurface(body.transform.position);
            float normalizedDistance = Mathf.Clamp01(distance / maxGravityDistance);
            float gravityMultiplier = gravityFalloff.Evaluate(normalizedDistance);
            
            Vector3 gravityForce = gravityDir * gravityStrength * gravityMultiplier * body.GravityScale;
            body.Rigidbody.AddForce(gravityForce, ForceMode.Acceleration);
        }

        public Vector3 CalculateGravityDirection(Vector3 worldPosition)
        {
            Vector3 toCenter = ringCenter - worldPosition;
            Vector3 projectedOnAxis = Vector3.Project(toCenter, ringAxis);
            Vector3 radialDirection = (toCenter - projectedOnAxis).normalized;
            
            float distanceFromAxis = Vector3.Distance(worldPosition, ringCenter + projectedOnAxis);
            
            if (distanceFromAxis > ringRadius)
                return radialDirection;
            else if (distanceFromAxis < ringRadius)
                return -radialDirection;
            else
                return Vector3.zero;
        }

        public float GetDistanceFromRingSurface(Vector3 worldPosition)
        {
            Vector3 toCenter = ringCenter - worldPosition;
            Vector3 projectedOnAxis = Vector3.Project(toCenter, ringAxis);
            float distanceFromAxis = Vector3.Distance(worldPosition, ringCenter + projectedOnAxis);
            return Mathf.Abs(distanceFromAxis - ringRadius);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            Gizmos.color = Color.cyan;
            DrawRingGizmo();
        }

        private void DrawRingGizmo()
        {
            int segments = 64;
            Vector3 perpendicular = Vector3.Cross(ringAxis, Vector3.right).normalized;
            if (perpendicular == Vector3.zero)
                perpendicular = Vector3.Cross(ringAxis, Vector3.forward).normalized;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * 360f;
                float angle2 = ((i + 1) / (float)segments) * 360f;
                
                Vector3 p1 = ringCenter + Quaternion.AngleAxis(angle1, ringAxis) * perpendicular * ringRadius;
                Vector3 p2 = ringCenter + Quaternion.AngleAxis(angle2, ringAxis) * perpendicular * ringRadius;
                
                Gizmos.DrawLine(p1, p2);
            }
        }
    }

    /// <summary>
    /// Component for objects affected by the custom gravity system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GravityAffectedBody : MonoBehaviour
    {
        [SerializeField] private float gravityScale = 1f;
        
        public float GravityScale
        {
            get => gravityScale;
            set => gravityScale = value;
        }

        public Rigidbody Rigidbody { get; private set; }

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Rigidbody.useGravity = false;
        }

        private void OnEnable()
        {
            GravitySystem.Instance?.RegisterBody(this);
        }

        private void OnDisable()
        {
            GravitySystem.Instance?.UnregisterBody(this);
        }
    }
}
