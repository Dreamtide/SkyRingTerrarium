using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyRingTerrarium.Core
{
    /// <summary>
    /// Input manager with rebinding support.
    /// Uses Unity's new Input System.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;

        // Action maps
        private InputActionMap playerMap;
        private InputActionMap uiMap;

        // Cached actions
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction interactAction;
        private InputAction pauseAction;
        private InputAction menuAction;

        // Events
        public event Action<Vector2> OnMoveInput;
        public event Action OnJumpPressed;
        public event Action OnJumpReleased;
        public event Action OnInteractPressed;
        public event Action OnPausePressed;
        public event Action OnMenuPressed;

        // Rebinding
        private InputActionRebindingExtensions.RebindingOperation currentRebindOperation;
        public event Action<string, string> OnBindingChanged;
        public event Action<string> OnRebindStarted;
        public event Action<string> OnRebindComplete;
        public event Action OnRebindCanceled;

        private const string BINDINGS_KEY = "input_bindings";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeInputActions();
            LoadBindings();
        }

        private void OnEnable()
        {
            EnablePlayerInput();
        }

        private void OnDisable()
        {
            DisableAllInput();
        }

        private void InitializeInputActions()
        {
            if (inputActions == null)
            {
                // Create default actions if asset not assigned
                inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
                CreateDefaultActions();
            }

            // Get action maps
            playerMap = inputActions.FindActionMap("Player");
            uiMap = inputActions.FindActionMap("UI");

            // Cache actions
            if (playerMap != null)
            {
                moveAction = playerMap.FindAction("Move");
                jumpAction = playerMap.FindAction("Jump");
                interactAction = playerMap.FindAction("Interact");
                pauseAction = playerMap.FindAction("Pause");
            }

            if (uiMap != null)
            {
                menuAction = uiMap.FindAction("Menu");
            }

            // Subscribe to actions
            SubscribeToActions();
        }

        private void CreateDefaultActions()
        {
            // This creates a minimal default input setup
            // In production, this would be a proper InputActionAsset
            var playerMapTemp = inputActions.AddActionMap("Player");
            
            var move = playerMapTemp.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Gamepad>/leftStick/up")
                .With("Down", "<Gamepad>/leftStick/down")
                .With("Left", "<Gamepad>/leftStick/left")
                .With("Right", "<Gamepad>/leftStick/right");

            var jump = playerMapTemp.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            var interact = playerMapTemp.AddAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");

            var pause = playerMapTemp.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            var uiMapTemp = inputActions.AddActionMap("UI");
            var menu = uiMapTemp.AddAction("Menu", InputActionType.Button);
            menu.AddBinding("<Keyboard>/tab");
            menu.AddBinding("<Gamepad>/select");

            // Store references
            playerMap = playerMapTemp;
            uiMap = uiMapTemp;
            moveAction = move;
            jumpAction = jump;
            interactAction = interact;
            pauseAction = pause;
            menuAction = menu;
        }

        private void SubscribeToActions()
        {
            if (moveAction != null)
            {
                moveAction.performed += ctx => OnMoveInput?.Invoke(ctx.ReadValue<Vector2>());
                moveAction.canceled += ctx => OnMoveInput?.Invoke(Vector2.zero);
            }

            if (jumpAction != null)
            {
                jumpAction.performed += ctx => OnJumpPressed?.Invoke();
                jumpAction.canceled += ctx => OnJumpReleased?.Invoke();
            }

            if (interactAction != null)
            {
                interactAction.performed += ctx => OnInteractPressed?.Invoke();
            }

            if (pauseAction != null)
            {
                pauseAction.performed += ctx => OnPausePressed?.Invoke();
            }

            if (menuAction != null)
            {
                menuAction.performed += ctx => OnMenuPressed?.Invoke();
            }
        }

        #region Public API

        public void EnablePlayerInput()
        {
            playerMap?.Enable();
            uiMap?.Enable();
        }

        public void DisablePlayerInput()
        {
            playerMap?.Disable();
        }

        public void DisableAllInput()
        {
            playerMap?.Disable();
            uiMap?.Disable();
        }

        public void EnableUIOnly()
        {
            playerMap?.Disable();
            uiMap?.Enable();
        }

        public Vector2 GetMoveInput()
        {
            return moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        }

        public bool IsJumpHeld()
        {
            return jumpAction?.IsPressed() ?? false;
        }

        #endregion

        #region Rebinding

        public void StartRebind(string actionName, int bindingIndex = 0)
        {
            InputAction action = inputActions.FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"[InputManager] Action not found: {actionName}");
                return;
            }

            // Disable action during rebind
            action.Disable();

            OnRebindStarted?.Invoke(actionName);

            currentRebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("Mouse")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation => CompleteRebind(action, actionName))
                .OnCancel(operation => CancelRebind(action))
                .Start();
        }

        private void CompleteRebind(InputAction action, string actionName)
        {
            currentRebindOperation?.Dispose();
            currentRebindOperation = null;

            action.Enable();
            SaveBindings();

            string newBinding = action.bindings[0].effectivePath;
            OnRebindComplete?.Invoke(actionName);
            OnBindingChanged?.Invoke(actionName, newBinding);

            Debug.Log($"[InputManager] Rebound {actionName} to {newBinding}");
        }

        private void CancelRebind(InputAction action)
        {
            currentRebindOperation?.Dispose();
            currentRebindOperation = null;

            action.Enable();
            OnRebindCanceled?.Invoke();
        }

        public void ResetBindingToDefault(string actionName, int bindingIndex = 0)
        {
            InputAction action = inputActions.FindAction(actionName);
            if (action == null) return;

            action.RemoveBindingOverride(bindingIndex);
            SaveBindings();

            string defaultBinding = action.bindings[bindingIndex].effectivePath;
            OnBindingChanged?.Invoke(actionName, defaultBinding);
        }

        public void ResetAllBindings()
        {
            foreach (InputActionMap map in inputActions.actionMaps)
            {
                map.RemoveAllBindingOverrides();
            }
            SaveBindings();
        }

        public string GetBindingDisplayString(string actionName, int bindingIndex = 0)
        {
            InputAction action = inputActions.FindAction(actionName);
            if (action == null) return "";
            return action.GetBindingDisplayString(bindingIndex);
        }

        #endregion

        #region Persistence

        private void SaveBindings()
        {
            string rebinds = inputActions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(BINDINGS_KEY, rebinds);
            PlayerPrefs.Save();
        }

        private void LoadBindings()
        {
            string rebinds = PlayerPrefs.GetString(BINDINGS_KEY, "");
            if (!string.IsNullOrEmpty(rebinds))
            {
                inputActions.LoadBindingOverridesFromJson(rebinds);
            }
        }

        #endregion

        private void OnDestroy()
        {
            currentRebindOperation?.Dispose();
        }
    }
}
