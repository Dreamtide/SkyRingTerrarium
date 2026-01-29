using UnityEngine;
// PlayerController is in the SkyRingTerrarium namespace (no sub-namespace needed)

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
            if (SaveSystem.Instance != null && SaveSystem.Instance.HasSavedGame)
            {
                // SaveSystem would provide last position
            }
            
            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            playerInstance = playerObj.GetComponent<PlayerController>();
            
            if (playerInstance == null)
            {
                Debug.LogWarning("[MainGame] Player prefab missing PlayerController component");
            }
            
            Debug.Log($"[MainGame] Player spawned at {spawnPos}");
        }
        
        private void SetupCamera()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            if (mainCamera != null && playerInstance != null)
            {
                // Setup camera follow if using GravityCameraController
                var cameraController = mainCamera.GetComponent<GravityCameraController>();
                if (cameraController != null)
                {
                    cameraController.SetTarget(playerInstance.transform);
                }
            }
        }
        
        private void InitializeSceneSystems()
        {
            // Initialize any scene-specific systems here
            Debug.Log("[MainGame] Scene systems initialized");
        }
        
        private void OnDestroy()
        {
            // Cleanup if needed
        }
    }
}