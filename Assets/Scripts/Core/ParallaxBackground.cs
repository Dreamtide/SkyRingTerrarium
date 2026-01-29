using UnityEngine;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Manages parallax background layers that shift with camera movement.
    /// Supports infinite scrolling, depth-based parallax factors, and ring-aware wrapping.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [System.Serializable]
        public class ParallaxLayerConfig
        {
            public string layerName;
            public SpriteRenderer[] sprites;
            [Range(0f, 1f)]
            public float parallaxFactorX = 0.5f;
            [Range(0f, 1f)]
            public float parallaxFactorY = 0.5f;
            public bool infiniteScrollX = true;
            public bool infiniteScrollY = false;
            public bool followGravityRotation = false;
            public float depth = 0f;
        }

        [Header("Configuration")]
        [SerializeField] private ParallaxLayerConfig[] layers;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private bool autoFindCamera = true;
        
        [Header("Infinite Scroll Settings")]
        [SerializeField] private float spriteWrapThreshold = 0.5f;
        
        [Header("Gravity Response")]
        [SerializeField] private bool rotateWithGravity = true;
        [SerializeField] private float gravityRotationSpeed = 2f;

        private Vector3 lastCameraPosition;
        private Quaternion currentRotation = Quaternion.identity;
        private float[][] spriteWidths;
        private Vector3[][] initialPositions;

        private void Start()
        {
            if (autoFindCamera && cameraTransform == null)
            {
                GravityCameraController camController = FindFirstObjectByType<GravityCameraController>();
                if (camController != null)
                {
                    cameraTransform = camController.transform;
                }
                else if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }
            }

            if (cameraTransform != null)
            {
                lastCameraPosition = cameraTransform.position;
            }

            InitializeLayers();
        }

        private void InitializeLayers()
        {
            if (layers == null) return;

            spriteWidths = new float[layers.Length][];
            initialPositions = new Vector3[layers.Length][];

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].sprites == null) continue;

                spriteWidths[i] = new float[layers[i].sprites.Length];
                initialPositions[i] = new Vector3[layers[i].sprites.Length];

                for (int j = 0; j < layers[i].sprites.Length; j++)
                {
                    SpriteRenderer sr = layers[i].sprites[j];
                    if (sr != null && sr.sprite != null)
                    {
                        spriteWidths[i][j] = sr.bounds.size.x;
                        initialPositions[i][j] = sr.transform.position;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            UpdateGravityRotation();
            UpdateParallax();
            
            lastCameraPosition = cameraTransform.position;
        }

        private void UpdateGravityRotation()
        {
            if (!rotateWithGravity) return;

            Vector3 gravityUp = Vector3.up;
            
            if (OmnidirectionalGravity.Instance != null)
            {
                gravityUp = -OmnidirectionalGravity.Instance.CurrentGravityDirection;
            }
            else
            {
                GravityCameraController camController = cameraTransform.GetComponent<GravityCameraController>();
                if (camController != null)
                {
                    gravityUp = camController.GravityUp;
                }
            }

            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, gravityUp);
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, gravityRotationSpeed * Time.deltaTime);
        }

        private void UpdateParallax()
        {
            Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

            for (int i = 0; i < layers.Length; i++)
            {
                ParallaxLayerConfig layer = layers[i];
                if (layer.sprites == null) continue;

                Vector3 parallaxOffset = new Vector3(
                    cameraDelta.x * layer.parallaxFactorX,
                    cameraDelta.y * layer.parallaxFactorY,
                    0f
                );

                for (int j = 0; j < layer.sprites.Length; j++)
                {
                    SpriteRenderer sr = layer.sprites[j];
                    if (sr == null) continue;

                    sr.transform.position += parallaxOffset;

                    if (layer.followGravityRotation)
                    {
                        sr.transform.rotation = currentRotation;
                    }

                    if (layer.infiniteScrollX)
                    {
                        HandleInfiniteScrollX(sr, i, j);
                    }

                    if (layer.infiniteScrollY)
                    {
                        HandleInfiniteScrollY(sr);
                    }
                }
            }
        }

        private void HandleInfiniteScrollX(SpriteRenderer sr, int layerIndex, int spriteIndex)
        {
            if (spriteWidths == null || spriteWidths[layerIndex] == null) return;
            
            float spriteWidth = spriteWidths[layerIndex][spriteIndex];
            float halfWidth = spriteWidth * spriteWrapThreshold;
            
            float distanceFromCamera = sr.transform.position.x - cameraTransform.position.x;
            
            if (distanceFromCamera > halfWidth)
            {
                sr.transform.position -= new Vector3(spriteWidth * 2f, 0f, 0f);
            }
            else if (distanceFromCamera < -halfWidth)
            {
                sr.transform.position += new Vector3(spriteWidth * 2f, 0f, 0f);
            }
        }

        private void HandleInfiniteScrollY(SpriteRenderer sr)
        {
            float spriteHeight = sr.bounds.size.y;
            float halfHeight = spriteHeight * spriteWrapThreshold;
            
            float distanceFromCamera = sr.transform.position.y - cameraTransform.position.y;
            
            if (distanceFromCamera > halfHeight)
            {
                sr.transform.position -= new Vector3(0f, spriteHeight * 2f, 0f);
            }
            else if (distanceFromCamera < -halfHeight)
            {
                sr.transform.position += new Vector3(0f, spriteHeight * 2f, 0f);
            }
        }

        /// <summary>
        /// Set the camera transform at runtime.
        /// </summary>
        public void SetCamera(Transform cam)
        {
            cameraTransform = cam;
            if (cam != null)
            {
                lastCameraPosition = cam.position;
            }
        }

        /// <summary>
        /// Add a new layer at runtime.
        /// </summary>
        public void AddLayer(ParallaxLayerConfig newLayer)
        {
            System.Array.Resize(ref layers, layers.Length + 1);
            layers[layers.Length - 1] = newLayer;
            InitializeLayers();
        }

        /// <summary>
        /// Reset all layers to their initial positions.
        /// </summary>
        public void ResetLayers()
        {
            if (layers == null || initialPositions == null) return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].sprites == null || initialPositions[i] == null) continue;

                for (int j = 0; j < layers[i].sprites.Length; j++)
                {
                    if (layers[i].sprites[j] != null)
                    {
                        layers[i].sprites[j].transform.position = initialPositions[i][j];
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (layers == null) return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].sprites == null) continue;

                Gizmos.color = new Color(1f - (i / (float)layers.Length), 0.5f, i / (float)layers.Length, 0.3f);
                
                foreach (var sr in layers[i].sprites)
                {
                    if (sr != null)
                    {
                        Gizmos.DrawWireCube(sr.transform.position, sr.bounds.size);
                    }
                }
            }
        }
#endif
    }
}