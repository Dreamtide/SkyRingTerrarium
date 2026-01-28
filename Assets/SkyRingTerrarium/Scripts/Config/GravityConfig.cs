using UnityEngine;

namespace SkyRingTerrarium
{
    /// <summary>
    /// Gravity system configuration
    /// </summary>
    [CreateAssetMenu(fileName = "Gravity", menuName = "SkyRingTerrarium/Config/Gravity")]
    public class GravityConfig : ScriptableObject
    {
        [Header("Base Gravity")]
        [Tooltip("Base gravity strength multiplier")]
        public float baseGravityStrength = 1f;
        
        [Tooltip("Minimum gravity strength")]
        public float minGravityStrength = 0.1f;
        
        [Tooltip("Maximum gravity strength")]
        public float maxGravityStrength = 3f;
        
        [Header("Gravity Zones")]
        [Tooltip("Enable gravity zone system")]
        public bool enableGravityZones = true;
        
        [Tooltip("Float band gravity multiplier (0 = zero-g)")]
        public float floatBandGravity = 0f;
        
        [Tooltip("Low gravity zone multiplier")]
        public float lowGravityZoneMultiplier = 0.3f;
        
        [Tooltip("High gravity zone multiplier")]
        public float highGravityZoneMultiplier = 2f;
        
        [Header("Visual Feedback")]
        [Tooltip("Color tint for normal gravity")]
        public Color normalGravityTint = new Color(1, 1, 1, 0);
        
        [Tooltip("Color tint for inverted gravity")]
        public Color invertedGravityTint = new Color(0.8f, 0.9f, 1f, 0.15f);
        
        [Tooltip("Color tint for low gravity zones")]
        public Color lowGravityTint = new Color(0.9f, 1f, 0.9f, 0.1f);
        
        [Header("Particle Settings")]
        [Tooltip("Reverse particle direction when gravity inverts")]
        public bool reverseParticlesOnFlip = true;
        
        [Tooltip("Particle gravity multiplier")]
        public float particleGravityMultiplier = 1f;
    }
}