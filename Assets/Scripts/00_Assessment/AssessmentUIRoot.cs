// AssessmentUIRoot.cs
// ✅ UPDATED:
// 1) Auto-adds ".json" if you forgot it (fixes your "No questions found" issue)
// 2) Stronger debug logs show exact StreamingAssets path and file name
// 3) If launched via AssessmentLaunchContext (Assessment2), it can:
//    - override JSON
//    - override Complete/Perfect reward ids
//    - override Chapter Completion reward id (CH2_COMPLETE)
//    - force Hub Spawn Override on return EVEN when using HubMinigameReturnContext

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AssessmentUIRoot : MonoBehaviour
{
    [Header("JSON")]
    [Tooltip("File name inside Assets/StreamingAssets/ (example: Assessment_questions.json)")]
    public string jsonFileName = "Assessment_questions.json";
    public bool shuffleQuestions = true;
    public bool shuffleChoices = true;

    [Header("UI - Question Board")]
    public TMP_Text questionText;
    public TMP_Text questionNumberText;
    public TMP_Text scoreText;

    [Header("UI - Choices")]
    public Transform choicesParent;
    public ChoiceButtonUI choiceButtonPrefab;

    [Header("Timer (Top Right Circle)")]
    public Image timerCircleImage;
    public float timePerQuestion = 10f;

    [Header("NPC Feedback")]
    public AssessmentNPCFeedback npcFeedback;

    [Header("Flow")]
    public float nextDelay = 0.55f;

    [Header("Auto Close After Finish")]
    public bool autoCloseOnFinish = true;

    [Tooltip("How long to show FINAL SCORE before returning.")]
    public float autoCloseDelay = 1.5f;

    [Tooltip("If true, this script will DISABLE this GameObject after finish (optional).")]
    public bool disableThisGameObjectOnFinish = false;

    [Tooltip("Optional override: if assigned, THIS object will be disabled instead of (this.gameObject).")]
    public GameObject disableTargetOverride;

    [Tooltip("If true, UI uses unscaled time (good if you pause the game).")]
    public bool useUnscaledTime = true;

    // =========================================================
    // Player Lock While Assessing (CutsceneRunner-based)
    // (DO NOT TOUCH - kept as-is)
    // =========================================================
    [Header("Player Lock While Assessing (via CutsceneRunner)")]
    public bool blockPlayerWhileActive = true;

    [Tooltip("Optional override. If null/empty, Assessment uses CutsceneRunner's defaultPlayerTag.")]
    public string playerTagOverride = "Player";

    [Tooltip("Optional direct player transform. If null, CutsceneRunner will find by tag.")]
    public Transform playerOverride;

    // Optional wrapper (music stop etc.)
    [Header("Optional Wrapper (music stop etc.)")]
    public AssessmentUI assessmentUI;

    // =========================================================
    // ✅ WIN STATE + RETURN (modern)
    // =========================================================
    [Header("Win State (Modern Return)")]
    [Tooltip("Optional win UI root (disabled by default). If assigned, it will show on finish.")]
    public GameObject winRoot;

    [Tooltip("Optional animator on win UI.")]
    public Animator winAnimator;

    [Tooltip("Animator trigger name.")]
    public string winTrigger = "Win";

    [Tooltip("How long to keep win UI / final score visible before returning.")]
    public float winHoldSeconds = 1.25f;

    [Header("Return Fallback (Safety)")]
    [Tooltip("If MinigameReturnContext is missing/broken, load this scene name.")]
    public string fallbackReturnSceneName = "";

    [Tooltip("Force Time.timeScale = 1 before returning.")]
    public bool forceUnpauseOnReturn = true;

    // =========================================================
    // ✅ Objective Update On Return (Hub)
    // =========================================================
    [Header("Objective Update On Return (Hub)")]
    [Tooltip("If true, before returning we will force the HUB objective id below.")]
    public bool forceHubObjectiveOnReturn = false;

    [Tooltip("Hub objective id to force (example: 04_objective).")]
    public string hubObjectiveIdOnReturn = "04_objective";

    [Tooltip("If true, play objective SFX when applying (only if hub is active / UI is available).")]
    public bool playObjectiveSfxOnApply = false;

    // =========================================================
    // ✅ HUB SPAWN OVERRIDE (STRING-BASED, NO NEW SCRIPTS)
    // =========================================================
    [Header("Hub Spawn Override (Story Return Only)")]
    [Tooltip("If set, returning to HUB from the STORY assessment path will teleport the player to the GameObject with this name (one-time).")]
    public string hubSpawnPointNameOnReturn = "HubSpawnPoint";

    [Tooltip("Hub scene name to match against returnSceneName.")]
    public string hubSceneName = "04_Gamehub";

    [Tooltip("Player tag used in the HUB scene (for teleport).")]
    public string playerTagForHubSpawn = "Player";

    // =========================================================
    // ✅ REWARDS (EXISTING)
    // =========================================================
    [Header("Rewards (Chapter Completion / Badges)")]
    [Tooltip("If true, unlock reward(s) when assessment finishes and is about to return.")]
    public bool unlockRewardsOnFinish = true;

    [Tooltip("Chapter completion reward ID (ex: CH1_COMPLETE).")]
    public string chapterCompletionRewardId = RewardIDs.Chapter1Complete;

    [Tooltip("Optional extra badge reward IDs to unlock on finish (leave empty if none).")]
    public List<string> extraRewardIdsToUnlock = new List<string>();

    [Tooltip("If true, only unlock rewards if score >= minScoreToUnlock. If false, always unlock on finish.")]
    public bool requireMinScoreToUnlock = false;

    [Tooltip("Minimum score required to unlock rewards (only used if requireMinScoreToUnlock = true).")]
    public int minScoreToUnlock = 0;

    // =========================================================
    // ✅ NEW: ASSESSMENT BADGES
    // =========================================================
    [Header("Rewards (Assessment Badges - NEW)")]
    [Tooltip("If ON: awards a badge the first time this assessment is completed (per save/profile).")]
    public bool awardCompletionBadgeFirstTime = true;

    [Tooltip("Reward ID for completing the assessment the first time (ex: CompleteAssessment1).")]
    public string assessmentCompleteRewardId = "CompleteAssessment1";

    [Tooltip("If ON: awards a badge when player finishes with a perfect score.")]
    public bool awardPerfectScoreBadge = true;

    [Tooltip("Reward ID for perfect score (ex: PerfectAssessment1).")]
    public string perfectScoreRewardId = "PerfectAssessment1";

    // ============================================================
    // START / FINISH INDICATORS (MATCHES GalleryGame EXACTLY)
    // ============================================================
    [Header("Start Countdown Gate (NEW)")]
    public bool enableStartCountdownGate = true;
    public GameObject countdownRoot;
    public TMP_Text countdownText;
    public float countdownStepSeconds = 0.75f;
    public float countdownStartHoldSeconds = 0.35f;
    public string countdownStartText = "START";

    [Header("Finish Overlay (NEW)")]
    public GameObject finishRoot;
    public TMP_Text finishText;
    public string finishMessage = "FINISHED!";
    public float finishHoldSeconds = 0.8f;

    [Header("Countdown/Finish Ease (NEW)")]
    [Tooltip("Ease for fade/scale in/out on Countdown + Finish overlays.")]
    public AnimationCurve countdownEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float countdownScaleMin = 0.75f;
    public float countdownScaleMax = 1.15f;

    [Header("Countdown/Finish Text Size (NEW)")]
    [Tooltip("If > 1, countdown text becomes bigger during 3-2-1-START (then restored).")]
    public float countdownFontSizeMultiplier = 3.0f;

    [Tooltip("If > 1, finish text becomes bigger during FINISHED! (then restored).")]
    public float finishFontSizeMultiplier = 2.5f;

    [Tooltip("If ON, forces center alignment for countdown/finish text while showing.")]
    public bool forceCenterAlignForCountdown = true;

    // ============================================================
    // ✅ Launch Overrides (from buttons)
    // ============================================================
    private bool _launchWantsHubSpawnOverride = false;

    // ============================================================
    // ONE-TIME HUB SPAWN OVERRIDE (STATIC, NO EXTRA SCRIPT)
    // ============================================================
    private static bool _pendingHubSpawn = false;
    private static string _pendingHubSceneName = "";
    private static string _pendingPlayerTag = "Player";
    private static string _pendingSpawnObjectName = "";
    private static bool _spawnHookInstalled = false;

    private static void InstallSpawnHookIfNeeded()
    {
        if (_spawnHookInstalled) return;
        SceneManager.sceneLoaded += ApplyPendingHubSpawn_OnSceneLoaded;
        _spawnHookInstalled = true;
    }

    private static void ArmPendingHubSpawnByName(string hubSceneName, string playerTag, string spawnObjectName)
    {
        if (string.IsNullOrWhiteSpace(spawnObjectName)) return;

        _pendingHubSpawn = true;
        _pendingHubSceneName = hubSceneName ?? "";
        _pendingPlayerTag = string.IsNullOrEmpty(playerTag) ? "Player" : playerTag;
        _pendingSpawnObjectName = spawnObjectName;

        InstallSpawnHookIfNeeded();
    }

    private static void ApplyPendingHubSpawn_OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (!_pendingHubSpawn) return;
        if (string.IsNullOrEmpty(_pendingHubSceneName)) return;

        if (!string.Equals(s.name, _pendingHubSceneName, StringComparison.OrdinalIgnoreCase))
            return;

        var playerGO = GameObject.FindGameObjectWithTag(_pendingPlayerTag);
        if (playerGO == null)
        {
            Debug.LogWarning($"[AssessmentUIRoot] Pending Hub Spawn: Player with tag '{_pendingPlayerTag}' not found in scene '{s.name}'.");
            return;
        }

        GameObject spawnGO = GameObject.Find(_pendingSpawnObjectName);
        if (spawnGO == null)
        {
            Debug.LogWarning($"[AssessmentUIRoot] Pending Hub Spawn: Spawn object '{_pendingSpawnObjectName}' not found in scene '{s.name}'.");
            return;
        }

        playerGO.transform.SetPositionAndRotation(spawnGO.transform.position, spawnGO.transform.rotation);

        // clear one-time override
        _pendingHubSpawn = false;
        _pendingHubSceneName = "";
        _pendingSpawnObjectName = "";
    }

    // ============================================================
    // runtime
    // ============================================================
    private AssessmentQuestionList _data;
    private List<AssessmentQuestion> _questions;
    private int _index;
    private int _score;
    private bool _running;
    private bool _finished;

    private readonly List<ChoiceButtonUI> _spawnedButtons = new List<ChoiceButtonUI>();
    private Coroutine _timerRoutine;
    private Coroutine _flowRoutine;
    private Coroutine _autoCloseRoutine;

    // input gate (countdown / finish)
    private bool inputLocked = false;
    private bool _countdownDone = false;

    public event Action OnAssessmentClosed;

    // =========================================================
    // ✅ AUTO START IN THIS SCENE
    // =========================================================
    private void Start()
    {
        // ✅ Apply one-time overrides BEFORE starting assessment
        if (AssessmentLaunchContext.TryConsume(out var launch))
        {
            if (!string.IsNullOrWhiteSpace(launch.jsonFileName))
                jsonFileName = launch.jsonFileName;

            if (!string.IsNullOrWhiteSpace(launch.completionRewardId))
                assessmentCompleteRewardId = launch.completionRewardId;

            if (!string.IsNullOrWhiteSpace(launch.perfectRewardId))
                perfectScoreRewardId = launch.perfectRewardId;

            if (!string.IsNullOrWhiteSpace(launch.chapterCompletionRewardId))
                chapterCompletionRewardId = launch.chapterCompletionRewardId;

            _launchWantsHubSpawnOverride = launch.useHubSpawnOverrideOnReturn;
            if (launch.useHubSpawnOverrideOnReturn && !string.IsNullOrWhiteSpace(launch.hubSpawnPointNameOnReturn))
                hubSpawnPointNameOnReturn = launch.hubSpawnPointNameOnReturn;
        }

        BeginAssessmentFromTrigger();
    }

    private void OnEnable()
    {
        if (winRoot) winRoot.SetActive(false);

        if (countdownRoot) countdownRoot.SetActive(false);
        if (finishRoot) finishRoot.SetActive(false);
    }

    private void OnDisable()
    {
        if (_running)
            EndAssessmentInternal(restorePlayer: true);

        StopAllRoutines();
    }

    public void BeginAssessmentFromTrigger()
    {
        if (_running) return;

        if (blockPlayerWhileActive)
            LockPlayer();

        BeginAssessment();
    }

    public void BeginAssessment()
    {
        if (_running) return;

        _running = true;
        _finished = false;
        _score = 0;
        _index = 0;

        _countdownDone = false;

        if (npcFeedback != null) npcFeedback.ResetToDefaultInstant();

        if (winRoot) winRoot.SetActive(false);
        if (countdownRoot) countdownRoot.SetActive(false);
        if (finishRoot) finishRoot.SetActive(false);

        StopAllRoutines();
        _flowRoutine = StartCoroutine(LoadThenStart());
    }

    public void CloseAndRestore()
    {
        if (_running)
            EndAssessmentInternal(restorePlayer: true);
        else
            RestorePlayerControl();

        OnAssessmentClosed?.Invoke();
    }

    private void EndAssessmentInternal(bool restorePlayer)
    {
        _running = false;

        StopAllRoutines();
        SetAllButtonsInteractable(false);

        if (timerCircleImage != null) timerCircleImage.fillAmount = 1f;
        if (npcFeedback != null) npcFeedback.ResetToDefaultInstant();

        if (restorePlayer)
            RestorePlayerControl();
    }

    // =========================================================
    // (DO NOT TOUCH) Player Lock
    // =========================================================
    private void LockPlayer()
    {
        if (CutsceneRunner.Instance != null)
        {
            CutsceneRunner.Instance.ExternalLockPlayer(playerOverride, playerTagOverride);
            return;
        }

        Debug.LogWarning("[AssessmentUIRoot] CutsceneRunner.Instance missing. Player will not be locked.");
    }

    private void RestorePlayerControl()
    {
        if (!blockPlayerWhileActive) return;

        if (CutsceneRunner.Instance != null)
        {
            CutsceneRunner.Instance.ExternalUnlockPlayer();
            return;
        }
    }

    // =========================================================
    // Routines
    // =========================================================
    private void StopAllRoutines()
    {
        if (_timerRoutine != null) StopCoroutine(_timerRoutine);
        if (_flowRoutine != null) StopCoroutine(_flowRoutine);
        if (_autoCloseRoutine != null) StopCoroutine(_autoCloseRoutine);

        _timerRoutine = null;
        _flowRoutine = null;
        _autoCloseRoutine = null;
    }

    private IEnumerator LoadThenStart()
    {
        yield return LoadJsonFromStreamingAssets();
        if (!_running) yield break;

        if (_data == null || _data.questions == null || _data.questions.Count == 0)
        {
            if (questionText) questionText.text = "No questions found.";
            if (questionNumberText) questionNumberText.text = "";
            if (scoreText) scoreText.text = "";
            ClearButtons();

            _running = false;
            RestorePlayerControl();

            if (autoCloseOnFinish)
                _autoCloseRoutine = StartCoroutine(WinThenReturnRoutine());

            yield break;
        }

        _questions = new List<AssessmentQuestion>(_data.questions);
        if (shuffleQuestions) Shuffle(_questions);

        _index = 0;
        _score = 0;

        if (enableStartCountdownGate)
            SetInputLocked(true);
        else
            SetInputLocked(false);

        ShowQuestion(_index);

        if (enableStartCountdownGate)
            StartCoroutine(StartCountdownGateRoutine());
    }

    private void ShowQuestion(int i)
    {
        if (!_running) return;

        if (_questions == null || i < 0 || i >= _questions.Count)
        {
            FinishAssessment();
            return;
        }

        var q = _questions[i];
        if (q == null)
        {
            _index++;
            ShowQuestion(_index);
            return;
        }

        if (q.choices == null) q.choices = new List<string>();

        if (questionText) questionText.text = string.IsNullOrEmpty(q.question) ? "(Missing question)" : q.question;
        if (questionNumberText) questionNumberText.text = $"Question {i + 1} / {_questions.Count}";
        UpdateScoreText();

        BuildChoiceButtonsForQuestion(q);

        if (_timerRoutine != null) StopCoroutine(_timerRoutine);
        _timerRoutine = null;

        if (!inputLocked)
            _timerRoutine = StartCoroutine(TimerRoutine());
        else
        {
            if (timerCircleImage != null) timerCircleImage.fillAmount = 1f;
            SetAllButtonsInteractable(false);
        }
    }

    private void BuildChoiceButtonsForQuestion(AssessmentQuestion q)
    {
        ClearButtons();

        if (choicesParent == null || choiceButtonPrefab == null)
        {
            Debug.LogWarning("[AssessmentUIRoot] Missing choicesParent or choiceButtonPrefab.");
            return;
        }

        List<string> choices = new List<string>(q.choices);

        int correct = Mathf.Clamp(q.correctIndex, 0, Mathf.Max(0, choices.Count - 1));
        if (shuffleChoices && choices.Count > 1)
        {
            List<int> order = new List<int>();
            for (int k = 0; k < choices.Count; k++) order.Add(k);
            Shuffle(order);

            List<string> shuffled = new List<string>(choices.Count);
            int newCorrect = 0;
            for (int k = 0; k < order.Count; k++)
            {
                int oldIndex = order[k];
                shuffled.Add(choices[oldIndex]);
                if (oldIndex == correct) newCorrect = k;
            }

            choices = shuffled;
            correct = newCorrect;
        }

        q.correctIndex = correct;

        for (int c = 0; c < choices.Count; c++)
        {
            var btn = Instantiate(choiceButtonPrefab, choicesParent);
            _spawnedButtons.Add(btn);

            string label = string.IsNullOrEmpty(choices[c]) ? "(Empty)" : choices[c];
            int choiceIndex = c;

            btn.Setup(label, choiceIndex, HandleChoiceClicked);
            btn.SetInteractable(!inputLocked);
        }
    }

    private void HandleChoiceClicked(int choiceIndex)
    {
        if (!_running || _finished || inputLocked) return;

        if (_timerRoutine != null) StopCoroutine(_timerRoutine);
        _timerRoutine = null;

        SetAllButtonsInteractable(false);

        var q = _questions[_index];
        bool correct = (choiceIndex == Mathf.Clamp(q.correctIndex, 0, (q.choices?.Count ?? 1) - 1));

        if (correct)
        {
            _score++;
            if (npcFeedback != null) npcFeedback.PlayCorrect();
        }
        else
        {
            if (npcFeedback != null) npcFeedback.PlayWrong();
        }

        UpdateScoreText();

        if (_flowRoutine != null) StopCoroutine(_flowRoutine);
        _flowRoutine = StartCoroutine(NextQuestionAfterDelay());
    }

    private IEnumerator NextQuestionAfterDelay()
    {
        yield return Wait(nextDelay);

        _index++;
        ShowQuestion(_index);
    }

    private IEnumerator TimerRoutine()
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, timePerQuestion);

        if (timerCircleImage != null)
            timerCircleImage.fillAmount = 1f;

        while (t < dur && _running && !_finished && !inputLocked)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float a = Mathf.Clamp01(1f - (t / dur));
            if (timerCircleImage != null)
                timerCircleImage.fillAmount = a;

            yield return null;
        }

        if (!_running || _finished || inputLocked) yield break;

        SetAllButtonsInteractable(false);
        if (npcFeedback != null) npcFeedback.PlayWrong();

        if (_flowRoutine != null) StopCoroutine(_flowRoutine);
        _flowRoutine = StartCoroutine(NextQuestionAfterDelay());
    }

    // =========================================================
    // FINISH + WIN + RETURN
    // =========================================================
    private void FinishAssessment()
    {
        if (_finished) return;

        _finished = true;
        _running = false;

        StopAllRoutines();
        SetAllButtonsInteractable(false);
        ClearButtons();

        if (timerCircleImage != null) timerCircleImage.fillAmount = 1f;

        if (questionText) questionText.text = "Assessment Complete!";
        if (questionNumberText) questionNumberText.text = "";
        if (scoreText) scoreText.text = $"Score: {_score} / {_questions?.Count ?? 0}";

        if (npcFeedback != null) npcFeedback.ResetToDefaultInstant();

        if (autoCloseOnFinish)
            _autoCloseRoutine = StartCoroutine(WinThenReturnRoutine());
    }

    private IEnumerator WinThenReturnRoutine()
    {
        SetInputLocked(true);

        if (winRoot)
        {
            winRoot.SetActive(true);
            ForceEnableTree(winRoot);
        }

        if (winAnimator && !string.IsNullOrEmpty(winTrigger))
        {
            try { winAnimator.ResetTrigger(winTrigger); } catch { }
            try { winAnimator.SetTrigger(winTrigger); } catch { }
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, winHoldSeconds > 0 ? winHoldSeconds : autoCloseDelay));

        if (finishRoot != null && finishText != null)
            yield return StartCoroutine(FinishOverlayRoutine());

        TryUnlockFinishRewards();

        RestorePlayerControl();

        if (assessmentUI != null)
            assessmentUI.StopAssessmentMusicOnly();

        if (disableThisGameObjectOnFinish)
        {
            var t = (disableTargetOverride != null) ? disableTargetOverride : gameObject;
            if (t != null) t.SetActive(false);
        }

        OnAssessmentClosed?.Invoke();

        if (forceUnpauseOnReturn)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        // (optional) hub objective force
        if (forceHubObjectiveOnReturn && !string.IsNullOrEmpty(hubObjectiveIdOnReturn))
        {
            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.ForceSetHubObjective(
                    hubObjectiveIdOnReturn,
                    playSfx: playObjectiveSfxOnApply,
                    applyOnNextHubLoadToo: true
                );
            }
            else
            {
                Debug.LogWarning("[AssessmentUIRoot] ObjectiveManager.Instance is null. Hub objective cannot be forced.");
            }
        }

        // ✅ Decide which return scene we are going to
        string targetReturnScene = null;

        if (HubMinigameReturnContext.hasData && !string.IsNullOrWhiteSpace(HubMinigameReturnContext.returnSceneName))
            targetReturnScene = HubMinigameReturnContext.returnSceneName;
        else if (MinigameReturnContext.hasData && !string.IsNullOrWhiteSpace(MinigameReturnContext.returnSceneName))
            targetReturnScene = MinigameReturnContext.returnSceneName;

        // ✅ NEW: Apply hub spawn override EVEN for HubMinigameReturnContext path
        bool isReturningToHub =
            !string.IsNullOrWhiteSpace(targetReturnScene) &&
            !string.IsNullOrWhiteSpace(hubSceneName) &&
            string.Equals(targetReturnScene, hubSceneName, StringComparison.OrdinalIgnoreCase);

        if (isReturningToHub && _launchWantsHubSpawnOverride && !string.IsNullOrWhiteSpace(hubSpawnPointNameOnReturn))
        {
            ArmPendingHubSpawnByName(hubSceneName, playerTagForHubSpawn, hubSpawnPointNameOnReturn);
        }

        // ✅ Return
        if (HubMinigameReturnContext.hasData && !string.IsNullOrWhiteSpace(HubMinigameReturnContext.returnSceneName))
        {
            HubMinigameReturnContext.ReturnToWorld();
            yield break;
        }

        if (MinigameReturnContext.hasData && !string.IsNullOrWhiteSpace(MinigameReturnContext.returnSceneName))
        {
            MinigameReturnContext.ReturnToWorld();
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(fallbackReturnSceneName))
        {
            SceneManager.LoadScene(fallbackReturnSceneName, LoadSceneMode.Single);
            yield break;
        }

        Debug.LogError("[AssessmentUIRoot] RETURN FAILED: No HubMinigameReturnContext, no MinigameReturnContext, AND no fallbackReturnSceneName set.");
    }

    private void TryUnlockFinishRewards()
    {
        if (!unlockRewardsOnFinish) return;

        if (requireMinScoreToUnlock && _score < minScoreToUnlock)
            return;

        if (RewardStateManager.Instance == null)
        {
            Debug.LogWarning("[AssessmentUIRoot] RewardStateManager.Instance is null. Rewards cannot be unlocked.");
            return;
        }

        // ✅ Chapter completion
        if (!string.IsNullOrWhiteSpace(chapterCompletionRewardId))
        {
            RewardStateManager.Instance.MarkRewardPending(chapterCompletionRewardId);
        }

        // ✅ Optional extra rewards
        if (extraRewardIdsToUnlock != null)
        {
            for (int i = 0; i < extraRewardIdsToUnlock.Count; i++)
            {
                var id = extraRewardIdsToUnlock[i];
                if (string.IsNullOrWhiteSpace(id)) continue;
                RewardStateManager.Instance.MarkRewardPending(id);
            }
        }

        // ✅ Complete Assessment (first time only)
        if (awardCompletionBadgeFirstTime && !string.IsNullOrWhiteSpace(assessmentCompleteRewardId))
        {
            bool already =
                RewardStateManager.Instance.IsRewardUnlocked(assessmentCompleteRewardId) ||
                RewardStateManager.Instance.IsRewardPending(assessmentCompleteRewardId);

            if (!already)
                RewardStateManager.Instance.MarkRewardPending(assessmentCompleteRewardId);
        }

        // ✅ Perfect Score Badge
        if (awardPerfectScoreBadge && !string.IsNullOrWhiteSpace(perfectScoreRewardId))
        {
            int total = _questions != null ? _questions.Count : 0;
            bool isPerfect = (total > 0 && _score >= total);

            if (isPerfect)
            {
                bool already =
                    RewardStateManager.Instance.IsRewardUnlocked(perfectScoreRewardId) ||
                    RewardStateManager.Instance.IsRewardPending(perfectScoreRewardId);

                if (!already)
                    RewardStateManager.Instance.MarkRewardPending(perfectScoreRewardId);
            }
        }
    }

    // =========================================================
    // Countdown + Finish UI
    // =========================================================
    private void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        SetAllButtonsInteractable(!inputLocked);

        if (!inputLocked && _running && !_finished && _timerRoutine == null)
            _timerRoutine = StartCoroutine(TimerRoutine());
    }

    private IEnumerator StartCountdownGateRoutine()
    {
        SetInputLocked(true);

        if (countdownRoot == null || countdownText == null)
        {
            SetInputLocked(false);
            yield break;
        }

        countdownRoot.SetActive(true);
        ForceEnableTree(countdownRoot);

        var textGO = countdownText.gameObject;

        var cg = textGO.GetComponent<CanvasGroup>();
        if (cg == null) cg = textGO.AddComponent<CanvasGroup>();

        var rt = countdownText.rectTransform;

        float originalFontSize = countdownText.fontSize;
        bool originalAutoSize = countdownText.enableAutoSizing;
        TextAlignmentOptions originalAlign = countdownText.alignment;

        if (forceCenterAlignForCountdown)
            countdownText.alignment = TextAlignmentOptions.Center;

        countdownText.enableAutoSizing = false;

        float mult = Mathf.Max(0.01f, countdownFontSizeMultiplier);
        countdownText.fontSize = originalFontSize * mult;

        Vector3 baseScale = (rt != null) ? rt.localScale : Vector3.one;

        for (int n = 3; n >= 1; n--)
        {
            countdownText.text = n.ToString();
            yield return AnimateOverlayInOut(cg, rt, baseScale, countdownStepSeconds);
        }

        countdownText.text = string.IsNullOrWhiteSpace(countdownStartText) ? "START" : countdownStartText;
        yield return AnimateOverlayInOut(cg, rt, baseScale, Mathf.Max(0.05f, countdownStartHoldSeconds));

        countdownText.fontSize = originalFontSize;
        countdownText.enableAutoSizing = originalAutoSize;
        countdownText.alignment = originalAlign;

        countdownRoot.SetActive(false);

        _countdownDone = true;
        SetInputLocked(false);
    }

    private IEnumerator FinishOverlayRoutine()
    {
        if (finishRoot == null || finishText == null) yield break;

        finishRoot.SetActive(true);
        ForceEnableTree(finishRoot);

        var textGO = finishText.gameObject;

        var cg = textGO.GetComponent<CanvasGroup>();
        if (cg == null) cg = textGO.AddComponent<CanvasGroup>();

        var rt = finishText.rectTransform;

        float originalFontSize = finishText.fontSize;
        bool originalAutoSize = finishText.enableAutoSizing;
        TextAlignmentOptions originalAlign = finishText.alignment;

        if (forceCenterAlignForCountdown)
            finishText.alignment = TextAlignmentOptions.Center;

        finishText.enableAutoSizing = false;

        float mult = Mathf.Max(0.01f, finishFontSizeMultiplier);
        finishText.fontSize = originalFontSize * mult;

        Vector3 baseScale = (rt != null) ? rt.localScale : Vector3.one;

        finishText.text = string.IsNullOrWhiteSpace(finishMessage) ? "FINISHED!" : finishMessage;

        yield return AnimateOverlayInOut(cg, rt, baseScale, Mathf.Max(0.05f, finishHoldSeconds));

        finishText.fontSize = originalFontSize;
        finishText.enableAutoSizing = originalAutoSize;
        finishText.alignment = originalAlign;

        finishRoot.SetActive(false);
    }

    private IEnumerator AnimateOverlayInOut(CanvasGroup cg, RectTransform rt, Vector3 baseScale, float holdSeconds)
    {
        float inDur = 0.18f;
        float outDur = 0.18f;

        yield return AnimateOverlay(cg, rt, baseScale, 0f, 1f, countdownScaleMin, countdownScaleMax, inDur);

        yield return new WaitForSeconds(Mathf.Max(0.01f, holdSeconds));

        yield return AnimateOverlay(cg, rt, baseScale, 1f, 0f, countdownScaleMax, countdownScaleMin, outDur);
    }

    private IEnumerator AnimateOverlay(CanvasGroup cg, RectTransform rt, Vector3 baseScale, float a0, float a1, float s0, float s1, float dur)
    {
        if (cg != null) cg.alpha = a0;
        if (rt != null) rt.localScale = baseScale * s0;

        float t = 0f;
        dur = Mathf.Max(0.01f, dur);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);
            float e = countdownEase != null ? countdownEase.Evaluate(u) : u;

            if (cg != null) cg.alpha = Mathf.LerpUnclamped(a0, a1, e);
            if (rt != null) rt.localScale = baseScale * Mathf.LerpUnclamped(s0, s1, e);

            yield return null;
        }

        if (cg != null) cg.alpha = a1;
        if (rt != null) rt.localScale = baseScale * s1;
    }

    // =========================================================
    // Helpers
    // =========================================================
    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {_score}";
    }

    private void ClearButtons()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            if (_spawnedButtons[i] != null)
                Destroy(_spawnedButtons[i].gameObject);
        }
        _spawnedButtons.Clear();
    }

    private void SetAllButtonsInteractable(bool value)
    {
        if (inputLocked) value = false;

        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            if (_spawnedButtons[i] != null)
                _spawnedButtons[i].SetInteractable(value);
        }
    }

    // ✅ NEW: auto-fix missing extension
    private string NormalizeJsonFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        name = name.Trim();

        if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            name += ".json";

        return name;
    }

    private IEnumerator LoadJsonFromStreamingAssets()
    {
        _data = null;

        // ✅ Normalize so Inspector can use "Assessment2_questions" OR "Assessment2_questions.json"
        jsonFileName = NormalizeJsonFileName(jsonFileName);

        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest req = UnityWebRequest.Get(path))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AssessmentUIRoot] Failed to load JSON.\nFile: {jsonFileName}\nPath: {path}\nError: {req.error}");
                yield break;
            }

            string json = req.downloadHandler.text;
            _data = JsonUtility.FromJson<AssessmentQuestionList>(json);

            if (_data == null || _data.questions == null)
                Debug.LogError($"[AssessmentUIRoot] JSON parsed but data/questions is null.\nFile: {jsonFileName}\nPath: {path}\n(Check root format: {{ \"questions\": [ ... ] }})");
        }
#else
        if (!File.Exists(path))
        {
            Debug.LogError($"[AssessmentUIRoot] JSON not found.\nFile: {jsonFileName}\nPath: {path}\nMake sure it's in Assets/StreamingAssets/ and the name matches exactly (case-sensitive on Android).");
            yield break;
        }

        string json = File.ReadAllText(path);
        _data = JsonUtility.FromJson<AssessmentQuestionList>(json);

        if (_data == null || _data.questions == null)
            Debug.LogError($"[AssessmentUIRoot] JSON parsed but data/questions is null.\nFile: {jsonFileName}\nPath: {path}\n(Check root format: {{ \"questions\": [ ... ] }})");

        yield return null;
#endif
    }

    private IEnumerator Wait(float seconds)
    {
        if (seconds <= 0f) yield break;
        if (useUnscaledTime) yield return new WaitForSecondsRealtime(seconds);
        else yield return new WaitForSeconds(seconds);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        if (list == null || list.Count <= 1) return;

        for (int i = 0; i < list.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    private static void ForceEnableTree(GameObject root)
    {
        if (!root) return;

        if (!root.activeSelf) root.SetActive(true);

        var t = root.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i).gameObject;
            if (!child.activeSelf) child.SetActive(true);
            ForceEnableTree(child);
        }

        var cgs = root.GetComponentsInChildren<CanvasGroup>(true);
        foreach (var cg in cgs)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        var canvases = root.GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases) c.enabled = true;

        var images = root.GetComponentsInChildren<Image>(true);
        foreach (var img in images) img.enabled = true;

        var tmps = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in tmps) tmp.enabled = true;
    }
}