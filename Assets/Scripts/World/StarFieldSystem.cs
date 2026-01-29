using UnityEngine;
using System.Collections.Generic;

namespace SkyRingTerrarium.World
{
    /// <summary>
    /// Manages the night sky star field that appears at night and fades at dawn.
    /// </summary>
    public class StarFieldSystem : MonoBehaviour
    {
        public static StarFieldSystem Instance { get; private set; }

        [Header("Star Configuration")]
        [SerializeField] private int starCount = 200;
        [SerializeField] private float starFieldRadius = 50f;
        [SerializeField] private float minStarSize = 0.02f;
        [SerializeField] private float maxStarSize = 0.08f;
        [SerializeField] private float twinkleSpeed = 2f;
        [SerializeField] private float twinkleIntensity = 0.3f;

        [Header("Visibility")]
        [SerializeField] private float fadeInStart = 0.8f;
        [SerializeField] private float fullVisibilityStart = 0.9f;
        [SerializeField] private float fadeOutStart = 0.15f;
        [SerializeField] private float fadeOutEnd = 0.25f;

        [Header("Aurora Settings")]
        [SerializeField] private bool enableAurora = true;
        [SerializeField] private float auroraChance = 0.1f;
        [SerializeField] private Color[] auroraColors;
        [SerializeField] private float auroraWaveSpeed = 0.5f;
        [SerializeField] private float auroraIntensity = 0.6f;

        [Header("References")]
        [SerializeField] private Material starMaterial;
        [SerializeField] private SpriteRenderer auroraRenderer;

        private List<StarData> stars = new List<StarData>();
        private MaterialPropertyBlock propertyBlock;
        private float currentVisibility;
        private bool auroraActive;
        private float auroraTimer;

        private struct StarData
        {
            public Vector3 localPosition;
            public float size;
            public float twinkleOffset;
            public float brightness;
            public SpriteRenderer renderer;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            propertyBlock = new MaterialPropertyBlock();
            GenerateStars();
        }

        private void Start()
        {
            WorldTimeManager.OnTimeOfDayChanged += OnTimeChanged;
            WorldTimeManager.OnTimeOfDayPhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            WorldTimeManager.OnTimeOfDayChanged -= OnTimeChanged;
            WorldTimeManager.OnTimeOfDayPhaseChanged -= OnPhaseChanged;
        }

        private void Update()
        {
            UpdateStarVisibility();
            UpdateTwinkle();
            UpdateAurora();
        }

        private void GenerateStars()
        {
            for (int i = 0; i < starCount; i++)
            {
                Vector2 randomPos = Random.insideUnitCircle * starFieldRadius;
                float size = Random.Range(minStarSize, maxStarSize);
                
                GameObject starObj = new GameObject($"Star_{i}");
                starObj.transform.SetParent(transform);
                starObj.transform.localPosition = new Vector3(randomPos.x, randomPos.y, 0);
                
                SpriteRenderer sr = starObj.AddComponent<SpriteRenderer>();
                sr.sprite = CreateStarSprite();
                sr.sortingOrder = -100;
                sr.color = GetStarColor();
                sr.transform.localScale = Vector3.one * size;

                StarData star = new StarData
                {
                    localPosition = starObj.transform.localPosition,
                    size = size,
                    twinkleOffset = Random.value * Mathf.PI * 2f,
                    brightness = Random.Range(0.6f, 1f),
                    renderer = sr
                };
                stars.Add(star);
            }
        }

        private Sprite CreateStarSprite()
        {
            Texture2D tex = new Texture2D(8, 8);
            Color[] pixels = new Color[64];
            
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f));
                    float alpha = Mathf.Clamp01(1f - dist / 4f);
                    alpha = alpha * alpha;
                    pixels[y * 8 + x] = new Color(1, 1, 1, alpha);
                }
            }
            
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);
        }

        private Color GetStarColor()
        {
            float temp = Random.value;
            if (temp < 0.6f) return new Color(1f, 1f, 1f);
            if (temp < 0.8f) return new Color(1f, 0.95f, 0.8f);
            if (temp < 0.95f) return new Color(0.8f, 0.9f, 1f);
            return new Color(1f, 0.7f, 0.5f);
        }

        private void OnTimeChanged(float timeNormalized)
        {
            CalculateVisibility(timeNormalized);
        }

        private void OnPhaseChanged(WorldTimeManager.TimeOfDay phase)
        {
            if (phase == WorldTimeManager.TimeOfDay.Night && enableAurora)
            {
                if (Random.value < auroraChance)
                {
                    TriggerAurora();
                }
            }
        }

        private void CalculateVisibility(float time)
        {
            if (time >= fadeInStart && time < fullVisibilityStart)
            {
                currentVisibility = (time - fadeInStart) / (fullVisibilityStart - fadeInStart);
            }
            else if (time >= fullVisibilityStart || time < fadeOutStart)
            {
                currentVisibility = 1f;
            }
            else if (time >= fadeOutStart && time < fadeOutEnd)
            {
                currentVisibility = 1f - (time - fadeOutStart) / (fadeOutEnd - fadeOutStart);
            }
            else
            {
                currentVisibility = 0f;
            }

            if (WeatherSystem.Instance != null)
            {
                currentVisibility *= WeatherSystem.Instance.Visibility;
            }
        }

        private void UpdateStarVisibility()
        {
            foreach (var star in stars)
            {
                if (star.renderer != null)
                {
                    Color c = star.renderer.color;
                    c.a = currentVisibility * star.brightness;
                    star.renderer.color = c;
                }
            }
        }

        private void UpdateTwinkle()
        {
            if (currentVisibility <= 0) return;

            for (int i = 0; i < stars.Count; i++)
            {
                var star = stars[i];
                if (star.renderer == null) continue;

                float twinkle = Mathf.Sin(Time.time * twinkleSpeed + star.twinkleOffset);
                float brightness = star.brightness + twinkle * twinkleIntensity;
                brightness = Mathf.Clamp01(brightness);

                Color c = star.renderer.color;
                c.a = currentVisibility * brightness;
                star.renderer.color = c;
            }
        }

        private void UpdateAurora()
        {
            if (!auroraActive || auroraRenderer == null) return;

            auroraTimer += Time.deltaTime * auroraWaveSpeed;
            
            float wave = Mathf.Sin(auroraTimer) * 0.5f + 0.5f;
            int colorIndex = Mathf.FloorToInt(auroraTimer % auroraColors.Length);
            int nextIndex = (colorIndex + 1) % auroraColors.Length;
            float t = (auroraTimer % 1f);
            
            Color auroraColor = Color.Lerp(auroraColors[colorIndex], auroraColors[nextIndex], t);
            auroraColor.a = wave * auroraIntensity * currentVisibility;
            auroraRenderer.color = auroraColor;
        }

        public void TriggerAurora()
        {
            auroraActive = true;
            auroraTimer = 0f;
        }

        public void StopAurora()
        {
            auroraActive = false;
            if (auroraRenderer != null)
            {
                Color c = auroraRenderer.color;
                c.a = 0;
                auroraRenderer.color = c;
            }
        }

        public float StarVisibility => currentVisibility;
        public bool IsAuroraActive => auroraActive;
    }
}