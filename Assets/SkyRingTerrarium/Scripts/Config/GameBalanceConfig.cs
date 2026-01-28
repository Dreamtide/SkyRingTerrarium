using UnityEngine;

namespace SkyRingTerrarium
{
    /// <summary>
    /// Master game balance configuration
    /// </summary>
    [CreateAssetMenu(fileName = "GameBalance", menuName = "SkyRingTerrarium/Config/Game Balance")]
    public class GameBalanceConfig : ScriptableObject
    {
        [Header("Player Movement")]
        [Tooltip("Base walk speed in units per second")]
        public float baseWalkSpeed = 5f;
        
        [Tooltip("Run speed multiplier")]
        public float runSpeedMultiplier = 1.6f;
        
        [Tooltip("Air control factor (0-1)")]
        [Range(0f, 1f)]
        public float airControl = 0.6f;
        
        [Header("Jump & Thrust")]
        [Tooltip("Jump force in units")]
        public float jumpForce = 12f;
        
        [Tooltip("Thrust force applied per second")]
        public float thrustForce = 15f;
        
        [Tooltip("Maximum thrust duration in seconds")]
        public float maxThrustDuration = 2f;
        
        [Tooltip("Thrust cooldown after releasing")]
        public float thrustCooldown = 0.5f;
        
        [Header("Thrust Fuel")]
        [Tooltip("Maximum fuel capacity")]
        public float maxFuel = 100f;
        
        [Tooltip("Fuel consumed per second while thrusting")]
        public float fuelConsumptionRate = 30f;
        
        [Tooltip("Fuel recharged per second while grounded")]
        public float fuelRechargeRate = 20f;
        
        [Header("Gravity Flip")]
        [Tooltip("Cooldown between flips in seconds")]
        public float flipCooldown = 1f;
        
        [Tooltip("Currency cost per flip")]
        public float flipCost = 10f;
        
        [Tooltip("Flip animation duration")]
        public float flipDuration = 0.3f;
        
        [Header("Speed Control")]
        [Tooltip("Minimum speed multiplier")]
        public float minSpeedMultiplier = 0.25f;
        
        [Tooltip("Maximum speed multiplier")]
        public float maxSpeedMultiplier = 3f;
        
        [Tooltip("Speed control step size")]
        public float speedStep = 0.25f;
        
        [Header("Timing")]
        [Tooltip("Coyote time for jump grace period")]
        public float coyoteTime = 0.15f;
        
        [Tooltip("Jump buffer time")]
        public float jumpBufferTime = 0.1f;
    }
}