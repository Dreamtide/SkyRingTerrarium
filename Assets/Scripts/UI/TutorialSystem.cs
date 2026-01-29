using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SkyRingTerrarium.UI
{
    /// <summary>
    /// Tutorial system that displays hints for first-time players.
    /// Tracks completion and only shows hints once.
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        public static TutorialSystem Instance { get; private set; }

        [Header("Tutorial Panel")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private TextMeshProUGUI stepCounterText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Image highlightOverlay;

        [Header("Settings")]
        [SerializeField] private float autoAdvanceDelay = 5f;
        [SerializeField] private bool showOnFirstPlay = true;

        [Header("Tutorial Steps")]
        [SerializeField] private List<TutorialStep> tutorialSteps;

        // Events
        public event Action OnTutorialStarted;
        public event Action OnTutorialCompleted;
        public event Action<int> OnStepShown;

        private int currentStepIndex = -1;
        private bool tutorialActive = false;
        private HashSet<string> completedHints;
        private Coroutine autoAdvanceCoroutine;

        private const string TUTORIAL_COMPLETED_KEY = "tutorial_completed";
        private const string COMPLETED_HINTS_KEY = "completed_hints";

        public bool TutorialActive => tutorialActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            completedHints = new HashSet<string>();
            LoadProgress();
            InitializeDefaultSteps();
        }

        private void Start()
        {
            // Setup buttons
            if (nextButton != null)
                nextButton.onClick.AddListener(NextStep);
            if (skipButton != null)
                skipButton.onClick.AddListener(SkipTutorial);
            if (dismissButton != null)
                dismissButton.onClick.AddListener(DismissCurrentHint);

            // Hide tutorial panel initially
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);

            // Check if we should start tutorial
            if (showOnFirstPlay && !HasCompletedTutorial())
            {
                StartCoroutine(StartTutorialDelayed(1f));
            }
        }

        private void InitializeDefaultSteps()
        {
            if (tutorialSteps == null || tutorialSteps.Count == 0)
            {
                tutorialSteps = new List<TutorialStep>
                {
                    new TutorialStep
                    {
                        Id = "welcome",
                        Title = "Welcome to Sky Ring Terrarium",
                        Message = "A living world floats before you. Explore, collect motes, and watch the ecosystem thrive.",
                        TriggerType = TutorialTriggerType.Immediate
                    },
                    new TutorialStep
                    {
                        Id = "movement",
                        Title = "Movement",
                        Message = "Use WASD or Arrow Keys to move. In this ring world, gravity always pulls toward the center.",
                        TriggerType = TutorialTriggerType.Immediate
                    },
                    new TutorialStep
                    {
                        Id = "jump",
                        Title = "Jumping",
                        Message = "Press SPACE to jump. Float bands will catch you at certain heights.",
                        TriggerType = TutorialTriggerType.Immediate
                    },
                    new TutorialStep
                    {
                        Id = "collect_motes",
                        Title = "Collecting Motes",
                        Message = "Walk through glowing motes to collect them. They're currency for upgrades!",
                        TriggerType = TutorialTriggerType.OnAction,
                        TriggerAction = "FirstMoteCollected"
                    },
                    new TutorialStep
                    {
                        Id = "upgrades",
                        Title = "Upgrades",
                        Message = "Press TAB to open the upgrade menu. Spend motes to enhance your terrarium.",
                        TriggerType = TutorialTriggerType.OnAction,
                        TriggerAction = "OpenUpgradeMenu"
                    },
                    new TutorialStep
                    {
                        Id = "creatures",
                        Title = "Creatures",
                        Message = "Drifters and other creatures live in this world. Watch them interact with the ecosystem.",
                        TriggerType = TutorialTriggerType.OnAction,
                        TriggerAction = "FirstCreatureSeen"
                    },
                    new TutorialStep
                    {
                        Id = "weather",
                        Title = "Weather",
                        Message = "Weather changes affect the world. Mote showers bring extra resources!",
                        TriggerType = TutorialTriggerType.OnAction,
                        TriggerAction = "FirstWeatherChange"
                    },
                    new TutorialStep
                    {
                        Id = "events",
                        Title = "World Events",
                        Message = "Special events occur periodically. Keep an eye out for meteors and migrations!",
                        TriggerType = TutorialTriggerType.OnAction,
                        TriggerAction = "FirstEventOccurred"
                    },
                    new TutorialStep
                    {
                        Id = "offline",
                        Title = "Offline Progress",
                        Message = "Your terrarium continues growing while you're away. Check back for rewards!",
                        TriggerType = TutorialTriggerType.Immediate
                    }
                };
            }
        }

        #region Public API

        public void StartTutorial()
        {
            if (tutorialActive) return;

            tutorialActive = true;
            currentStepIndex = -1;
            OnTutorialStarted?.Invoke();

            NextStep();
        }

        public void ShowContextualHint(string hintId, string message)
        {
            if (completedHints.Contains(hintId)) return;

            ShowHint(hintId, message);
        }

        public void TriggerAction(string actionName)
        {
            // Check if any step is waiting for this action
            for (int i = currentStepIndex + 1; i < tutorialSteps.Count; i++)
            {
                TutorialStep step = tutorialSteps[i];
                if (step.TriggerType == TutorialTriggerType.OnAction && 
                    step.TriggerAction == actionName &&
                    !completedHints.Contains(step.Id))
                {
                    // Jump to this step
                    currentStepIndex = i - 1;
                    NextStep();
                    return;
                }
            }
        }

        public void CompleteTutorial()
        {
            tutorialActive = false;
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);

            PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1);
            SaveProgress();

            OnTutorialCompleted?.Invoke();
            AudioManager.Instance?.PlayUISound("tutorial_complete");
        }

        public bool HasCompletedTutorial()
        {
            return PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;
        }

        public void ResetTutorial()
        {
            PlayerPrefs.DeleteKey(TUTORIAL_COMPLETED_KEY);
            PlayerPrefs.DeleteKey(COMPLETED_HINTS_KEY);
            completedHints.Clear();
            currentStepIndex = -1;
        }

        #endregion

        #region Step Navigation

        private void NextStep()
        {
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            currentStepIndex++;

            // Find next immediate step or complete
            while (currentStepIndex < tutorialSteps.Count)
            {
                TutorialStep step = tutorialSteps[currentStepIndex];
                if (step.TriggerType == TutorialTriggerType.Immediate && !completedHints.Contains(step.Id))
                {
                    ShowStep(step);
                    return;
                }
                currentStepIndex++;
            }

            // All immediate steps complete
            CompleteTutorial();
        }

        private void ShowStep(TutorialStep step)
        {
            if (tutorialPanel != null)
                tutorialPanel.SetActive(true);

            if (hintText != null)
            {
                hintText.text = $"<b>{step.Title}</b>\n\n{step.Message}";
            }

            if (stepCounterText != null)
            {
                int immediateSteps = tutorialSteps.FindAll(s => s.TriggerType == TutorialTriggerType.Immediate).Count;
                int currentImmediate = tutorialSteps.GetRange(0, currentStepIndex + 1)
                    .FindAll(s => s.TriggerType == TutorialTriggerType.Immediate).Count;
                stepCounterText.text = $"{currentImmediate}/{immediateSteps}";
            }

            // Mark as shown
            completedHints.Add(step.Id);
            SaveProgress();

            OnStepShown?.Invoke(currentStepIndex);
            AudioManager.Instance?.PlayUISound("tutorial_step");

            // Auto-advance for immediate steps
            if (step.AutoAdvance && step.TriggerType == TutorialTriggerType.Immediate)
            {
                autoAdvanceCoroutine = StartCoroutine(AutoAdvance(step.AutoAdvanceDelay > 0 ? step.AutoAdvanceDelay : autoAdvanceDelay));
            }
        }

        private void ShowHint(string hintId, string message)
        {
            if (tutorialPanel != null)
                tutorialPanel.SetActive(true);

            if (hintText != null)
            {
                hintText.text = message;
            }

            if (stepCounterText != null)
            {
                stepCounterText.text = "Hint";
            }

            completedHints.Add(hintId);
            SaveProgress();

            // Auto-dismiss hints
            autoAdvanceCoroutine = StartCoroutine(AutoDismissHint(3f));
        }

        private void DismissCurrentHint()
        {
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
        }

        private void SkipTutorial()
        {
            // Mark all steps as complete
            foreach (var step in tutorialSteps)
            {
                completedHints.Add(step.Id);
            }
            CompleteTutorial();
        }

        private IEnumerator AutoAdvance(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            NextStep();
        }

        private IEnumerator AutoDismissHint(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            DismissCurrentHint();
        }

        private IEnumerator StartTutorialDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartTutorial();
        }

        #endregion

        #region Persistence

        private void SaveProgress()
        {
            string hintsJson = string.Join(",", completedHints);
            PlayerPrefs.SetString(COMPLETED_HINTS_KEY, hintsJson);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            string hintsJson = PlayerPrefs.GetString(COMPLETED_HINTS_KEY, "");
            if (!string.IsNullOrEmpty(hintsJson))
            {
                string[] hints = hintsJson.Split(',');
                foreach (string hint in hints)
                {
                    if (!string.IsNullOrEmpty(hint))
                    {
                        completedHints.Add(hint);
                    }
                }
            }
        }

        #endregion
    }

    [Serializable]
    public class TutorialStep
    {
        public string Id;
        public string Title;
        [TextArea] public string Message;
        public TutorialTriggerType TriggerType;
        public string TriggerAction;
        public bool AutoAdvance = true;
        public float AutoAdvanceDelay = 0f;
    }

    public enum TutorialTriggerType
    {
        Immediate,      // Show in sequence
        OnAction,       // Show when specific action occurs
        Manual          // Only shown via code
    }
}
