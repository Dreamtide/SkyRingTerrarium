using UnityEngine;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Camera controller that follows the player and aligns with gravity direction.
    /// Smoothly rotates to keep "down" aligned with current gravity.
    /// Supports parallax backgrounds and dynamic zoom.
    /// </summary>
    public class GravityCameraController : MonoBehaviour
    {
        [Header("Target & Follow")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 baseOffset = new Vector3(0f, 2f, -10f);
        [SerializeField] private float followSmoothTime = 0.15f;
        [SerializeField] private float maxFollowSpeed = 50f;

        [Header("Gravity Alignment")]
        [SerializeField] private float rotationSmoothTime = 0.25f;
        [SerializeField] private float flipRotationSpeed = 5f;
        [SerializeField] private bool alignWithGravity = true;

        [Header("Dynamic Zoom")]
        [SerializeField] private bool enableDynamicZoom = true;
        [SerializeField] private float baseZoom = 10f;
        [SerializeField] private float maxZoomOut = 15f;
        [SerializeField] private float zoomSpeedThreshold = 10f;
        [SerializeField] private float zoomFlipMultiplier = 1.3f;
        [SerializeField] private float zoomSmoothTime = 0.3f;

        [Header("Look Ahead")]
        [SerializeField] private bool enableLookAhead = true;
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float lookAheadSmoothTime = 0.5f;

        [Header("Screen Shake")]
        [SerializeField] private float flipShakeIntensity = 0.3f;
        [SerializeField] private float flipShakeDuration = 0.3f;

        [Header("Parallax Layers")]
        [SerializeField] private ParallaxLayer[] parallaxLayers;

        private Camera mainCamera;
        private Vector3 currentVelocity;
        private Vector3 lookAheadVelocity;
        private Vector3 currentLookAhead;
        private float currentZoom;
        private float zoomVelocity;
        private Quaternion targetRotation;
        private float rotationVelocity;
        private Vector3 shakeOffset;
        private float shakeTimer;
        private Vector3 lastTargetPosition;
        private Vector3 targetVelocity;
        private Vector3 currentGravityUp = Vector3.up;
        private float gravityRotationVelocity;

        public Camera Camera => mainCamera;
        public Vector3 GravityUp => currentGravityUp;

        [System.Serializable]
        public class ParallaxLayer
        {
            public Transform layerTransform;
            [Range(0f, 1f)]
            public float parallaxFactor = 0.5f;
            public bool lockVertical;
            [HideInInspector]
            public Vector3 startPosition;
        }

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            currentZoom = baseZoom;
            targetRotation = transform.rotation;

            InitializeParallaxLayers();
        }

        private void Start()
        {
            if (target != null)
            {
                lastTargetPosition = target.position;
            }

            OmnidirectionalGravity.OnGravityFlipStarted += OnGravityFlipStarted;
            OmnidirectionalGravity.OnGravityFlipCompleted += OnGravityFlipCompleted;
        }

        private void OnDestroy()
        {
            OmnidirectionalGravity.OnGravityFlipStarted -= OnGravityFlipStarted;
            OmnidirectionalGravity.OnGravityFlipCompleted -= OnGravityFlipCompleted;
        }

        private void InitializeParallaxLayers()
        {
            if (parallaxLayers == null) return;

            foreach (var layer in parallaxLayers)
            {
                if (layer.layerTransform != null)
                {
                    layer.startPosition = layer.layerTransform.position;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            UpdateTargetVelocity();
            UpdateGravityAlignment();
            UpdatePosition();
            UpdateZoom();
            UpdateShake();
            UpdateParallax();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                lastTargetPosition = target.position;
            }
        }

        private void UpdateTargetVelocity()
        {
            if (target == null) return;

            targetVelocity = (target.position - lastTargetPosition) / Time.deltaTime;
            lastTargetPosition = target.position;
        }

        private void UpdateGravityAlignment()
        {
            if (!alignWithGravity) return;

            Vector3 gravityDir = Vector3.down;
            
            if (OmnidirectionalGravity.Instance != null)
            {
                gravityDir = OmnidirectionalGravity.Instance.CurrentGravityDirection;
            }
            else if (GravitySystem.Instance != null && target != null)
            {
                gravityDir = GravitySystem.Instance.GetGravityDirection(target.position);
            }

            Vector3 newGravityUp = -gravityDir;
            
            float angle = Vector3.Angle(currentGravityUp, newGravityUp);
            float speed = OmnidirectionalGravity.Instance != null && OmnidirectionalGravity.Instance.IsFlipping
                ? flipRotationSpeed
                : 1f / rotationSmoothTime;

            currentGravityUp = Vector3.Slerp(currentGravityUp, newGravityUp, speed * Time.deltaTime);
            currentGravityUp.Normalize();

            Vector3 forward = -Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(currentGravityUp, forward)) > 0.99f)
            {
                forward = Vector3.right;
            }
            
            targetRotation = Quaternion.LookRotation(forward, currentGravityUp);
        }

        private void UpdatePosition()
        {
            Vector3 rotatedOffset = targetRotation * baseOffset;
            Vector3 targetPosition = target.position + rotatedOffset;

            if (enableLookAhead && targetVelocity.sqrMagnitude > 0.1f)
            {
                Vector3 lookAheadTarget = targetVelocity.normalized * lookAheadDistance;
                currentLookAhead = Vector3.SmoothDamp(currentLookAhead, lookAheadTarget, ref lookAheadVelocity, lookAheadSmoothTime);
                targetPosition += currentLookAhead;
            }

            Vector3 newPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition + shakeOffset,
                ref currentVelocity,
                followSmoothTime,
                maxFollowSpeed
            );

            transform.position = newPosition;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / rotationSmoothTime);
        }

        private void UpdateZoom()
        {
            if (!enableDynamicZoom || mainCamera == null) return;

            float targetZoom = baseZoom;

            float speed = targetVelocity.magnitude;
            if (speed > zoomSpeedThreshold)
            {
                float speedFactor = Mathf.InverseLerp(zoomSpeedThreshold, zoomSpeedThreshold * 3f, speed);
                targetZoom = Mathf.Lerp(baseZoom, maxZoomOut, speedFactor);
            }

            if (OmnidirectionalGravity.Instance != null && OmnidirectionalGravity.Instance.IsFlipping)
            {
                targetZoom *= zoomFlipMultiplier;
            }

            currentZoom = Mathf.SmoothDamp(currentZoom, targetZoom, ref zoomVelocity, zoomSmoothTime);

            if (mainCamera.orthographic)
            {
                mainCamera.orthographicSize = currentZoom;
            }
            else
            {
                float fovZoom = Mathf.Lerp(50f, 70f, (currentZoom - baseZoom) / (maxZoomOut - baseZoom));
                mainCamera.fieldOfView = fovZoom;
            }
        }

        private void UpdateShake()
        {
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;
                float intensity = flipShakeIntensity * (shakeTimer / flipShakeDuration);
                shakeOffset = new Vector3(
                    Random.Range(-intensity, intensity),
                    Random.Range(-intensity, intensity),
                    0f
                );
            }
            else
            {
                shakeOffset = Vector3.zero;
            }
        }

        private void UpdateParallax()
        {
            if (parallaxLayers == null || target == null) return;

            Vector3 cameraMovement = transform.position;

            foreach (var layer in parallaxLayers)
            {
                if (layer.layerTransform == null) continue;

                Vector3 parallaxOffset = cameraMovement * layer.parallaxFactor;
                
                Vector3 newPosition = layer.startPosition + parallaxOffset;
                
                if (layer.lockVertical)
                {
                    newPosition.y = layer.startPosition.y;
                }

                layer.layerTransform.position = newPosition;
            }
        }

        private void OnGravityFlipStarted(Vector3 from, Vector3 to)
        {
            TriggerShake();
        }

        private void OnGravityFlipCompleted(Vector3 newDirection)
        {
        }

        public void TriggerShake()
        {
            shakeTimer = flipShakeDuration;
        }

        public void TriggerShake(float intensity, float duration)
        {
            flipShakeIntensity = intensity;
            flipShakeDuration = duration;
            shakeTimer = duration;
        }

        public void SetZoom(float zoom, bool instant = false)
        {
            if (instant)
            {
                currentZoom = zoom;
                zoomVelocity = 0f;
            }
            baseZoom = zoom;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (target == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(target.position, 0.5f);
            Gizmos.DrawLine(target.position, target.position + currentGravityUp * 3f);

            Gizmos.color = Color.yellow;
            Vector3 offset = targetRotation * baseOffset;
            Gizmos.DrawLine(target.position, target.position + offset);
        }
#endif
    }
}