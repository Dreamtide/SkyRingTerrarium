using UnityEngine;

namespace SkyRingTerrarium
{
    public enum PickupSize { Small, Medium, Large }
    
    /// <summary>
    /// Collectible currency pickup
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CurrencyPickup : MonoBehaviour
    {
        [SerializeField] private PickupSize size = PickupSize.Small;
        [SerializeField] private float customValue = 0f;
        [SerializeField] private ParticleSystem collectEffect;
        [SerializeField] private AudioClip collectSound;
        
        [Header("Animation")]
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobAmount = 0.2f;
        [SerializeField] private float rotateSpeed = 90f;
        
        private Vector3 startPosition;
        private float bobOffset;
        
        private void Start()
        {
            startPosition = transform.position;
            bobOffset = Random.Range(0f, Mathf.PI * 2f);
            GetComponent<Collider2D>().isTrigger = true;
        }
        
        private void Update()
        {
            // Bob animation
            float bob = Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;
            transform.position = startPosition + Vector3.up * bob;
            
            // Rotate
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            
            // Get value
            float value = customValue > 0 ? customValue : GetDefaultValue();
            
            // Add currency
            GameBootstrap.Instance?.EconomyManager?.AddCurrency(value);
            
            // Effects
            if (collectEffect != null)
            {
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            }
            
            // Destroy pickup
            Destroy(gameObject);
        }
        
        private float GetDefaultValue()
        {
            EconomyConfig config = GameBootstrap.Instance?.GetEconomyConfig();
            if (config == null) return 5f;
            
            return size switch
            {
                PickupSize.Small => config.smallPickupValue,
                PickupSize.Medium => config.mediumPickupValue,
                PickupSize.Large => config.largePickupValue,
                _ => config.smallPickupValue
            };
        }
    }
}