using UnityEngine;

namespace SkyRingTerrarium
{
    /// <summary>
    /// Economy and currency configuration
    /// </summary>
    [CreateAssetMenu(fileName = "Economy", menuName = "SkyRingTerrarium/Config/Economy")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Starting Values")]
        [Tooltip("Currency player starts with")]
        public float startingCurrency = 100f;
        
        [Header("Earning Rates")]
        [Tooltip("Currency earned per second of airtime")]
        public float airtimeCurrencyRate = 1f;
        
        [Tooltip("Currency bonus for flip combos")]
        public float flipComboBonus = 5f;
        
        [Tooltip("Currency multiplier per combo")]
        public float comboMultiplier = 1.1f;
        
        [Tooltip("Maximum combo multiplier")]
        public float maxComboMultiplier = 5f;
        
        [Header("Costs")]
        [Tooltip("Currency cost per gravity flip")]
        public float flipCost = 10f;
        
        [Tooltip("Cost to refill thrust fuel")]
        public float fuelRefillCost = 25f;
        
        [Header("Pickups")]
        [Tooltip("Small currency pickup value")]
        public float smallPickupValue = 5f;
        
        [Tooltip("Medium currency pickup value")]
        public float mediumPickupValue = 15f;
        
        [Tooltip("Large currency pickup value")]
        public float largePickupValue = 50f;
        
        [Header("Display")]
        [Tooltip("Currency icon character")]
        public string currencySymbol = "◆";
        
        [Tooltip("Number of decimal places to show")]
        public int decimalPlaces = 0;
    }
}