using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace SkyRingTerrarium.UI
{
    /// <summary>
    /// Tooltip system for UI elements.
    /// Displays contextual help text when hovering over UI elements.
    /// </summary>
    public class TooltipSystem : MonoBehaviour
    {
        public static TooltipSystem Instance { get; private set; }

        [Header("Tooltip Panel")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private RectTransform tooltipRect;

        [Header("Settings")]
        [SerializeField] private float showDelay = 0.5f;
        [SerializeField] private float hideDelay = 0.1f;
        [SerializeField] private Vector2 offset = new Vector2(10f, -10f);
        [SerializeField] private float padding = 10f;

        [Header("Animation")]
        [SerializeField] private float fadeSpeed = 10f;

        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;
        private RectTransform canvasRect;

        private float showTimer;
        private float targetAlpha;
        private TooltipData pendingTooltip;
        private bool isShowing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            canvasGroup = tooltipPanel?.GetComponent<CanvasGroup>();
            if (canvasGroup == null && tooltipPanel != null)
            {
                canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            }

            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasRect = parentCanvas.GetComponent<RectTransform>();
            }

            Hide();
        }

        private void Update()
        {
            // Handle show delay
            if (pendingTooltip != null && !isShowing)
            {
                showTimer -= Time.unscaledDeltaTime;
                if (showTimer <= 0f)
                {
                    ShowImmediate(pendingTooltip);
                }
            }

            // Update position to follow mouse
            if (isShowing)
            {
                UpdatePosition();
            }

            // Fade animation
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
            }
        }

        #region Public API

        public void Show(string header, string content)
        {
            Show(new TooltipData { Header = header, Content = content });
        }

        public void Show(TooltipData data)
        {
            pendingTooltip = data;
            showTimer = showDelay;
        }

        public void ShowImmediate(string header, string content)
        {
            ShowImmediate(new TooltipData { Header = header, Content = content });
        }

        public void ShowImmediate(TooltipData data)
        {
            if (tooltipPanel == null) return;

            pendingTooltip = null;
            isShowing = true;

            // Set content
            if (headerText != null)
            {
                headerText.text = data.Header;
                headerText.gameObject.SetActive(!string.IsNullOrEmpty(data.Header));
            }

            if (contentText != null)
            {
                contentText.text = data.Content;
            }

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

            // Show
            tooltipPanel.SetActive(true);
            targetAlpha = 1f;

            UpdatePosition();
        }

        public void Hide()
        {
            pendingTooltip = null;
            isShowing = false;
            targetAlpha = 0f;

            if (canvasGroup != null && canvasGroup.alpha <= 0.01f && tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        #endregion

        #region Position

        private void UpdatePosition()
        {
            if (tooltipRect == null || canvasRect == null) return;

            Vector2 mousePos = Input.mousePosition;
            Vector2 tooltipPos = mousePos + offset;

            // Get tooltip size
            Vector2 tooltipSize = tooltipRect.sizeDelta;

            // Clamp to screen bounds
            float rightEdge = tooltipPos.x + tooltipSize.x + padding;
            float bottomEdge = tooltipPos.y - tooltipSize.y - padding;

            if (rightEdge > Screen.width)
            {
                tooltipPos.x = mousePos.x - tooltipSize.x - offset.x;
            }

            if (bottomEdge < 0)
            {
                tooltipPos.y = mousePos.y + tooltipSize.y - offset.y;
            }

            tooltipRect.position = tooltipPos;
        }

        #endregion
    }

    [Serializable]
    public class TooltipData
    {
        public string Header;
        public string Content;
    }

    /// <summary>
    /// Attach to UI elements to enable tooltips on hover.
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Tooltip Content")]
        [SerializeField] private string header;
        [SerializeField, TextArea] private string content;

        [Header("Settings")]
        [SerializeField] private bool showImmediate = false;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (showImmediate)
            {
                TooltipSystem.Instance?.ShowImmediate(header, content);
            }
            else
            {
                TooltipSystem.Instance?.Show(header, content);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipSystem.Instance?.Hide();
        }

        public void SetContent(string newHeader, string newContent)
        {
            header = newHeader;
            content = newContent;
        }
    }
}
