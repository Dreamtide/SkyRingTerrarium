using UnityEngine;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Handles visual feedback for gravity system including:
    /// - Gravity direction indicator arrow
    /// - Aim reticle during gravity selection
    /// - Screen effects during flip transition
    /// </summary>
    public class GravityUIController : MonoBehaviour
    {
        [Header("Gravity Direction Indicator")]
        [SerializeField] private bool showGravityIndicator = true;
        [SerializeField] private LineRenderer gravityArrow;
        [SerializeField] private float arrowLength = 2f;
        [SerializeField] private Color normalGravityColor = new Color(0.3f, 0.6f, 1f, 0.8f);
        [SerializeField] private Color flippingGravityColor = new Color(1f, 0.8f, 0.2f, 1f);
        
        [Header("Aim Reticle")]
        [SerializeField] private bool showAimReticle = true;
        [SerializeField] private LineRenderer aimReticleLine;
        [SerializeField] private float reticleLength = 3f;
        [SerializeField] private Color validAimColor = new Color(0.2f, 1f, 0.4f, 0.9f);
        [SerializeField] private Color invalidAimColor = new Color(1f, 0.3f, 0.2f, 0.6f);
        [SerializeField] private int reticleSegments = 32;
        
        [Header("Radial Selection UI")]
        [SerializeField] private LineRenderer radialGuide;
        [SerializeField] private float radialRadius = 2f;
        [SerializeField] private Color radialColor = new Color(1f, 1f, 1f, 0.3f);
        
        [Header("Screen Effects")]
        [SerializeField] private bool enableScreenEffects = true;
        [SerializeField] private CanvasGroup flipEffectOverlay;
        [SerializeField] private Color flipEffectColor = new Color(0.8f, 0.9f, 1f, 0.3f);
        [SerializeField] private float flipEffectDuration = 0.4f;
        
        [Header("Target Reference")]
        [SerializeField] private Transform playerTransform;

        private float flipEffectTimer;
        private bool isShowingFlipEffect;
        private Material lineMaterial;

        private void Awake()
        {
            CreateLineMaterial();
            SetupLineRenderers();
        }

        private void Start()
        {
            OmnidirectionalGravity.OnGravityFlipStarted += OnFlipStarted;
            OmnidirectionalGravity.OnGravityFlipCompleted += OnFlipCompleted;
            OmnidirectionalGravity.OnGravitySelectionModeEntered += OnSelectionModeEntered;
            OmnidirectionalGravity.OnGravitySelectionModeExited += OnSelectionModeExited;
            
            SetAimReticleVisible(false);
            SetRadialGuideVisible(false);
        }

        private void OnDestroy()
        {
            OmnidirectionalGravity.OnGravityFlipStarted -= OnFlipStarted;
            OmnidirectionalGravity.OnGravityFlipCompleted -= OnFlipCompleted;
            OmnidirectionalGravity.OnGravitySelectionModeEntered -= OnSelectionModeEntered;
            OmnidirectionalGravity.OnGravitySelectionModeExited -= OnSelectionModeExited;

            if (lineMaterial != null)
            {
                Destroy(lineMaterial);
            }
        }

        private void CreateLineMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                lineMaterial = new Material(shader);
            }
        }

        private void SetupLineRenderers()
        {
            if (gravityArrow == null)
            {
                gravityArrow = CreateLineRenderer("GravityArrow", normalGravityColor, 0.08f, 0.02f);
            }

            if (aimReticleLine == null)
            {
                aimReticleLine = CreateLineRenderer("AimReticle", validAimColor, 0.06f, 0.06f);
            }

            if (radialGuide == null)
            {
                radialGuide = CreateLineRenderer("RadialGuide", radialColor, 0.03f, 0.03f);
                radialGuide.loop = true;
            }
        }

        private LineRenderer CreateLineRenderer(string objName, Color color, float startWidth, float endWidth)
        {
            GameObject obj = new GameObject(objName);
            obj.transform.SetParent(transform);
            
            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = startWidth;
            lr.endWidth = endWidth;
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            
            return lr;
        }

        private void Update()
        {
            UpdateGravityIndicator();
            UpdateAimReticle();
            UpdateFlipEffect();
        }

        private void UpdateGravityIndicator()
        {
            if (!showGravityIndicator || gravityArrow == null || playerTransform == null) return;

            Vector3 gravityDir = Vector3.down;
            bool isFlipping = false;
            
            if (OmnidirectionalGravity.Instance != null)
            {
                gravityDir = OmnidirectionalGravity.Instance.CurrentGravityDirection;
                isFlipping = OmnidirectionalGravity.Instance.IsFlipping;
            }

            Vector3 start = playerTransform.position;
            Vector3 end = start + gravityDir * arrowLength;
            
            gravityArrow.SetPosition(0, start);
            gravityArrow.SetPosition(1, end);
            
            Color arrowColor = isFlipping ? flippingGravityColor : normalGravityColor;
            gravityArrow.startColor = arrowColor;
            gravityArrow.endColor = arrowColor * 0.5f;
        }

        private void UpdateAimReticle()
        {
            if (!showAimReticle || aimReticleLine == null || playerTransform == null) return;

            bool inSelectionMode = OmnidirectionalGravity.Instance != null && 
                                   OmnidirectionalGravity.Instance.IsInSelectionMode;

            if (!inSelectionMode)
            {
                SetAimReticleVisible(false);
                return;
            }

            SetAimReticleVisible(true);

            Vector3 aimedDir = OmnidirectionalGravity.Instance.AimedDirection;
            bool isValid = OmnidirectionalGravity.Instance.IsValidGravityDirection(aimedDir);
            
            Vector3 start = playerTransform.position;
            Vector3 end = start + aimedDir * reticleLength;
            
            aimReticleLine.SetPosition(0, start);
            aimReticleLine.SetPosition(1, end);
            
            Color reticleColor = isValid ? validAimColor : invalidAimColor;
            aimReticleLine.startColor = reticleColor;
            aimReticleLine.endColor = reticleColor;

            UpdateRadialGuide();
        }

        private void UpdateRadialGuide()
        {
            if (radialGuide == null || playerTransform == null) return;

            SetRadialGuideVisible(true);

            Vector3 center = playerTransform.position;
            Vector3 normal = Vector3.forward;
            
            if (OmnidirectionalGravity.Instance != null)
            {
                normal = -OmnidirectionalGravity.Instance.CurrentGravityDirection;
            }

            radialGuide.positionCount = reticleSegments + 1;
            
            Vector3 right = Vector3.Cross(normal, Vector3.up);
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.Cross(normal, Vector3.forward);
            }
            right.Normalize();

            for (int i = 0; i <= reticleSegments; i++)
            {
                float angle = (i / (float)reticleSegments) * 360f;
                Vector3 point = center + Quaternion.AngleAxis(angle, normal) * (right * radialRadius);
                radialGuide.SetPosition(i, point);
            }
        }

        private void SetAimReticleVisible(bool visible)
        {
            if (aimReticleLine != null)
            {
                aimReticleLine.enabled = visible;
            }
        }

        private void SetRadialGuideVisible(bool visible)
        {
            if (radialGuide != null)
            {
                radialGuide.enabled = visible;
            }
        }

        private void UpdateFlipEffect()
        {
            if (!enableScreenEffects || flipEffectOverlay == null) return;

            if (isShowingFlipEffect)
            {
                flipEffectTimer -= Time.deltaTime;
                
                float normalizedTime = 1f - (flipEffectTimer / flipEffectDuration);
                float alpha = Mathf.Sin(normalizedTime * Mathf.PI) * flipEffectColor.a;
                
                flipEffectOverlay.alpha = alpha;
                
                if (flipEffectTimer <= 0)
                {
                    isShowingFlipEffect = false;
                    flipEffectOverlay.alpha = 0;
                }
            }
        }

        private void OnFlipStarted(Vector3 from, Vector3 to)
        {
            if (enableScreenEffects && flipEffectOverlay != null)
            {
                isShowingFlipEffect = true;
                flipEffectTimer = flipEffectDuration;
            }
        }

        private void OnFlipCompleted(Vector3 newDirection)
        {
        }

        private void OnSelectionModeEntered()
        {
            SetRadialGuideVisible(true);
        }

        private void OnSelectionModeExited()
        {
            SetAimReticleVisible(false);
            SetRadialGuideVisible(false);
        }

        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }

        /// <summary>
        /// Create the screen effect overlay if using Unity UI.
        /// Call this during scene setup if flipEffectOverlay is null.
        /// </summary>
        public void SetupUIOverlay(Canvas canvas)
        {
            if (canvas == null) return;

            GameObject overlayObj = new GameObject("FlipEffectOverlay");
            overlayObj.transform.SetParent(canvas.transform, false);
            
            UnityEngine.UI.Image image = overlayObj.AddComponent<UnityEngine.UI.Image>();
            image.color = flipEffectColor;
            
            RectTransform rect = overlayObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            flipEffectOverlay = overlayObj.AddComponent<CanvasGroup>();
            flipEffectOverlay.alpha = 0;
            flipEffectOverlay.blocksRaycasts = false;
            flipEffectOverlay.interactable = false;
        }
    }
}