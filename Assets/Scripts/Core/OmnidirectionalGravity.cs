using UnityEngine;
using System;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Manages omnidirectional gravity for the Sky Ring.
    /// Players can flip gravity toward ANY direction on the ring, not just antipode.
    /// When in float band, enters gravity selection mode to aim and commit new direction.
    /// </summary>
    public class OmnidirectionalGravity : MonoBehaviour
    {
        public static OmnidirectionalGravity Instance { get; private set; }

        public static event Action<Vector3, Vector3> OnGravityFlipStarted;
        public static event Action<Vector3> OnGravityFlipCompleted;
        public static event Action OnGravitySelectionModeEntered;
        public static event Action OnGravitySelectionModeExited;

        [Header("Gravity Flip Settings")]
        [SerializeField] private float flipTransitionDuration = 0.5f;
        [SerializeField] private AnimationCurve flipTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float minFlipAngle = 15f;
        
        [Header("Selection Mode")]
        [SerializeField] private float selectionSensitivity = 2f;
        [SerializeField] private float maxAimAngle = 180f;
        [SerializeField] private bool requireFloatBandForSelection = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool showAimIndicator = true;
        [SerializeField] private Color aimValidColor = new Color(0.2f, 1f, 0.4f, 0.8f);
        [SerializeField] private Color aimInvalidColor = new Color(1f, 0.3f, 0.2f, 0.5f);

        private GravityAffectedBody targetBody;
        private Vector3 currentGravityDirection = Vector3.down;
        private Vector3 targetGravityDirection = Vector3.down;
        private Vector3 flipStartDirection;
        private float flipProgress = 1f;
        private bool isFlipping;
        private bool isInSelectionMode;
        private Vector3 aimedDirection;
        private Vector2 aimInput;

        public Vector3 CurrentGravityDirection => currentGravityDirection;
        public Vector3 TargetGravityDirection => targetGravityDirection;
        public bool IsFlipping => isFlipping;
        public bool IsInSelectionMode => isInSelectionMode;
        public Vector3 AimedDirection => aimedDirection;
        public float FlipProgress => flipProgress;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize(GravityAffectedBody body)
        {
            targetBody = body;
            
            if (GravitySystem.Instance != null)
            {
                currentGravityDirection = GravitySystem.Instance.GetGravityDirection(body.transform.position);
                targetGravityDirection = currentGravityDirection;
            }
        }

        private void Update()
        {
            if (targetBody == null) return;

            UpdateFlipTransition();
            
            if (isInSelectionMode)
            {
                UpdateAimDirection();
            }
        }

        /// <summary>
        /// Enter gravity selection mode when in float band.
        /// </summary>
        public bool TryEnterSelectionMode()
        {
            if (isFlipping) return false;
            
            if (requireFloatBandForSelection && targetBody != null && !targetBody.IsInFloatBand)
            {
                return false;
            }

            if (!isInSelectionMode)
            {
                isInSelectionMode = true;
                aimedDirection = currentGravityDirection;
                OnGravitySelectionModeEntered?.Invoke();
            }
            return true;
        }

        /// <summary>
        /// Exit gravity selection mode without committing.
        /// </summary>
        public void ExitSelectionMode()
        {
            if (isInSelectionMode)
            {
                isInSelectionMode = false;
                OnGravitySelectionModeExited?.Invoke();
            }
        }

        /// <summary>
        /// Set aim input for gravity selection (mouse delta or stick input).
        /// </summary>
        public void SetAimInput(Vector2 input)
        {
            aimInput = input;
        }

        /// <summary>
        /// Set aimed direction directly (for mouse world position aiming).
        /// </summary>
        public void SetAimedDirection(Vector3 direction)
        {
            if (!isInSelectionMode) return;
            
            aimedDirection = direction.normalized;
            ClampAimedDirection();
        }

        private void UpdateAimDirection()
        {
            if (aimInput.sqrMagnitude < 0.001f) return;

            Vector3 right = GetLocalRight();
            Vector3 forward = GetLocalForward();

            Vector3 aimDelta = (right * aimInput.x + forward * aimInput.y) * selectionSensitivity * Time.deltaTime;
            aimedDirection = (aimedDirection + aimDelta).normalized;
            
            ClampAimedDirection();
        }

        private void ClampAimedDirection()
        {
            float angle = Vector3.Angle(-currentGravityDirection, aimedDirection);
            if (angle > maxAimAngle)
            {
                Vector3 axis = Vector3.Cross(-currentGravityDirection, aimedDirection).normalized;
                aimedDirection = Quaternion.AngleAxis(maxAimAngle, axis) * (-currentGravityDirection);
            }
        }

        private Vector3 GetLocalRight()
        {
            if (targetBody == null) return Vector3.right;
            return targetBody.transform.right;
        }

        private Vector3 GetLocalForward()
        {
            if (targetBody == null) return Vector3.forward;
            return targetBody.transform.forward;
        }

        /// <summary>
        /// Commit the aimed gravity direction and start the flip.
        /// </summary>
        public bool CommitGravityFlip()
        {
            if (!isInSelectionMode || isFlipping) return false;

            float angle = Vector3.Angle(currentGravityDirection, aimedDirection);
            if (angle < minFlipAngle)
            {
                ExitSelectionMode();
                return false;
            }

            StartGravityFlip(aimedDirection);
            ExitSelectionMode();
            return true;
        }

        /// <summary>
        /// Flip gravity to a specific direction.
        /// </summary>
        public void StartGravityFlip(Vector3 newDirection)
        {
            if (isFlipping) return;

            newDirection = newDirection.normalized;
            
            flipStartDirection = currentGravityDirection;
            targetGravityDirection = newDirection;
            flipProgress = 0f;
            isFlipping = true;

            OnGravityFlipStarted?.Invoke(flipStartDirection, targetGravityDirection);
        }

        /// <summary>
        /// Flip gravity to point toward a world position.
        /// </summary>
        public void FlipGravityToward(Vector3 worldPosition)
        {
            if (targetBody == null) return;

            Vector3 direction = (worldPosition - targetBody.transform.position).normalized;
            StartGravityFlip(direction);
        }

        /// <summary>
        /// Flip to the antipode (opposite side of ring).
        /// </summary>
        public void FlipToAntipode()
        {
            StartGravityFlip(-currentGravityDirection);
        }

        private void UpdateFlipTransition()
        {
            if (!isFlipping) return;

            flipProgress += Time.deltaTime / flipTransitionDuration;
            
            if (flipProgress >= 1f)
            {
                flipProgress = 1f;
                isFlipping = false;
                currentGravityDirection = targetGravityDirection;
                OnGravityFlipCompleted?.Invoke(currentGravityDirection);
            }
            else
            {
                float t = flipTransitionCurve.Evaluate(flipProgress);
                currentGravityDirection = Vector3.Slerp(flipStartDirection, targetGravityDirection, t).normalized;
            }

            if (targetBody != null)
            {
                targetBody.SetCustomGravityDirection(currentGravityDirection);
            }
        }

        /// <summary>
        /// Get gravity direction at a specific angle around the ring from current position.
        /// </summary>
        public Vector3 GetGravityDirectionAtAngle(float angleOffset)
        {
            if (GravitySystem.Instance == null || targetBody == null)
            {
                return Vector3.down;
            }

            Vector3 ringAxis = GravitySystem.Instance.RingAxis;
            return Quaternion.AngleAxis(angleOffset, ringAxis) * currentGravityDirection;
        }

        /// <summary>
        /// Check if a gravity direction is valid (not blocked, within limits).
        /// </summary>
        public bool IsValidGravityDirection(Vector3 direction)
        {
            float angle = Vector3.Angle(currentGravityDirection, direction);
            return angle >= minFlipAngle && angle <= maxAimAngle;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (targetBody == null) return;

            Vector3 pos = targetBody.transform.position;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(pos, currentGravityDirection * 3f);

            if (isInSelectionMode)
            {
                Gizmos.color = IsValidGravityDirection(aimedDirection) ? aimValidColor : aimInvalidColor;
                Gizmos.DrawRay(pos, aimedDirection * 4f);
                
                UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.2f);
                UnityEditor.Handles.DrawWireDisc(pos, currentGravityDirection, 2f);
            }

            if (isFlipping)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(pos, targetGravityDirection * 3f);
            }
        }
#endif
    }
}