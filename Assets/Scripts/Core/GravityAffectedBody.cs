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
        [SerializeField] private bool gravityEnabled = true;
        
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
        
        public bool IsGravityEnabled
        {
            get => gravityEnabled && useCustomGravity;
            set => gravityEnabled = value;
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
            if (!useCustomGravity || !gravityEnabled) return;
            
            ApplyGravity();
            
            if (autoAlignToGravity)
            {
                AlignToGravity();
            }
        }

        private void ApplyGravity()
        {
            if (GravitySystem.Instance == null) return;
            
            Vector3 gravityDirection;
            
            if (hasCustomDirection && allowDirectionOverride)
            {
                gravityDirection = customGravityDirection;
            }
            else
            {
                gravityDirection = GravitySystem.Instance.GetGravityDirection(transform.position);
            }
            
            currentGravityDirection = gravityDirection;
            
            float effectiveScale = gravityScale;
            if (isInFloatBand)
            {
                effectiveScale *= (1f - floatBandBlend);
            }
            
            Vector3 gravity = GravitySystem.Instance.CalculateGravity(transform.position);
            rb.AddForce(gravity * effectiveScale, ForceMode.Acceleration);
        }

        private void AlignToGravity()
        {
            if (currentGravityDirection.sqrMagnitude < 0.001f) return;
            
            Quaternion targetRotation = Quaternion.FromToRotation(-transform.up, currentGravityDirection) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.fixedDeltaTime);
        }

        public void SetCustomGravityDirection(Vector3 direction)
        {
            customGravityDirection = direction.normalized;
            hasCustomDirection = true;
        }

        public void ClearCustomGravityDirection()
        {
            hasCustomDirection = false;
        }

        public void SetFloatBandBlend(float blend)
        {
            floatBandBlend = Mathf.Clamp01(blend);
        }

        public void EnableGravity()
        {
            gravityEnabled = true;
        }

        public void DisableGravity()
        {
            gravityEnabled = false;
        }
    }
}
