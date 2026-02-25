using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class HubMinigameNavigatorUI : MonoBehaviour
{
    [Serializable]
    public class MinigameButton
    {
        [Header("UI + Scene")]
        public Button button;
        public string sceneName;

        [TextArea(2, 4)]
        public string confirmMessage = "Enter this minigame?";

        public bool forceWhiteFade = true;

        // =========================================================
        // ✅ NEW: Optional Assessment Launch Overrides (per-button)
        // If sceneName is your assessment scene (or any scene that
        // has AssessmentUIRoot), this config can override what JSON
        // + reward IDs that assessment uses.
        // =========================================================
        [Header("Assessment Overrides (Optional)")]
        [Tooltip("If ON, this button will set AssessmentLaunchContext before loading the scene.")]
        public bool useAssessmentOverrides = false;

        [Tooltip("StreamingAssets JSON file (ex: Assessment2_questions.json). Leave empty to keep AssessmentUIRoot default.")]
        public string assessmentJsonFileName = "";

        [Tooltip("First-time completion badge (ex: CompleteAssessment2). Leave empty to keep AssessmentUIRoot default.")]
        public string completeAssessmentRewardId = "";

        [Tooltip("Perfect score badge (ex: PerfectAssessment2). Leave empty to keep AssessmentUIRoot default.")]
        public string perfectAssessmentRewardId = "";

        [Tooltip("Story completion reward (ex: CH2_COMPLETE). Leave empty to not override.")]
        public string chapterCompletionRewardId = "";

        [Header("Assessment -> Hub Spawn Override (Optional)")]
        [Tooltip("If ON, when assessment returns to HUB it will teleport to hubSpawnPointNameOnReturn (one-time).")]
        public bool useHubSpawnOverrideOnReturn = false;

        [Tooltip("Spawn point object name IN THE HUB scene (ex: Assessment2_Return).")]
        public string hubSpawnPointNameOnReturn = "";
    }

    // =========================
    // SAVEPOINT-STYLE INTERACT
    // =========================
    [Header("Prompt UI (like SavePoint / WorldInteractable)")]
    public GameObject promptObject;

    [Header("Interaction (New Input System)")]
    [Tooltip("Assign Player/Interact from your Input Actions asset (same as SavePoint).")]
    public InputActionReference interactAction;

    [Tooltip("If true, interaction is blocked while dialogue is playing.")]
    public bool blockDuringDialogue = true;

    [Tooltip("Player tag to detect in trigger.")]
    public string playerTag = "Player";

    // =========================
    // UI
    // =========================
    [Header("Root")]
    public GameObject root;

    [Header("Title")]
    public TMP_Text titleText;
    public string title = "Minigames";

    [Header("Buttons")]
    public Button closeButton;
    public GameObject firstSelectedOnOpen;

    [Header("Minigame Buttons (manual drag & drop)")]
    public List<MinigameButton> minigames = new List<MinigameButton>();

    [Header("Confirm Prompt")]
    public string confirmTitle = "A Memory Worth Saving";
    public string yesLabel = "Yes";
    public string noLabel = "No";

    // =========================
    // HUB RETURN
    // =========================
    [Header("Hub Return")]
    public string hubSceneName = "04_Gamehub";
    public string hubSceneNameForPose = "04_Gamehub";
    public bool saveHubPoseBeforeLeaving = true;

    // =========================
    // FREEZE MOVEMENT (COPY SAVE MENU)
    // =========================
    [Header("Freeze Like Save Menu (Time.timeScale)")]
    [Tooltip("If ON: OpenMenu sets Time.timeScale=0, CloseMenu restores it.")]
    public bool freezeWorldWithTimeScale = true;

    private float _prevTimeScale = 1f;
    private bool _timeScaleCaptured = false;

    // =========================
    // OPTIONAL: ACTION MAP SWAP (extra safety)
    // =========================
    [Header("Optional Extra Lock (Action Map Swap)")]
    public bool alsoSwapActionMap = true;

    [Tooltip("Drag your PlayerInput here. If empty, auto-finds PlayerInput on the Player.")]
    public PlayerInput playerInput;

    [Tooltip("Gameplay action map name (movement). Usually 'Player' or 'Gameplay'.")]
    public string gameplayActionMapName = "Player";

    [Tooltip("UI action map name. Usually 'UI'.")]
    public string uiActionMapName = "UI";

    // =========================
    // INTERNALS
    // =========================
    private bool _playerInRange;
    private bool _isOpen;

    private string _prevActionMap = "";
    private bool _cachedPrevActionMap = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;

        if (root) root.SetActive(false);
        if (promptObject) promptObject.SetActive(false);
    }

    private void Start()
    {
        WireButtons();
    }

    private void OnEnable()
    {
        if (interactAction != null && interactAction.action != null)
            interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
            interactAction.action.Disable();

        _playerInRange = false;
        if (promptObject) promptObject.SetActive(false);

        if (_isOpen)
        {
            ForceCloseAndRestore();
        }
        else
        {
            RestoreTimeScaleIfWePaused();
            RestoreActionMapIfWeSwapped();
        }
    }

    private void ForceCloseAndRestore()
    {
        _isOpen = false;

        if (root) root.SetActive(false);

        RestoreTimeScaleIfWePaused();
        RestoreActionMapIfWeSwapped();

        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void WireButtons()
    {
        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseMenu);
        }

        for (int i = 0; i < minigames.Count; i++)
        {
            int idx = i;
            var entry = minigames[idx];
            if (entry.button == null) continue;

            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() => TryOpenConfirm(idx));
        }
    }

    // =========================
    // TRIGGER (SavePoint style)
    // =========================
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInRange = true;

        if (promptObject)
            promptObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInRange = false;

        if (promptObject)
            promptObject.SetActive(false);
    }

    private void Update()
    {
        if (!_playerInRange) return;

        if (blockDuringDialogue && SimpleDialogueManager.Instance != null && SimpleDialogueManager.Instance.IsPlaying)
            return;

        if (interactAction == null || interactAction.action == null) return;

        if (interactAction.action.WasPressedThisFrame())
        {
            if (_isOpen) CloseMenu();
            else OpenMenu();
        }
    }

    // =========================
    // SAVE MENU FREEZE / RESTORE
    // =========================
    private void PauseTimeScaleLikeSaveMenu()
    {
        if (!freezeWorldWithTimeScale) return;

        if (!_timeScaleCaptured)
        {
            _prevTimeScale = Time.timeScale;
            _timeScaleCaptured = true;
        }

        Time.timeScale = 0f;
    }

    private void RestoreTimeScaleIfWePaused()
    {
        if (!freezeWorldWithTimeScale) return;

        if (_timeScaleCaptured)
        {
            Time.timeScale = _prevTimeScale;
            _timeScaleCaptured = false;
        }
    }

    // =========================
    // OPTIONAL ACTION MAP SWAP
    // =========================
    private PlayerInput ResolvePlayerInput()
    {
        if (playerInput) return playerInput;

        var playerGo = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGo)
        {
            playerInput = playerGo.GetComponent<PlayerInput>();
            if (playerInput) return playerInput;

            playerInput = playerGo.GetComponentInChildren<PlayerInput>(true);
            if (playerInput) return playerInput;
        }

#if UNITY_2023_1_OR_NEWER
        playerInput = UnityEngine.Object.FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
#else
        playerInput = UnityEngine.Object.FindObjectOfType<PlayerInput>(true);
#endif
        return playerInput;
    }

    private void SwapToUIMap()
    {
        if (!alsoSwapActionMap) return;

        var pi = ResolvePlayerInput();
        if (!pi) return;

        if (!_cachedPrevActionMap)
        {
            _prevActionMap = pi.currentActionMap != null ? pi.currentActionMap.name : "";
            _cachedPrevActionMap = true;
        }

        if (!string.IsNullOrWhiteSpace(uiActionMapName))
        {
            try { pi.SwitchCurrentActionMap(uiActionMapName); } catch { }
        }
    }

    private void RestoreActionMapIfWeSwapped()
    {
        if (!alsoSwapActionMap) return;

        var pi = ResolvePlayerInput();
        if (!pi) return;

        if (_cachedPrevActionMap)
        {
            string restore = !string.IsNullOrWhiteSpace(_prevActionMap) ? _prevActionMap : gameplayActionMapName;
            if (!string.IsNullOrWhiteSpace(restore))
            {
                try { pi.SwitchCurrentActionMap(restore); } catch { }
            }

            _cachedPrevActionMap = false;
            _prevActionMap = "";
        }
    }

    // =========================
    // MENU OPEN/CLOSE
    // =========================
    public void OpenMenu()
    {
        if (!root) return;
        if (_isOpen) return;

        if (titleText) titleText.text = title;

        root.SetActive(true);
        _isOpen = true;

        PauseTimeScaleLikeSaveMenu();
        SwapToUIMap();

        StartCoroutine(SetSelectionNextFrame());
    }

    public void CloseMenu()
    {
        if (!root) return;
        if (!_isOpen) return;

        root.SetActive(false);
        _isOpen = false;

        RestoreTimeScaleIfWePaused();
        RestoreActionMapIfWeSwapped();

        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private IEnumerator SetSelectionNextFrame()
    {
        yield return null;

        if (EventSystem.current && firstSelectedOnOpen)
            EventSystem.current.SetSelectedGameObject(firstSelectedOnOpen);
    }

    // =========================
    // CONFIRM + LOAD
    // =========================
    private void TryOpenConfirm(int index)
    {
        if (index < 0 || index >= minigames.Count) return;

        var entry = minigames[index];
        if (string.IsNullOrEmpty(entry.sceneName)) return;

        var mgr = SimpleDialogueManager.Instance;
        if (mgr == null)
        {
            LoadMinigame(entry);
            return;
        }

        mgr.TryStartYesNoPrompt(
            confirmTitle,
            entry.confirmMessage,
            yesLabel,
            noLabel,
            onYes: () => LoadMinigame(entry),
            onNo: () =>
            {
                StartCoroutine(SetSelectionNextFrame());
            },
            useTyping: true
        );
    }

    private void LoadMinigame(MinigameButton entry)
    {
        // ✅ mark "return to hub" context so minigames return to hub
        HubMinigameReturnContext.SetHubReturn(hubSceneName, entry.forceWhiteFade);

        // optional hub pose save
        if (saveHubPoseBeforeLeaving)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                LoadCharacter.SaveHubPoseNow(player.transform, hubSceneNameForPose);
        }

        // ✅ NEW: per-button Assessment content selection
        // Only applies if the button wants assessment overrides.
        if (entry.useAssessmentOverrides)
        {
            AssessmentLaunchContext.Set(
                entry.assessmentJsonFileName,
                entry.completeAssessmentRewardId,
                entry.perfectAssessmentRewardId,
                entry.chapterCompletionRewardId,
                entry.useHubSpawnOverrideOnReturn,
                entry.hubSpawnPointNameOnReturn
            );
        }

        // close & restore
        CloseMenu();

        // transition load
        if (SceneTransition.Instance != null)
        {
            if (entry.forceWhiteFade)
                SceneTransition.Instance.LoadSceneWhite(entry.sceneName);
            else
                SceneTransition.Instance.LoadScene(entry.sceneName);
        }
        else
        {
            SceneManager.LoadScene(entry.sceneName);
        }
    }
}