using UnityEngine;
using System;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Manages orbital trajectories around the ring structure.
    /// Objects can enter and maintain stable orbits at various altitudes.
    /// </summary>
    public class OrbitalLoop : MonoBehaviour
    {
        public static event Action<OrbitalBody> OnOrbitEstablished;
        public static event Action<OrbitalBody> OnOrbitDecayed;

        [Header("Orbital Configuration")]
        [SerializeField] private float minOrbitalAltitude = 20f;
        [SerializeField] private float maxOrbitalAltitude = 80f;
        [SerializeField] private float orbitalVelocityMultiplier = 1f;

        [Header("Stability Settings")]
        [SerializeField] private float orbitStabilizationStrength = 0.5f;
        [SerializeField] private float velocityMatchThreshold = 0.9f;
        [SerializeField] private float orbitDecayRate = 0.01f;

        [Header("Debug")]
        [SerializeField] private bool showOrbitalPaths = true;
        [SerializeField] private Color orbitColor = Color.yellow;

        public float MinOrbitalAltitude => minOrbitalAltitude;
        public float MaxOrbitalAltitude => maxOrbitalAltitude;

        private void FixedUpdate()
        {
            var orbitalBodies = FindObjectsOfType<OrbitalBody>();
            foreach (var body in orbitalBodies)
            {
                UpdateOrbitalMechanics(body);
            }
        }

        private void UpdateOrbitalMechanics(OrbitalBody body)
        {
            if (!body.IsInOrbit)
            {
                CheckForOrbitEntry(body);
            }
            else
            {
                MaintainOrbit(body);
                CheckForOrbitDecay(body);
            }
        }

        private void CheckForOrbitEntry(OrbitalBody body)
        {
            float altitude = GetOrbitalAltitude(body.transform.position);
            
            if (altitude < minOrbitalAltitude || altitude > maxOrbitalAltitude)
                return;

            float requiredVelocity = CalculateOrbitalVelocity(altitude);
            float currentTangentialVelocity = GetTangentialVelocity(body);

            if (Mathf.Abs(currentTangentialVelocity - requiredVelocity) / requiredVelocity < (1f - velocityMatchThreshold))
            {
                EstablishOrbit(body, altitude);
            }
        }

        private void EstablishOrbit(OrbitalBody body, float altitude)
        {
            body.IsInOrbit = true;
            body.OrbitalAltitude = altitude;
            body.OrbitalVelocity = CalculateOrbitalVelocity(altitude);
            
            var gravityBody = body.GetComponent<GravityAffectedBody>();
            if (gravityBody != null)
                gravityBody.GravityScale = 0f;

            OnOrbitEstablished?.Invoke(body);
        }

        private void MaintainOrbit(OrbitalBody body)
        {
            Vector3 position = body.transform.position;
            float currentAltitude = GetOrbitalAltitude(position);
            float targetAltitude = body.OrbitalAltitude;

            // Altitude correction
            float altitudeError = targetAltitude - currentAltitude;
            Vector3 radialDirection = GetRadialDirection(position);
            Vector3 correctionForce = radialDirection * altitudeError * orbitStabilizationStrength;

            // Velocity maintenance
            float targetVelocity = body.OrbitalVelocity * orbitalVelocityMultiplier;
            Vector3 tangentialDirection = GetTangentialDirection(position);
            float currentSpeed = Vector3.Dot(body.Rigidbody.velocity, tangentialDirection);
            float speedError = targetVelocity - currentSpeed;
            Vector3 velocityCorrection = tangentialDirection * speedError * orbitStabilizationStrength;

            body.Rigidbody.AddForce(correctionForce + velocityCorrection, ForceMode.Acceleration);
        }

        private void CheckForOrbitDecay(OrbitalBody body)
        {
            float currentAltitude = GetOrbitalAltitude(body.transform.position);
            float deviation = Mathf.Abs(currentAltitude - body.OrbitalAltitude) / body.OrbitalAltitude;

            body.OrbitStability -= orbitDecayRate * deviation * Time.fixedDeltaTime;

            if (body.OrbitStability <= 0f)
            {
                DecayOrbit(body);
            }
        }

        private void DecayOrbit(OrbitalBody body)
        {
            body.IsInOrbit = false;
            body.OrbitStability = 1f;

            var gravityBody = body.GetComponent<GravityAffectedBody>();
            if (gravityBody != null)
                gravityBody.GravityScale = 1f;

            OnOrbitDecayed?.Invoke(body);
        }

        private float GetOrbitalAltitude(Vector3 position)
        {
            return GravitySystem.Instance?.GetDistanceFromRingSurface(position) ?? 0f;
        }

        private float CalculateOrbitalVelocity(float altitude)
        {
            if (GravitySystem.Instance == null) return 0f;
            
            float radius = GravitySystem.Instance.RingRadius + altitude;
            float gravity = GravitySystem.Instance.GravityStrength;
            
            return Mathf.Sqrt(gravity * radius);
        }

        private float GetTangentialVelocity(OrbitalBody body)
        {
            Vector3 tangent = GetTangentialDirection(body.transform.position);
            return Vector3.Dot(body.Rigidbody.velocity, tangent);
        }

        private Vector3 GetRadialDirection(Vector3 position)
        {
            return GravitySystem.Instance?.CalculateGravityDirection(position) ?? Vector3.down;
        }

        private Vector3 GetTangentialDirection(Vector3 position)
        {
            Vector3 radial = GetRadialDirection(position);
            Vector3 axis = Vector3.up;
            return Vector3.Cross(radial, axis).normalized;
        }

        private void OnDrawGizmos()
        {
            if (!showOrbitalPaths) return;

            Gizmos.color = orbitColor;
            DrawOrbitalZone();
        }

        private void DrawOrbitalZone()
        {
            Vector3 center = GravitySystem.Instance?.RingCenter ?? Vector3.zero;
            float baseRadius = GravitySystem.Instance?.RingRadius ?? 100f;

            DrawOrbitRing(center, baseRadius + minOrbitalAltitude);
            DrawOrbitRing(center, baseRadius + maxOrbitalAltitude);
        }

        private void DrawOrbitRing(Vector3 center, float radius)
        {
            int segments = 64;
            for (int i = 0; i < segments; i++)
            {
                float a1 = (i / (float)segments) * Mathf.PI * 2;
                float a2 = ((i + 1) / (float)segments) * Mathf.PI * 2;

                Vector3 p1 = center + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;
                Vector3 p2 = center + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * radius;

                Gizmos.DrawLine(p1, p2);
            }
        }
    }

    /// <summary>
    /// Component for objects that can enter orbital trajectories.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class OrbitalBody : MonoBehaviour
    {
        public Rigidbody Rigidbody { get; private set; }
        public bool IsInOrbit { get; set; }
        public float OrbitalAltitude { get; set; }
        public float OrbitalVelocity { get; set; }
        public float OrbitStability { get; set; } = 1f;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
        }
    }
}
