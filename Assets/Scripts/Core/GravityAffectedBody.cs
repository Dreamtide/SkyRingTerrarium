using UnityEngine;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Component for objects affected by the custom gravity system.
    /// Updated to support omnidirectional gravity with custom direction override.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GravityAffectedBody : MonoBehaviour
    {
        [Header("Gravity Settings")]
        [SerializeField] private float gravityScale = 1f;
        [SerializeField] private bool useCustomGravity = true;
        [SerializeField] private bool allowDirectionOverride = true;
        
        [Header("State")]
        [SerializeField] private bool isInFloatBand = false;
        [SerializeField] private bool isInOrbit = false;
        
        [Header("Orientation")]
        [SerializeField] private bool autoAlignToGravity = false;
        [SerializeField] private float alignmentSpeed = 5f;
        
        private Rigidbody rb;
        private Vector3 currentGravityDirection;
        private Vector3 customGravityDirection;
        private bool hasCustomDirection;
        private float floatBandBlend;
        
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
        
        public Vector3 CurrentGravityDirection => currentGravityDirection;
        public float FloatBandBlend => floatBandBlend;
        
        public Rigidbody Rigidbody => rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            currentGravityDirection = Vector3.down;
        }

        private void OnEnable()
        {
            if (GravitySystem.Instance != null)
            {
                GravitySystem.Instance.RegisterBody(this);
            }
        }

        private void OnDisable()
        {
            if (GravitySystem.Instance != null)
            {
                GravitySystem.Instance.UnregisterBody(this);
            }
        }

        private void FixedUpdate()
        {
            if (!useCustomGravity || isInOrbit) return;

            UpdateGravityDirection();
            ApplyGravity();
            
            if (autoAlignToGravity)
            {
                AlignToGravity();
            }
        }

        private void UpdateGravityDirection()
        {
            if (hasCustomDirection && allowDirectionOverride)
            {
                currentGravityDirection = customGravityDirection;
            }
            else if (GravitySystem.Instance != null)
            {
                currentGravityDirection = GravitySystem.Instance.GetGravityDirection(transform.position);
            }
            else
            {
                currentGravityDirection = Vector3.down;
            }
        }

        private void ApplyGravity()
        {
            if (isInFloatBand)
            {
                return;
            }

            float strength = GravitySystem.Instance != null 
                ? GravitySystem.Instance.GravityStrength 
                : 9.81f;

            float effectiveStrength = strength * gravityScale * (1f - floatBandBlend);
            Vector3 gravityForce = currentGravityDirection * effectiveStrength;
            
            rb.AddForce(gravityForce, ForceMode.Acceleration);
        }

        private void AlignToGravity()
        {
            Vector3 up = -currentGravityDirection;
            Vector3 forward = transform.forward;
            
            Vector3 projectedForward = Vector3.ProjectOnPlane(forward, up);
            if (projectedForward.sqrMagnitude < 0.001f)
            {
                projectedForward = Vector3.ProjectOnPlane(transform.right, up);
            }
            
            Quaternion targetRotation = Quaternion.LookRotation(projectedForward.normalized, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Set a custom gravity direction, overriding the radial ring gravity.
        /// Used by OmnidirectionalGravity during and after gravity flips.
        /// </summary>
        public void SetCustomGravityDirection(Vector3 direction)
        {
            customGravityDirection = direction.normalized;
            hasCustomDirection = true;
            currentGravityDirection = customGravityDirection;
        }

        /// <summary>
        /// Clear the custom gravity direction, returning to radial ring gravity.
        /// </summary>
        public void ClearCustomGravityDirection()
        {
            hasCustomDirection = false;
        }

        /// <summary>
        /// Set the float band blend factor (0 = full gravity, 1 = zero gravity).
        /// </summary>
        public void SetFloatBandBlend(float blend)
        {
            floatBandBlend = Mathf.Clamp01(blend);
        }

        /// <summary>
        /// Temporarily add a force in world space (knockback, explosions, etc.).
        /// </summary>
        public void AddWorldForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
        {
            rb.AddForce(force, mode);
        }

        /// <summary>
        /// Add a force relative to current gravity orientation.
        /// Positive Y is "up" (against gravity).
        /// </summary>
        public void AddLocalForce(Vector3 localForce, ForceMode mode = ForceMode.Impulse)
        {
            Vector3 up = -currentGravityDirection;
            Vector3 right = Vector3.Cross(up, transform.forward).normalized;
            Vector3 forward = Vector3.Cross(right, up).normalized;

            Vector3 worldForce = right * localForce.x + up * localForce.y + forward * localForce.z;
            rb.AddForce(worldForce, mode);
        }

        /// <summary>
        /// Get the "up" direction (opposite of gravity).
        /// </summary>
        public Vector3 GetUp()
        {
            return -currentGravityDirection;
        }

        /// <summary>
        /// Check if this body is grounded relative to gravity direction.
        /// </summary>
        public bool CheckGrounded(float distance = 0.1f, LayerMask? groundMask = null)
        {
            LayerMask mask = groundMask ?? Physics.DefaultRaycastLayers;
            return Physics.Raycast(transform.position, currentGravityDirection, distance, mask);
        }
    }
}