using UnityEngine;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Component for objects affected by the custom gravity system.
    /// Attach this to any object that should respond to ring gravity.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GravityAffectedBody : MonoBehaviour
    {
        [Header("Gravity Settings")]
        [SerializeField] private float gravityScale = 1f;
        [SerializeField] private bool useCustomGravity = true;
        
        [Header("State")]
        [SerializeField] private bool isInFloatBand = false;
        [SerializeField] private bool isInOrbit = false;
        
        private Rigidbody rb;
        private Vector3 currentGravityDirection;
        
        public float GravityScale 
        { 
            get => gravityScale; 
            set => gravityScale = value; 
        }
        
        public bool UseCustomGravity 
        { 
            get => useCustomGravity; 
            set => useCustomGravity = value; 
        }
        
        public bool IsInFloatBand 
        { 
            get => isInFloatBand; 
            set => isInFloatBand = value; 
        }
        
        public bool IsInOrbit 
        { 
            get => isInOrbit; 
            set => isInOrbit = value; 
        }
        
        public Rigidbody Rigidbody => rb;
        public Vector3 CurrentGravityDirection => currentGravityDirection;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false; // We handle gravity ourselves
            }
        }
        
        private void Start()
        {
            // Register with gravity system
            if (GravitySystem.Instance != null)
            {
                GravitySystem.Instance.RegisterBody(this);
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from gravity system
            if (GravitySystem.Instance != null)
            {
                GravitySystem.Instance.UnregisterBody(this);
            }
        }
        
        /// <summary>
        /// Apply gravity force to this body
        /// </summary>
        public void ApplyGravity(Vector3 gravityDirection, float strength)
        {
            if (!useCustomGravity || rb == null) return;
            
            currentGravityDirection = gravityDirection;
            float effectiveStrength = strength * gravityScale;
            
            if (!isInFloatBand)
            {
                rb.AddForce(gravityDirection * effectiveStrength, ForceMode.Acceleration);
            }
        }
        
        /// <summary>
        /// Get the current velocity of this body
        /// </summary>
        public Vector3 GetVelocity()
        {
            return rb != null ? rb.linearVelocity : Vector3.zero;
        }
        
        /// <summary>
        /// Set the velocity of this body
        /// </summary>
        public void SetVelocity(Vector3 velocity)
        {
            if (rb != null)
            {
                rb.linearVelocity = velocity;
            }
        }
    }
}
