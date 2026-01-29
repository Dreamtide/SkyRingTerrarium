using UnityEngine;

namespace SkyRingTerrarium.World
{
    /// <summary>
    /// Bootstraps all living world systems in the correct order.
    /// Attach to a persistent GameObject in the scene.
    /// </summary>
    public class LivingWorldBootstrap : MonoBehaviour
    {
        [Header("System Prefabs")]
        [SerializeField] private GameObject worldTimeManagerPrefab;
        [SerializeField] private GameObject weatherSystemPrefab;
        [SerializeField] private GameObject starFieldSystemPrefab;
        [SerializeField] private GameObject worldEventManagerPrefab;
        [SerializeField] private GameObject ecosystemManagerPrefab;
        [SerializeField] private GameObject resourceManagerPrefab;
        [SerializeField] private GameObject offlineProgressionManagerPrefab;

        [Header("Auto-Create if Missing")]
        [SerializeField] private bool autoCreateSystems = true;

        private void Awake()
        {
            InitializeSystems();
        }

        private void InitializeSystems()
        {
            // Order matters - systems that others depend on should be created first
            
            // 1. Time System (foundation for all time-based systems)
            EnsureSystem<WorldTimeManager>(worldTimeManagerPrefab, "WorldTimeManager");
            
            // 2. Weather System (depends on time for season-based weather)
            EnsureSystem<WeatherSystem>(weatherSystemPrefab, "WeatherSystem");
            
            // 3. Visual Systems
            EnsureSystem<StarFieldSystem>(starFieldSystemPrefab, "StarFieldSystem");
            
            // 4. Ecosystem Systems
            EnsureSystem<Ecosystem.ResourceManager>(resourceManagerPrefab, "ResourceManager");
            EnsureSystem<Ecosystem.EcosystemManager>(ecosystemManagerPrefab, "EcosystemManager");
            
            // 5. Event System (depends on time, weather, ecosystem)
            EnsureSystem<WorldEventManager>(worldEventManagerPrefab, "WorldEventManager");
            
            // 6. Offline Progression (depends on all above for state saving)
            EnsureSystem<OfflineProgressionManager>(offlineProgressionManagerPrefab, "OfflineProgressionManager");
        }

        private void EnsureSystem<T>(GameObject prefab, string systemName) where T : MonoBehaviour
        {
            if (FindAnyObjectByType<T>() != null) return;

            if (prefab != null)
            {
                Instantiate(prefab);
            }
            else if (autoCreateSystems)
            {
                var go = new GameObject(systemName);
                go.AddComponent<T>();
            }
        }
    }
}