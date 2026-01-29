#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SkyRingTerrarium; // Added for GameBalanceConfig, GravityConfig, EconomyConfig, PlayerController

namespace SkyRingTerrarium.Editor
{
    /// <summary>
    /// Editor utilities for setting up Sky Ring Terrarium project
    /// </summary>
    public static class SkyRingSetup
    {
        [MenuItem("SkyRing/Setup/Create Folder Structure")]
        public static void CreateFolderStructure()
        {
            string[] folders = new string[]
            {
                "Assets/SkyRingTerrarium",
                "Assets/SkyRingTerrarium/Scripts",
                "Assets/SkyRingTerrarium/Scripts/Core",
                "Assets/SkyRingTerrarium/Scripts/Player",
                "Assets/SkyRingTerrarium/Scripts/UI",
                "Assets/SkyRingTerrarium/Scripts/Managers",
                "Assets/SkyRingTerrarium/Scripts/Utilities",
                "Assets/SkyRingTerrarium/Prefabs",
                "Assets/SkyRingTerrarium/Prefabs/Player",
                "Assets/SkyRingTerrarium/Prefabs/UI",
                "Assets/SkyRingTerrarium/Prefabs/Environment",
                "Assets/SkyRingTerrarium/Prefabs/Pickups",
                "Assets/SkyRingTerrarium/Prefabs/Effects",
                "Assets/SkyRingTerrarium/Scenes",
                "Assets/SkyRingTerrarium/Art",
                "Assets/SkyRingTerrarium/Art/Sprites",
                "Assets/SkyRingTerrarium/Art/Particles",
                "Assets/SkyRingTerrarium/Art/UI",
                "Assets/SkyRingTerrarium/Art/Backgrounds",
                "Assets/SkyRingTerrarium/Audio",
                "Assets/SkyRingTerrarium/Audio/Music",
                "Assets/SkyRingTerrarium/Audio/SFX",
                "Assets/SkyRingTerrarium/Config",
                "Assets/SkyRingTerrarium/Materials",
                "Assets/SkyRingTerrarium/Animations"
            };
            
            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string[] parts = folder.Split('/');
                    string currentPath = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string parentPath = currentPath;
                        currentPath = currentPath + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(currentPath))
                        {
                            AssetDatabase.CreateFolder(parentPath, parts[i]);
                        }
                    }
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log("[SkyRing Setup] Folder structure created!");
        }
        
        [MenuItem("SkyRing/Setup/Create Default Configs")]
        public static void CreateDefaultConfigs()
        {
            // Create GameBalanceConfig
            var balance = ScriptableObject.CreateInstance<GameBalanceConfig>();
            AssetDatabase.CreateAsset(balance, "Assets/SkyRingTerrarium/Config/GameBalance.asset");
            
            // Create GravityConfig
            var gravity = ScriptableObject.CreateInstance<GravityConfig>();
            AssetDatabase.CreateAsset(gravity, "Assets/SkyRingTerrarium/Config/Gravity.asset");
            
            // Create EconomyConfig
            var economy = ScriptableObject.CreateInstance<EconomyConfig>();
            AssetDatabase.CreateAsset(economy, "Assets/SkyRingTerrarium/Config/Economy.asset");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SkyRing Setup] Default configs created!");
        }
        
        [MenuItem("SkyRing/Setup/Create Player Prefab")]
        public static void CreatePlayerPrefab()
        {
            // Create player GameObject
            GameObject player = new GameObject("Player");
            
            // Add components
            player.AddComponent<SpriteRenderer>();
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<BoxCollider2D>();
            player.AddComponent<PlayerController>();
            
            // Create ground check
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform);
            groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);
            
            // Setup Rigidbody2D
            var rb = player.GetComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            
            // Setup tag
            player.tag = "Player";
            
            // Save prefab
            string prefabPath = "Assets/SkyRingTerrarium/Prefabs/Player/Player.prefab";
            PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
            Object.DestroyImmediate(player);
            
            Debug.Log("[SkyRing Setup] Player prefab created!");
        }
        
        [MenuItem("SkyRing/Setup/Setup All")]
        public static void SetupAll()
        {
            CreateFolderStructure();
            CreateDefaultConfigs();
            CreatePlayerPrefab();
            Debug.Log("[SkyRing Setup] Complete setup finished!");
        }
    }
}
#endif