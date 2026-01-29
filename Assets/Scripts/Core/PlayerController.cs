using UnityEngine;

namespace SkyRingTerrarium
{
    /// <summary>
    /// Main player controller handling movement, input, and player state.
    /// Updated for omnidirectional gravity - ground is relative to current gravity direction.
    /// Movement and thrust work relative to current orientation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runSpeedMultiplier = 1.6f;
        [SerializeField] private float airControl = 0.6f;
        [SerializeField] private float rotationSpeed = 10f;
        
        [Header("Jump & Thrust")]
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float thrustForce = 15f;
        [SerializeField] private float maxThrustDuration = 2f;
        [SerializeField] private float thrustRechargeRate = 0.5f;
        
        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private float groundCheckRadius = 0.3f;
        
        [Header("Gravity Selection")]
        [SerializeField] private KeyCode gravitySelectKey = KeyCode.Space;
        [SerializeField] private KeyCode gravityCommitKey = KeyCode.Mouse0;
        [SerializeField] private bool useMouseForAiming = true;
        [SerializeField] private float mouseAimDistance = 100f;
        
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        private Rigidbody rb;
        private Core.GravityAffectedBody gravityBody;
        private Core.OmnidirectionalGravity omniGravity;
        private bool isGrounded;
        private bool isThrusting;
        private float thrustTimeRemaining;
        private Vector2 moveInput;
        private bool jumpRequested;
        private bool thrustRequested;
        private Vector3 currentUp = Vector3.up;
        private Quaternion targetRotation;

        public bool IsGrounded => isGrounded;
        public bool IsThrusting => isThrusting;
        public bool IsInGravitySelectionMode => omniGravity != null && omniGravity.IsInSelectionMode;
        public Rigidbody Rigidbody => rb;
        public Vector3 GravityUp => currentUp;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            gravityBody = GetComponent<Core.GravityAffectedBody>();
            thrustTimeRemaining = maxThrustDuration;
            targetRotation = transform.rotation;
        }

        private void Start()
        {
            omniGravity = Core.OmnidirectionalGravity.Instance;
            if (omniGravity != null && gravityBody != null)
            {
                omniGravity.Initialize(gravityBody);
            }

            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void Update()
        {
            UpdateGravityDirection();
            CheckGrounded();
            HandleInput();
            UpdateRotation();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
            ApplyThrust();
        }

        private void UpdateGravityDirection()
        {
            if (omniGravity != null)
            {
                currentUp = -omniGravity.CurrentGravityDirection;
            }
            else if (gravityBody != null)
            {
                currentUp = -gravityBody.CurrentGravityDirection;
            }
        }

        private void CheckGrounded()
        {
            Vector3 checkOrigin = transform.position + currentUp * 0.1f;
            isGrounded = Physics.SphereCast(
                checkOrigin,
                groundCheckRadius,
                -currentUp,
                out RaycastHit hit,
                groundCheckDistance + 0.1f,
                groundLayer
            );

            if (isGrounded && thrustTimeRemaining < maxThrustDuration)
            {
                thrustTimeRemaining += thrustRechargeRate * Time.deltaTime;
                thrustTimeRemaining = Mathf.Min(thrustTimeRemaining, maxThrustDuration);
            }
        }

        private void HandleInput()
        {
            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            
            HandleGravitySelectionInput();
            
            if (!IsInGravitySelectionMode)
            {
                HandleMovementInput();
            }
        }

        private void HandleGravitySelectionInput()
        {
            if (omniGravity == null) return;

            if (gravityBody != null && gravityBody.IsInFloatBand)
            {
                if (Input.GetKeyDown(gravitySelectKey))
                {
                    omniGravity.TryEnterSelectionMode();
                }
            }

            if (IsInGravitySelectionMode)
            {
                if (useMouseForAiming)
                {
                    UpdateMouseAiming();
                }
                else
                {
                    Vector2 aimInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                    omniGravity.SetAimInput(aimInput);
                }

                if (Input.GetKeyDown(gravityCommitKey))
                {
                    omniGravity.CommitGravityFlip();
                }
                
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    omniGravity.ExitSelectionMode();
                }
            }
        }

        private void UpdateMouseAiming()
        {
            if (cameraTransform == null) return;

            Camera cam = cameraTransform.GetComponent<Camera>();
            if (cam == null) return;

            Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
            
            Plane aimPlane = new Plane(-cameraTransform.forward, transform.position);
            
            if (aimPlane.Raycast(mouseRay, out float distance))
            {
                Vector3 worldPoint = mouseRay.GetPoint(distance);
                Vector3 aimDirection = (worldPoint - transform.position).normalized;
                omniGravity.SetAimedDirection(aimDirection);
            }
        }

        private void HandleMovementInput()
        {
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                jumpRequested = true;
            }

            thrustRequested = Input.GetButton("Jump") && !isGrounded && thrustTimeRemaining > 0;
        }

        private void UpdateRotation()
        {
            Vector3 forward = Vector3.Cross(currentUp, Vector3.forward);
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.Cross(currentUp, Vector3.right);
            }
            forward.Normalize();

            targetRotation = Quaternion.LookRotation(forward, currentUp);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void ApplyMovement()
        {
            if (IsInGravitySelectionMode) return;

            Vector3 right = Vector3.Cross(currentUp, transform.forward).normalized;
            Vector3 forward = Vector3.Cross(right, currentUp).normalized;

            float currentSpeed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= runSpeedMultiplier;
            }

            Vector3 moveDirection = (right * moveInput.x + forward * moveInput.y).normalized;
            float controlFactor = isGrounded ? 1f : airControl;
            
            Vector3 targetVelocity = moveDirection * currentSpeed * controlFactor;
            
            Vector3 velocityAlongGravity = Vector3.Project(rb.linearVelocity, currentUp);
            Vector3 horizontalVelocity = rb.linearVelocity - velocityAlongGravity;
            
            Vector3 newHorizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, 10f * Time.fixedDeltaTime);
            rb.linearVelocity = newHorizontalVelocity + velocityAlongGravity;

            if (jumpRequested && isGrounded)
            {
                rb.AddForce(currentUp * jumpForce, ForceMode.Impulse);
                jumpRequested = false;
            }
        }

        private void ApplyThrust()
        {
            if (IsInGravitySelectionMode) return;

            if (thrustRequested && thrustTimeRemaining > 0)
            {
                if (!isThrusting)
                {
                    isThrusting = true;
                }

                rb.AddForce(currentUp * thrustForce, ForceMode.Force);
                thrustTimeRemaining -= Time.fixedDeltaTime;
            }
            else
            {
                isThrusting = false;
            }
        }

        public void SetCameraTransform(Transform camTransform)
        {
            cameraTransform = camTransform;
        }

        public float GetThrustFuelRatio()
        {
            return thrustTimeRemaining / maxThrustDuration;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 up = Application.isPlaying ? currentUp : transform.up;
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, up * 2f);
            
            Gizmos.color = Color.red;
            Vector3 checkOrigin = transform.position + up * 0.1f;
            Gizmos.DrawWireSphere(checkOrigin - up * groundCheckDistance, groundCheckRadius);
        }
#endif
    }
}