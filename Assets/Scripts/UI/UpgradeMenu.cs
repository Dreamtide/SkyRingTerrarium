using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SkyRingTerrarium.Core;

namespace SkyRingTerrarium.UI
{
    /// <summary>
    /// Upgrade menu for the 6 MVP upgrades.
    /// Displays upgrade cards with level, cost, and purchase buttons.
    /// </summary>
    public class UpgradeMenu : MonoBehaviour
    {
        [Header("Menu Container")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Transform upgradeCardsContainer;

        [Header("Prefab")]
        [SerializeField] private GameObject upgradeCardPrefab;

        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI currencyDisplayText;

        // Cached upgrade cards
        private Dictionary<UpgradeManager.UpgradeType, UpgradeCard> upgradeCards;

        // Events
        public event Action OnMenuOpened;
        public event Action OnMenuClosed;

        private bool isOpen = false;
        public bool IsOpen => isOpen;

        private void Awake()
        {
            upgradeCards = new Dictionary<UpgradeManager.UpgradeType, UpgradeCard>();
        }

        private void Start()
        {
            CreateUpgradeCards();
            
            // Subscribe to upgrade manager events
            UpgradeManager upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                upgradeManager.OnCurrencyChanged += UpdateCurrencyDisplay;
                upgradeManager.OnUpgradePurchased += OnUpgradePurchased;
                UpdateCurrencyDisplay(upgradeManager.Currency);
            }

            // Start closed
            Close();
        }

        private void Update()
        {
            // Toggle with Tab or M key
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.M))
            {
                Toggle();
            }
        }

        #region Menu Controls

        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
            }
            isOpen = true;
            RefreshAllCards();
            OnMenuOpened?.Invoke();

            // Play UI sound
            AudioManager.Instance?.PlayUISound("menu_open");
        }

        public void Close()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }
            isOpen = false;
            OnMenuClosed?.Invoke();

            // Play UI sound
            AudioManager.Instance?.PlayUISound("menu_close");
        }

        #endregion

        #region Card Management

        private void CreateUpgradeCards()
        {
            foreach (UpgradeManager.UpgradeType type in Enum.GetValues(typeof(UpgradeManager.UpgradeType)))
            {
                CreateCard(type);
            }
        }

        private void CreateCard(UpgradeManager.UpgradeType type)
        {
            UpgradeManager upgradeManager = UpgradeManager.Instance;
            if (upgradeManager == null) return;

            UpgradeManager.UpgradeDefinition definition = upgradeManager.GetUpgradeDefinition(type);
            if (definition == null) return;

            // Create card
            GameObject cardObj;
            if (upgradeCardPrefab != null)
            {
                cardObj = Instantiate(upgradeCardPrefab, upgradeCardsContainer);
            }
            else
            {
                // Create card programmatically if no prefab
                cardObj = CreateCardProgrammatically(type);
            }

            UpgradeCard card = cardObj.GetComponent<UpgradeCard>();
            if (card == null)
            {
                card = cardObj.AddComponent<UpgradeCard>();
            }

            card.Initialize(type, definition);
            card.OnPurchaseClicked += () => TryPurchaseUpgrade(type);

            upgradeCards[type] = card;
        }

        private GameObject CreateCardProgrammatically(UpgradeManager.UpgradeType type)
        {
            // Create a basic card layout when no prefab is assigned
            GameObject card = new GameObject($"UpgradeCard_{type}");
            card.transform.SetParent(upgradeCardsContainer, false);

            // Add layout components
            RectTransform rect = card.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 120);

            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;

            // Add UpgradeCard component
            UpgradeCard cardComponent = card.AddComponent<UpgradeCard>();

            return card;
        }

        private void RefreshAllCards()
        {
            foreach (var kvp in upgradeCards)
            {
                RefreshCard(kvp.Key);
            }
        }

        private void RefreshCard(UpgradeManager.UpgradeType type)
        {
            if (!upgradeCards.TryGetValue(type, out UpgradeCard card)) return;

            UpgradeManager upgradeManager = UpgradeManager.Instance;
            if (upgradeManager == null) return;

            int currentLevel = upgradeManager.GetUpgradeLevel(type);
            int maxLevel = upgradeManager.GetMaxLevel(type);
            int cost = upgradeManager.GetUpgradeCost(type);
            float currentValue = upgradeManager.GetUpgradeValue(type);
            bool canAfford = upgradeManager.CanUpgrade(type);

            card.UpdateDisplay(currentLevel, maxLevel, cost, currentValue, canAfford);
        }

        #endregion

        #region Purchase

        private void TryPurchaseUpgrade(UpgradeManager.UpgradeType type)
        {
            UpgradeManager upgradeManager = UpgradeManager.Instance;
            if (upgradeManager == null) return;

            if (upgradeManager.TryPurchaseUpgrade(type))
            {
                AudioManager.Instance?.PlayUISound("upgrade_purchased");
                RefreshCard(type);
            }
            else
            {
                AudioManager.Instance?.PlayUISound("error");
            }
        }

        private void OnUpgradePurchased(UpgradeManager.UpgradeType type, int newLevel)
        {
            RefreshAllCards(); // Refresh all because affordability may have changed
        }

        #endregion

        #region Currency Display

        private void UpdateCurrencyDisplay(int amount)
        {
            if (currencyDisplayText != null)
            {
                currencyDisplayText.text = $"Currency: {amount}";
            }
        }

        #endregion

        private void OnDestroy()
        {
            UpgradeManager upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                upgradeManager.OnCurrencyChanged -= UpdateCurrencyDisplay;
                upgradeManager.OnUpgradePurchased -= OnUpgradePurchased;
            }
        }
    }

    /// <summary>
    /// Individual upgrade card UI component.
    /// </summary>
    public class UpgradeCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Image progressFill;

        // Events
        public event Action OnPurchaseClicked;

        private UpgradeManager.UpgradeType upgradeType;
        private UpgradeManager.UpgradeDefinition definition;

        public void Initialize(UpgradeManager.UpgradeType type, UpgradeManager.UpgradeDefinition def)
        {
            upgradeType = type;
            definition = def;

            if (nameText != null)
                nameText.text = def.Name;
            if (descriptionText != null)
                descriptionText.text = def.Description;

            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(() => OnPurchaseClicked?.Invoke());
            }
        }

        public void UpdateDisplay(int currentLevel, int maxLevel, int cost, float currentValue, bool canAfford)
        {
            if (levelText != null)
                levelText.text = $"Level {currentLevel}/{maxLevel}";

            if (costText != null)
            {
                if (currentLevel >= maxLevel)
                    costText.text = "MAX";
                else
                    costText.text = $"Cost: {cost}";
            }

            if (valueText != null)
                valueText.text = $"Effect: {currentValue:F1}x";

            if (progressFill != null)
                progressFill.fillAmount = (float)currentLevel / maxLevel;

            if (purchaseButton != null)
            {
                purchaseButton.interactable = canAfford && currentLevel < maxLevel;
            }

            // Visual feedback for affordability
            if (costText != null)
            {
                costText.color = canAfford ? Color.white : Color.red;
            }
        }
    }
}
