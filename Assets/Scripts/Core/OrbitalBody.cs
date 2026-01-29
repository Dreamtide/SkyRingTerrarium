using UnityEngine;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Component for objects that can enter and maintain orbital trajectories.
    /// Works with the OrbitalLoop system to manage stable orbits.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class OrbitalBody : MonoBehaviour
    {
        [Header("Orbital State")]
        [SerializeField] private bool isInOrbit = false;
        [SerializeField] private float orbitalAltitude = 0f;
        [SerializeField] private float orbitalVelocity = 0f;
        
        [Header("Orbit Quality")]
        [SerializeField] private float orbitStability = 0f;
        [SerializeField] private float velocityMatchRatio = 0f;
        
        private Rigidbody rb;
        private GravityAffectedBody gravityBody;
        
        public bool IsInOrbit 
        { 
            get => isInOrbit; 
            set => isInOrbit = value; 
        }
        
        public float OrbitalAltitude 
        { 
            get => orbitalAltitude; 
            set => orbitalAltitude = value; 
        }
        
        public float OrbitalVelocity 
        { 
            get => orbitalVelocity; 
            set => orbitalVelocity = value; 
        }
        
        public float OrbitStability 
        { 
            get => orbitStability; 
            set => orbitStability = value; 
        }
        
        public float VelocityMatchRatio 
        { 
            get => velocityMatchRatio; 
            set => velocityMatchRatio = value; 
        }
        
        public Rigidbody Rigidbody => rb;
        public GravityAffectedBody GravityBody => gravityBody;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            gravityBody = GetComponent<GravityAffectedBody>();
        }
        
        /// <summary>
        /// Get the current velocity magnitude
        /// </summary>
        public float GetSpeed()
        {
            return rb != null ? rb.linearVelocity.magnitude : 0f;
        }
        
        /// <summary>
        /// Get the tangential velocity component relative to the ring center
        /// </summary>
        public Vector3 GetTangentialVelocity(Vector3 ringCenter)
        {
            if (rb == null) return Vector3.zero;
            
            Vector3 toCenter = (ringCenter - transform.position).normalized;
            Vector3 velocity = rb.linearVelocity;
            
            // Remove radial component to get tangential
            Vector3 radialComponent = Vector3.Dot(velocity, toCenter) * toCenter;
            return velocity - radialComponent;
        }
        
        /// <summary>
        /// Apply orbital correction force
        /// </summary>
        public void ApplyOrbitalCorrection(Vector3 correctionForce)
        {
            if (rb != null && isInOrbit)
            {
                rb.AddForce(correctionForce, ForceMode.Acceleration);
            }
        }
        
        /// <summary>
        /// Enter orbit at current position
        /// </summary>
        public void EnterOrbit(float altitude, float velocity)
        {
            isInOrbit = true;
            orbitalAltitude = altitude;
            orbitalVelocity = velocity;
            
            if (gravityBody != null)
            {
                gravityBody.IsInOrbit = true;
            }
        }
        
        /// <summary>
        /// Exit orbital state
        /// </summary>
        public void ExitOrbit()
        {
            isInOrbit = false;
            orbitStability = 0f;
            
            if (gravityBody != null)
            {
                gravityBody.IsInOrbit = false;
            }
        }
    }
}
