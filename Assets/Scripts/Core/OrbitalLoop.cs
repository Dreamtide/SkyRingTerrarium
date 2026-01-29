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
            var orbitalBodies = FindObjectsByType<OrbitalBody>(FindObjectsSortMode.None);
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
            if (GravitySystem.Instance == null) return;

            float altitude = GravitySystem.Instance.GetDistanceFromRingSurface(body.transform.position);
            
            if (altitude >= minOrbitalAltitude && altitude <= maxOrbitalAltitude)
            {
                Rigidbody rb = body.GetComponent<Rigidbody>();
                if (rb == null) return;

                Vector3 idealOrbitalVelocity = CalculateIdealOrbitalVelocity(body.transform.position, altitude);
                float velocityMatch = Vector3.Dot(rb.linearVelocity.normalized, idealOrbitalVelocity.normalized);

                if (velocityMatch >= velocityMatchThreshold)
                {
                    EstablishOrbit(body, altitude, rb.linearVelocity.magnitude);
                }
            }
        }

        private Vector3 CalculateIdealOrbitalVelocity(Vector3 position, float altitude)
        {
            if (GravitySystem.Instance == null) return Vector3.zero;
            
            Vector3 gravityDir = GravitySystem.Instance.CalculateGravityDirection(position);
            Vector3 tangent = Vector3.Cross(gravityDir, Vector3.forward).normalized;
            
            float orbitalSpeed = Mathf.Sqrt(GravitySystem.Instance.GravityStrength * altitude) * orbitalVelocityMultiplier;
            
            return tangent * orbitalSpeed;
        }

        private void EstablishOrbit(OrbitalBody body, float altitude, float velocity)
        {
            body.IsInOrbit = true;
            body.OrbitalAltitude = altitude;
            body.OrbitalVelocity = velocity;
            body.OrbitStability = 1f;

            OnOrbitEstablished?.Invoke(body);
            Debug.Log($"[OrbitalLoop] {body.name} entered stable orbit at altitude {altitude:F1}");
        }

        private void MaintainOrbit(OrbitalBody body)
        {
            Rigidbody rb = body.GetComponent<Rigidbody>();
            if (rb == null || GravitySystem.Instance == null) return;

            float currentAltitude = GravitySystem.Instance.GetDistanceFromRingSurface(body.transform.position);
            float altitudeError = body.OrbitalAltitude - currentAltitude;

            Vector3 gravityDir = GravitySystem.Instance.CalculateGravityDirection(body.transform.position);
            Vector3 correctionForce = -gravityDir * altitudeError * orbitStabilizationStrength;
            rb.AddForce(correctionForce, ForceMode.Acceleration);

            Vector3 idealVelocity = CalculateIdealOrbitalVelocity(body.transform.position, currentAltitude);
            Vector3 velocityCorrection = (idealVelocity - rb.linearVelocity) * orbitStabilizationStrength * 0.1f;
            rb.AddForce(velocityCorrection, ForceMode.VelocityChange);

            body.OrbitalAltitude = currentAltitude;
            body.OrbitalVelocity = rb.linearVelocity.magnitude;
        }

        private void CheckForOrbitDecay(OrbitalBody body)
        {
            if (GravitySystem.Instance == null) return;

            float altitude = GravitySystem.Instance.GetDistanceFromRingSurface(body.transform.position);

            if (altitude < minOrbitalAltitude || altitude > maxOrbitalAltitude)
            {
                body.OrbitStability -= orbitDecayRate * Time.fixedDeltaTime * 10f;
            }
            else
            {
                Rigidbody rb = body.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 idealVelocity = CalculateIdealOrbitalVelocity(body.transform.position, altitude);
                    float velocityMatch = Vector3.Dot(rb.linearVelocity.normalized, idealVelocity.normalized);
                    
                    if (velocityMatch < velocityMatchThreshold * 0.8f)
                    {
                        body.OrbitStability -= orbitDecayRate * Time.fixedDeltaTime;
                    }
                    else
                    {
                        body.OrbitStability = Mathf.Min(1f, body.OrbitStability + orbitDecayRate * Time.fixedDeltaTime * 0.5f);
                    }
                }
            }

            if (body.OrbitStability <= 0f)
            {
                ExitOrbit(body);
            }
        }

        private void ExitOrbit(OrbitalBody body)
        {
            body.IsInOrbit = false;
            body.OrbitStability = 0f;

            OnOrbitDecayed?.Invoke(body);
            Debug.Log($"[OrbitalLoop] {body.name} orbit decayed");
        }

        private void OnDrawGizmos()
        {
            if (!showOrbitalPaths || GravitySystem.Instance == null) return;

            Gizmos.color = orbitColor;
            
            int segments = 64;
            float minRadius = GravitySystem.Instance.RingRadius + minOrbitalAltitude;
            float maxRadius = GravitySystem.Instance.RingRadius + maxOrbitalAltitude;
            
            DrawOrbitRing(minRadius, segments);
            
            Gizmos.color = new Color(orbitColor.r, orbitColor.g, orbitColor.b, 0.5f);
            DrawOrbitRing(maxRadius, segments);
        }

        private void DrawOrbitRing(float radius, int segments)
        {
            Vector3 center = GravitySystem.Instance != null ? GravitySystem.Instance.RingCenter : Vector3.zero;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * Mathf.PI * 2f;
                float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                
                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0f);
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0f);
                
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}