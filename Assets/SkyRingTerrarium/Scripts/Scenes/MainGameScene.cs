using UnityEngine;

namespace SkyRingTerrarium
{
    /// <summary>
    /// Main game scene controller - spawns player, initializes scene-specific systems
    /// </summary>
    public class MainGameScene : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private GameObject playerPrefab;
        
        [Header("Scene References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform worldBounds;
        
        private PlayerController playerInstance;
        
        private void Start()
        {
            SpawnPlayer();
            SetupCamera();
            InitializeSceneSystems();
        }
        
        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[MainGame] No player prefab assigned!");
                return;
            }
            
            Vector3 spawnPos = playerSpawnPoint != null 
                ? playerSpawnPoint.position 
                : Vector3.zero;
            
            // Check for saved position
            if (PlayerPrefs.HasKey("SavedPlayerX"))
            {
                spawnPos.x = PlayerPrefs.GetFloat("SavedPlayerX");
                spawnPos.y = PlayerPrefs.GetFloat("SavedPlayerY");
                PlayerPrefs.DeleteKey("SavedPlayerX");
                PlayerPrefs.DeleteKey("SavedPlayerY");
            }
            
            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            playerInstance = playerObj.GetComponent<PlayerController>();
            
            // Apply saved gravity state
            if (PlayerPrefs.HasKey("SavedGravityInverted"))
            {
                bool inverted = PlayerPrefs.GetInt("SavedGravityInverted") == 1;
                if (inverted)
                    playerInstance.ForceFlipGravity();
                PlayerPrefs.DeleteKey("SavedGravityInverted");
            }
            
            Debug.Log($"[MainGame] Player spawned at {spawnPos}");
        }
        
        private void SetupCamera()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
                
            CameraFollow camFollow = mainCamera?.GetComponent<CameraFollow>();
            if (camFollow != null && playerInstance != null)
            {
                camFollow.SetTarget(playerInstance.transform);
            }
        }
        
        private void InitializeSceneSystems()
        {
            // Scene-specific initialization
        }
    }
}