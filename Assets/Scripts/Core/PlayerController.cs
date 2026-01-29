using UnityEngine;

namespace SkyRingTerrarium
{
    /// <summary>
    /// Main player controller handling movement, input, and player state.
    /// Works with the custom gravity system for ring-based movement.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runSpeedMultiplier = 1.6f;
        [SerializeField] private float airControl = 0.6f;
        
        [Header("Jump & Thrust")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float thrustForce = 15f;
        [SerializeField] private float maxThrustDuration = 2f;
        
        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.1f;
        
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        
        private Rigidbody rb;
        private Core.GravityAffectedBody gravityBody;
        private bool isGrounded;
        private bool isThrusting;
        private float thrustTimeRemaining;
        
        public bool IsGrounded => isGrounded;
        public bool IsThrusting => isThrusting;
        public Rigidbody Rigidbody => rb;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            gravityBody = GetComponent<Core.GravityAffectedBody>();
            thrustTimeRemaining = maxThrustDuration;
        }
        
        private void Update()
        {
            CheckGrounded();
            HandleInput();
        }
        
        private void FixedUpdate()
        {
            ApplyMovement();
        }
        
        private void CheckGrounded()
        {
            Vector3 gravityDir = gravityBody != null 
                ? gravityBody.CurrentGravityDirection 
                : Vector3.down;
            
            isGrounded = Physics.Raycast(
                transform.position, 
                gravityDir, 
                groundCheckDistance, 
                groundLayer
            );
        }
        
        private void HandleInput()
        {
            // Jump
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                Jump();
            }
            
            // Thrust
            if (Input.GetButton("Fire1") && !isGrounded && thrustTimeRemaining > 0)
            {
                StartThrust();
            }
            else
            {
                StopThrust();
            }
        }
        
        private void ApplyMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            Vector3 moveDirection = Vector3.zero;
            
            if (cameraTransform != null)
            {
                moveDirection = cameraTransform.forward * vertical + cameraTransform.right * horizontal;
                moveDirection.y = 0;
                moveDirection.Normalize();
            }
            else
            {
                moveDirection = new Vector3(horizontal, 0, vertical).normalized;
            }
            
            float speed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed *= runSpeedMultiplier;
            }
            
            float controlMultiplier = isGrounded ? 1f : airControl;
            
            Vector3 targetVelocity = moveDirection * speed * controlMultiplier;
            Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 velocityChange = targetVelocity - currentHorizontalVelocity;
            
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
            
            // Apply thrust
            if (isThrusting)
            {
                Vector3 thrustDir = gravityBody != null 
                    ? -gravityBody.CurrentGravityDirection 
                    : Vector3.up;
                rb.AddForce(thrustDir * thrustForce, ForceMode.Acceleration);
                thrustTimeRemaining -= Time.fixedDeltaTime;
            }
        }
        
        private void Jump()
        {
            Vector3 jumpDir = gravityBody != null 
                ? -gravityBody.CurrentGravityDirection 
                : Vector3.up;
            rb.AddForce(jumpDir * jumpForce, ForceMode.Impulse);
        }
        
        private void StartThrust()
        {
            isThrusting = true;
        }
        
        private void StopThrust()
        {
            isThrusting = false;
        }
        
        /// <summary>
        /// Refill thrust fuel
        /// </summary>
        public void RefillThrust()
        {
            thrustTimeRemaining = maxThrustDuration;
        }
        
        /// <summary>
        /// Get remaining thrust time as a percentage (0-1)
        /// </summary>
        public float GetThrustPercentage()
        {
            return thrustTimeRemaining / maxThrustDuration;
        }
    }
}
