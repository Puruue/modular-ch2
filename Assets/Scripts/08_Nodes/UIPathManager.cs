using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIPathManager : MonoBehaviour
{
    [Header("Control Points in order (A → … → B)")]
    public RectTransform[] controlPoints;

    [Header("Sampling")]
    public int samplesPerSegment = 10;

    [Header("Runner")]
    public RectTransform runnerBall;
    public float runnerSpeed = 300f; // pixels / second

    [Header("Checkpoints")]
    public UICheckpoint[] checkpoints;

    [Header("Checkpoint Sprites (by order)")]
    public Sprite[] inactiveNumberSprites;
    public Sprite[] passedNumberSprites;
    public bool oneBasedSprites = true;

    [Header("Path Line (optional)")]
    public UIPathLineGraphic pathLine;

    // ---------------------------
    // ✅ LOOP REQUIREMENTS + UI INDICATOR
    // ---------------------------
    [Header("Win Condition: Required Loops")]
    [Tooltip("How many SUCCESSFUL laps are required to fully complete.")]
    public int loopsToComplete = 1;

    [Tooltip("Optional UI indicator text like: 'Loops: 1 / 3'")]
    public TMP_Text loopIndicatorText;

    [Tooltip("Label prefix for the loop indicator.")]
    public string loopIndicatorPrefix = "Loops";

    [Header("Loop Behavior")]
    [Tooltip("If true: after each successful lap (that isn't final), generate a NEW random puzzle.")]
    public bool randomizePuzzleEachSuccessfulLap = true;

    [Tooltip("Optional: assign your UIPathPuzzleGenerator for loop regeneration.")]
    public UIPathPuzzleGenerator generator;

    // ✅ Final solved event (Generator uses this)
    public event Action OnPuzzleSolved;

    // ---------------------------
    // ✅ SFX
    // ---------------------------
    [Header("SFX")]
    [Tooltip("AudioSource used for checkpoint SFX. If left empty, one will be auto-created.")]
    public AudioSource sfxSource;

    [Tooltip("Plays when the runner hits the correct checkpoint in order.")]
    public AudioClip correctCheckpointSfx;

    [Tooltip("Plays when the runner hits a wrong checkpoint and the lap resets.")]
    public AudioClip wrongCheckpointSfx;

    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Tooltip("Prevents spam if multiple checkpoints are detected in the same frame.")]
    public float sfxCooldownSeconds = 0.05f;

    // ---------------------------
    // Internal path data
    // ---------------------------
    private readonly List<Vector2> sampledPoints = new List<Vector2>();
    private readonly List<float> cumulativeLen = new List<float>();
    private float totalLen = 0f;

    private float progressDist = 0f;

    private int nextCheckpointIndex;
    private bool puzzleCompleted;
    private bool allCheckpointsHitThisLap;

    private int _loopsCompleted = 0;
    private bool _isFailResetting = false;

    private Vector2[] _lastControlPositions;
    [Tooltip("How sensitive the path-change detector is (smaller = more sensitive).")]
    public float controlPointChangeEpsilon = 0.25f;

    private float _nextSfxTime = 0f;

    public IReadOnlyList<Vector2> SampledPoints => sampledPoints;

    private void Awake()
    {
        // ✅ Ensure we have an AudioSource for SFX
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f; // UI sound
        }
    }

    private void Start()
    {
        CacheControlPointPositions();
        RebuildPath();

        if (pathLine != null)
            pathLine.SetPoints(sampledPoints);

        ApplyCheckpointSprites();
        ResetRunner();
        UpdateLoopIndicator();
    }

    private void CacheControlPointPositions()
    {
        if (controlPoints == null)
        {
            _lastControlPositions = null;
            return;
        }

        _lastControlPositions = new Vector2[controlPoints.Length];
        for (int i = 0; i < controlPoints.Length; i++)
            _lastControlPositions[i] = controlPoints[i] ? controlPoints[i].anchoredPosition : Vector2.zero;
    }

    private bool ControlPointsChanged()
    {
        if (controlPoints == null || _lastControlPositions == null || _lastControlPositions.Length != controlPoints.Length)
            return true;

        float epsSqr = controlPointChangeEpsilon * controlPointChangeEpsilon;

        for (int i = 0; i < controlPoints.Length; i++)
        {
            var rt = controlPoints[i];
            if (!rt) continue;

            Vector2 now = rt.anchoredPosition;
            Vector2 old = _lastControlPositions[i];
            if ((now - old).sqrMagnitude > epsSqr)
                return true;
        }

        return false;
    }

    private void UpdateLastControlPositions()
    {
        if (controlPoints == null || _lastControlPositions == null) return;

        for (int i = 0; i < controlPoints.Length; i++)
        {
            var rt = controlPoints[i];
            if (!rt) continue;
            _lastControlPositions[i] = rt.anchoredPosition;
        }
    }

    public void RebuildPath()
    {
        sampledPoints.Clear();
        cumulativeLen.Clear();
        totalLen = 0f;

        if (controlPoints == null || controlPoints.Length < 2)
            return;

        for (int i = 0; i < controlPoints.Length - 1; i++)
        {
            Vector2 a = controlPoints[i].anchoredPosition;
            Vector2 b = controlPoints[i + 1].anchoredPosition;

            for (int s = 0; s < samplesPerSegment; s++)
            {
                float t = s / (float)samplesPerSegment;
                Vector2 p = Vector2.Lerp(a, b, t);
                sampledPoints.Add(p);
            }
        }

        sampledPoints.Add(controlPoints[controlPoints.Length - 1].anchoredPosition);

        if (sampledPoints.Count > 0)
        {
            cumulativeLen.Add(0f);
            for (int i = 1; i < sampledPoints.Count; i++)
            {
                totalLen += Vector2.Distance(sampledPoints[i - 1], sampledPoints[i]);
                cumulativeLen.Add(totalLen);
            }
        }

        if (pathLine != null)
            pathLine.SetPoints(sampledPoints);
    }

    private void Update()
    {
        if (sampledPoints.Count == 0 || runnerBall == null)
            return;

        if (ControlPointsChanged())
        {
            Vector2 oldPos = runnerBall.anchoredPosition;

            RebuildPath();
            UpdateLastControlPositions();

            SnapRunnerToPath(oldPos);

            if (pathLine != null)
                pathLine.SetPoints(sampledPoints);
        }

        if (puzzleCompleted)
        {
            runnerBall.anchoredPosition = sampledPoints[sampledPoints.Count - 1];
            return;
        }

        if (_isFailResetting)
            return;

        progressDist += runnerSpeed * Time.deltaTime;

        if (progressDist >= totalLen)
        {
            progressDist = totalLen;
            runnerBall.anchoredPosition = sampledPoints[sampledPoints.Count - 1];

            if (allCheckpointsHitThisLap && nextCheckpointIndex >= (checkpoints?.Length ?? 0))
            {
                HandleSuccessfulLap();
                return;
            }
            else
            {
                FailCurrentLap("End reached without all checkpoints");
                return;
            }
        }

        runnerBall.anchoredPosition = EvaluatePointAtDistance(progressDist);

        CheckCheckpoints();
    }

    // -------------------------------------------------------------
    // ✅ SFX helpers
    // -------------------------------------------------------------
    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        // Prevent rapid double-triggers in one frame / tight radius cases
        if (Time.unscaledTime < _nextSfxTime) return;
        _nextSfxTime = Time.unscaledTime + Mathf.Max(0f, sfxCooldownSeconds);

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume));
    }

    // -------------------------------------------------------------
    // ✅ Always stick to the path
    // -------------------------------------------------------------
    private void SnapRunnerToPath(Vector2 posInPanelSpace)
    {
        if (sampledPoints.Count < 2)
            return;

        float bestDistSqr = float.MaxValue;
        float bestAlong = 0f;
        Vector2 bestPoint = sampledPoints[0];

        for (int i = 0; i < sampledPoints.Count - 1; i++)
        {
            Vector2 a = sampledPoints[i];
            Vector2 b = sampledPoints[i + 1];

            Vector2 ab = b - a;
            float abLenSqr = ab.sqrMagnitude;
            if (abLenSqr < 0.000001f)
                continue;

            float t = Vector2.Dot(posInPanelSpace - a, ab) / abLenSqr;
            t = Mathf.Clamp01(t);
            Vector2 proj = a + ab * t;

            float dSqr = (proj - posInPanelSpace).sqrMagnitude;
            if (dSqr < bestDistSqr)
            {
                bestDistSqr = dSqr;
                bestPoint = proj;

                float segStartAlong = cumulativeLen[i];
                float segLen = Vector2.Distance(a, b);
                bestAlong = segStartAlong + (segLen * t);
            }
        }

        progressDist = Mathf.Clamp(bestAlong, 0f, totalLen);
        runnerBall.anchoredPosition = bestPoint;
    }

    private Vector2 EvaluatePointAtDistance(float dist)
    {
        dist = Mathf.Clamp(dist, 0f, totalLen);

        int seg = 0;
        for (int i = 0; i < cumulativeLen.Count - 1; i++)
        {
            if (cumulativeLen[i + 1] >= dist)
            {
                seg = i;
                break;
            }
        }

        Vector2 a = sampledPoints[seg];
        Vector2 b = sampledPoints[seg + 1];
        float start = cumulativeLen[seg];
        float end = cumulativeLen[seg + 1];
        float len = Mathf.Max(0.000001f, end - start);
        float t = (dist - start) / len;

        return Vector2.Lerp(a, b, t);
    }

    // -------------------------------------------------------------
    // ✅ SUCCESSFUL LAP HANDLING
    // -------------------------------------------------------------
    private void HandleSuccessfulLap()
    {
        _loopsCompleted++;
        UpdateLoopIndicator();

        int req = Mathf.Max(1, loopsToComplete);

        var loop = FindObjectOfType<MinigameLoopController>();
        if (loop != null)
            loop.NotifyWin();

        if (_loopsCompleted >= req)
        {
            puzzleCompleted = true;
            progressDist = totalLen;
            runnerBall.anchoredPosition = sampledPoints[sampledPoints.Count - 1];

            OnPuzzleSolved?.Invoke();
            return;
        }

        if (randomizePuzzleEachSuccessfulLap)
        {
            if (generator == null)
                generator = FindObjectOfType<UIPathPuzzleGenerator>();

            if (generator != null)
            {
                generator.GenerateNewPuzzleForNextLoop();
                return;
            }
        }

        progressDist = 0f;
        runnerBall.anchoredPosition = sampledPoints[0];
        ResetCheckpointsOnly();
    }

    public void ApplyCheckpointSprites()
    {
        if (checkpoints == null) return;

        for (int i = 0; i < checkpoints.Length; i++)
        {
            var cp = checkpoints[i];
            if (cp == null) continue;

            if (cp.orderIndex != i)
                cp.orderIndex = i;

            cp.AssignSpritesByOrder(inactiveNumberSprites, passedNumberSprites, oneBasedSprites);

            if (cp.reached) cp.OnCorrectHit();
            else cp.ResetVisual();
        }
    }

    // -------------------------------------------------------------
    // ✅ CHECKPOINT LOGIC + SFX
    // -------------------------------------------------------------
    private void CheckCheckpoints()
    {
        if (_isFailResetting) return;
        if (checkpoints == null || checkpoints.Length == 0) return;

        Vector2 ballPos = runnerBall.anchoredPosition;

        for (int i = 0; i < checkpoints.Length; i++)
        {
            UICheckpoint cp = checkpoints[i];
            if (cp == null || cp.reached) continue;

            float dist = Vector2.Distance(ballPos, cp.Rect.anchoredPosition);
            if (dist > cp.radius) continue;

            // ✅ Correct order
            if (cp.orderIndex == nextCheckpointIndex)
            {
                cp.reached = true;
                nextCheckpointIndex++;
                cp.OnCorrectHit();

                // ✅ play correct SFX
                PlaySfx(correctCheckpointSfx);

                if (nextCheckpointIndex >= checkpoints.Length)
                    allCheckpointsHitThisLap = true;
            }
            else
            {
                // ❌ Wrong order -> play wrong SFX then reset lap
                PlaySfx(wrongCheckpointSfx);
                FailCurrentLap($"Wrong checkpoint order. Hit={cp.orderIndex}, expected={nextCheckpointIndex}");
                return;
            }
        }
    }

    private void FailCurrentLap(string reason)
    {
        if (_isFailResetting) return;
        _isFailResetting = true;

        ResetRunnerKeepLoops();

        _isFailResetting = false;
    }

    private void ResetCheckpointsOnly()
    {
        nextCheckpointIndex = 0;
        allCheckpointsHitThisLap = false;

        if (checkpoints == null) return;

        foreach (var cp in checkpoints)
        {
            if (cp == null) continue;
            cp.reached = false;
            cp.ResetVisual();
        }

        ApplyCheckpointSprites();
    }

    public void ResetRunnerKeepLoops()
    {
        puzzleCompleted = false;
        allCheckpointsHitThisLap = false;

        progressDist = 0f;

        if (sampledPoints.Count > 0 && runnerBall != null)
            runnerBall.anchoredPosition = sampledPoints[0];

        ResetCheckpointsOnly();
    }

    public void ResetRunner()
    {
        puzzleCompleted = false;
        allCheckpointsHitThisLap = false;

        _loopsCompleted = 0;
        UpdateLoopIndicator();

        progressDist = 0f;

        if (sampledPoints.Count > 0 && runnerBall != null)
            runnerBall.anchoredPosition = sampledPoints[0];

        ResetCheckpointsOnly();
    }

    private void UpdateLoopIndicator()
    {
        if (loopIndicatorText == null) return;

        int req = Mathf.Max(1, loopsToComplete);
        int done = Mathf.Clamp(_loopsCompleted, 0, req);

        loopIndicatorText.text = $"{loopIndicatorPrefix}: {done} / {req}";
    }
}
