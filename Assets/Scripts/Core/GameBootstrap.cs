using System.Collections;
using UnityEngine;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Main game bootstrap that initializes and wires up all game systems in the correct order.
    /// This is the entry point for the game.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("System Initialization Order")]
        [SerializeField] private bool initializeOnAwake = true;

        [Header("Core System Prefabs")]
        [SerializeField] private GameObject saveSystemPrefab;
        [SerializeField] private GameObject upgradeManagerPrefab;
        [SerializeField] private GameObject gameSettingsPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        [SerializeField] private GameObject inputManagerPrefab;
        [SerializeField] private GameObject performanceProfilerPrefab;

        [Header("World System Prefabs")]
        [SerializeField] private GameObject worldTimeManagerPrefab;
        [SerializeField] private GameObject weatherSystemPrefab;
        [SerializeField] private GameObject ecosystemManagerPrefab;
        [SerializeField] private GameObject resourceManagerPrefab;
        [SerializeField] private GameObject worldEventManagerPrefab;
        [SerializeField] private GameObject offlineProgressPrefab;

        [Header("Player & Camera")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject cameraPrefab;
        [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;

        [Header("Environment")]
        [SerializeField] private GameObject parallaxBackgroundPrefab;
        [SerializeField] private GameObject starFieldPrefab;
        [SerializeField] private GameObject terrainPlaceholderPrefab;
        [SerializeField] private GameObject floatBandSystemPrefab;

        [Header("UI")]
        [SerializeField] private GameObject uiCanvasPrefab;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;

        // Spawned references
        private GameObject player;
        private GameObject mainCamera;
        private GameObject uiCanvas;

        public GameObject Player => player;
        public GameObject MainCamera => mainCamera;

        private void Awake()
        {
            if (initializeOnAwake)
            {
                StartCoroutine(InitializeGame());
            }
        }

        public IEnumerator InitializeGame()
        {
            Log("=== Sky Ring Terrarium Bootstrap ===");
            Log("Initializing game systems...");

            // Phase 1: Core singleton systems
            yield return InitializeCoreSystemsPhase();

            // Phase 2: World systems
            yield return InitializeWorldSystemsPhase();

            // Phase 3: Environment
            yield return InitializeEnvironmentPhase();

            // Phase 4: Player & Camera
            yield return InitializePlayerPhase();

            // Phase 5: UI
            yield return InitializeUIPhase();

            // Phase 6: Load save data
            yield return LoadGameDataPhase();

            // Phase 7: Final initialization
            yield return FinalizeInitialization();

            Log("=== Bootstrap Complete ===");
        }

        #region Initialization Phases

        private IEnumerator InitializeCoreSystemsPhase()
        {
            Log("Phase 1: Core Systems");

            // Save System - must be first
            SpawnSingleton(saveSystemPrefab, "SaveSystem");
            yield return null;

            // Game Settings
            SpawnSingleton(gameSettingsPrefab, "GameSettings");
            yield return null;

            // Upgrade Manager
            SpawnSingleton(upgradeManagerPrefab, "UpgradeManager");
            yield return null;

            // Audio Manager
            SpawnSingleton(audioManagerPrefab, "AudioManager");
            yield return null;

            // Input Manager
            SpawnSingleton(inputManagerPrefab, "InputManager");
            yield return null;

            // Performance Profiler
            SpawnSingleton(performanceProfilerPrefab, "PerformanceProfiler");
            yield return null;

            Log("  Core systems initialized.");
        }

        private IEnumerator InitializeWorldSystemsPhase()
        {
            Log("Phase 2: World Systems");

            // World Time Manager
            SpawnSingleton(worldTimeManagerPrefab, "WorldTimeManager");
            yield return null;

            // Weather System
            SpawnSingleton(weatherSystemPrefab, "WeatherSystem");
            yield return null;

            // Resource Manager
            SpawnSingleton(resourceManagerPrefab, "ResourceManager");
            yield return null;

            // Ecosystem Manager
            SpawnSingleton(ecosystemManagerPrefab, "EcosystemManager");
            yield return null;

            // World Event Manager
            SpawnSingleton(worldEventManagerPrefab, "WorldEventManager");
            yield return null;

            // Offline Progress Manager
            SpawnSingleton(offlineProgressPrefab, "OfflineProgressManager");
            yield return null;

            Log("  World systems initialized.");
        }

        private IEnumerator InitializeEnvironmentPhase()
        {
            Log("Phase 3: Environment");

            // Star field (background)
            if (starFieldPrefab != null)
            {
                Instantiate(starFieldPrefab).name = "StarField";
            }
            yield return null;

            // Parallax background
            if (parallaxBackgroundPrefab != null)
            {
                Instantiate(parallaxBackgroundPrefab).name = "ParallaxBackground";
            }
            yield return null;

            // Float band system
            if (floatBandSystemPrefab != null)
            {
                Instantiate(floatBandSystemPrefab).name = "FloatBandSystem";
            }
            yield return null;

            // Terrain placeholder
            if (terrainPlaceholderPrefab != null)
            {
                Instantiate(terrainPlaceholderPrefab).name = "Terrain";
            }
            yield return null;

            Log("  Environment initialized.");
        }

        private IEnumerator InitializePlayerPhase()
        {
            Log("Phase 4: Player & Camera");

            // Spawn player
            if (playerPrefab != null)
            {
                player = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
                player.name = "Player";
            }
            else
            {
                // Create basic player if no prefab
                player = CreateDefaultPlayer();
            }
            yield return null;

            // Spawn camera
            if (cameraPrefab != null)
            {
                mainCamera = Instantiate(cameraPrefab);
                mainCamera.name = "MainCamera";
            }
            else
            {
                mainCamera = CreateDefaultCamera();
            }

            // Link camera to player
            GravityCameraController cameraController = mainCamera.GetComponent<GravityCameraController>();
            if (cameraController != null && player != null)
            {
                cameraController.SetTarget(player.transform);
            }
            yield return null;

            Log("  Player & Camera initialized.");
        }

        private IEnumerator InitializeUIPhase()
        {
            Log("Phase 5: UI");

            if (uiCanvasPrefab != null)
            {
                uiCanvas = Instantiate(uiCanvasPrefab);
                uiCanvas.name = "UICanvas";
            }
            else
            {
                uiCanvas = CreateDefaultUICanvas();
            }
            yield return null;

            Log("  UI initialized.");
        }

        private IEnumerator LoadGameDataPhase()
        {
            Log("Phase 6: Loading Save Data");

            SaveSystem saveSystem = SaveSystem.Instance;
            if (saveSystem != null && saveSystem.HasSaveFile())
            {
                saveSystem.LoadGame();
                Log("  Save data loaded.");
            }
            else
            {
                Log("  No save file found, starting fresh.");
            }

            yield return null;
        }

        private IEnumerator FinalizeInitialization()
        {
            Log("Phase 7: Finalization");

            // Calculate offline progress
            OfflineProgressionManager offlineMgr = FindFirstObjectByType<OfflineProgressionManager>();
            offlineMgr?.CalculateOfflineProgress();
            yield return null;

            // Enable input
            InputManager.Instance?.EnablePlayerInput();
            yield return null;

            // Start ambient audio
            AudioManager.Instance?.SetAmbientLoop("ambient_day");
            yield return null;

            Log("  Finalization complete.");
        }

        #endregion

        #region Helper Methods

        private void SpawnSingleton(GameObject prefab, string fallbackName)
        {
            if (prefab != null)
            {
                Instantiate(prefab).name = fallbackName;
            }
            else
            {
                // Create empty game object with component if no prefab
                CreateFallbackSingleton(fallbackName);
            }
        }

        private void CreateFallbackSingleton(string systemName)
        {
            GameObject obj = new GameObject(systemName);

            switch (systemName)
            {
                case "SaveSystem":
                    obj.AddComponent<SaveSystem>();
                    break;
                case "UpgradeManager":
                    obj.AddComponent<UpgradeManager>();
                    break;
                case "GameSettings":
                    obj.AddComponent<GameSettingsManager>();
                    break;
                case "AudioManager":
                    obj.AddComponent<AudioManager>();
                    break;
                case "InputManager":
                    obj.AddComponent<InputManager>();
                    break;
                case "PerformanceProfiler":
                    obj.AddComponent<PerformanceProfiler>();
                    break;
                case "WorldTimeManager":
                    obj.AddComponent<WorldTimeManager>();
                    break;
                case "WeatherSystem":
                    obj.AddComponent<WeatherSystem>();
                    break;
                case "EcosystemManager":
                    obj.AddComponent<EcosystemManager>();
                    break;
                case "ResourceManager":
                    obj.AddComponent<ResourceManager>();
                    break;
                case "WorldEventManager":
                    obj.AddComponent<WorldEventManager>();
                    break;
                case "OfflineProgressManager":
                    obj.AddComponent<OfflineProgressionManager>();
                    break;
            }
        }

        private GameObject CreateDefaultPlayer()
        {
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = playerSpawnPosition;

            // Add required components
            playerObj.AddComponent<SpriteRenderer>();
            Rigidbody2D rb = playerObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; // Using custom gravity
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D col = playerObj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            playerObj.AddComponent<PlayerController>();
            playerObj.AddComponent<GravityAffectedBody>();

            return playerObj;
        }

        private GameObject CreateDefaultCamera()
        {
            GameObject camObj = new GameObject("MainCamera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);

            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<GravityCameraController>();
            camObj.tag = "MainCamera";

            return camObj;
        }

        private GameObject CreateDefaultUICanvas()
        {
            // Create canvas
            GameObject canvasObj = new GameObject("UICanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Add EventSystem if not present
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Add UI components
            GameObject hudObj = new GameObject("HUD");
            hudObj.transform.SetParent(canvasObj.transform, false);
            hudObj.AddComponent<GameHUD>();

            GameObject upgradeMenuObj = new GameObject("UpgradeMenu");
            upgradeMenuObj.transform.SetParent(canvasObj.transform, false);
            upgradeMenuObj.AddComponent<UpgradeMenu>();

            GameObject pauseMenuObj = new GameObject("PauseMenu");
            pauseMenuObj.transform.SetParent(canvasObj.transform, false);
            pauseMenuObj.AddComponent<PauseMenu>();

            GameObject settingsMenuObj = new GameObject("SettingsMenu");
            settingsMenuObj.transform.SetParent(canvasObj.transform, false);
            settingsMenuObj.AddComponent<SettingsMenu>();

            GameObject offlinePopupObj = new GameObject("OfflineProgressPopup");
            offlinePopupObj.transform.SetParent(canvasObj.transform, false);
            offlinePopupObj.AddComponent<OfflineProgressPopup>();

            GameObject tooltipObj = new GameObject("TooltipSystem");
            tooltipObj.transform.SetParent(canvasObj.transform, false);
            tooltipObj.AddComponent<TooltipSystem>();

            GameObject tutorialObj = new GameObject("TutorialSystem");
            tutorialObj.transform.SetParent(canvasObj.transform, false);
            tutorialObj.AddComponent<TutorialSystem>();

            return canvasObj;
        }

        private void Log(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[Bootstrap] {message}");
            }
        }

        #endregion
    }
}
