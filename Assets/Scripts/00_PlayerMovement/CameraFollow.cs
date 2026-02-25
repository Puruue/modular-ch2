using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    [Header("Auto Target")]
    public bool autoFindTarget = true;
    public string playerTag = "Player";
    public float refindInterval = 0.25f;
    private float _nextRefindTime = 0f;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Centering / Focus Point (IMPORTANT for isometric)")]
    public Vector3 focusOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Offset")]
    public Vector3 offset;
    public bool autoComputeOffsetIfZero = true;

    [Header("Isometric Helper (optional)")]
    public bool useDefaultIsometricOffsetIfOffsetZero = false;
    public Vector3 defaultIsometricOffset = new Vector3(-8f, 10f, -8f);

    [Header("Follow Smoothing")]
    public bool smoothFollow = true;
    public float smoothTime = 6f;

    [Header("Vertical Damp (stairs help)")]
    public bool dampVertical = true;
    public float verticalDampTime = 4f;

    [Header("Snap Settings")]
    public float snapDistance = 3f;

    [Header("Spawn/Scene Load Stabilizer (recommended)")]
    public bool snapAfterSettle = true;
    public int settleFixedFrames = 3;
    public bool waitForGroundedBeforeSnap = true;
    public float groundedCheckDistance = 3.0f;
    public LayerMask groundMask = ~0;
    public bool waitForLowVelocityBeforeSnap = true;
    public float settledVelocity = 0.15f;

    [Header("Anti-Clipping / Collision Avoidance (RECOMMENDED)")]
    public bool avoidClipping = true;
    public LayerMask clipMask = ~0;
    public float clipSphereRadius = 0.35f;
    public float clipPadding = 0.10f;
    public float minDistanceFromTarget = 1.0f;

    // ===========================
    // ✅ CAMERA SETTINGS LOCK
    // ===========================
    [Header("Camera Settings Lock (Fixes GameHub → Chapter skew)")]
    public bool enforceSceneCameraSettings = true;
    public bool enforceRotation = true;
    public bool enforceProjection = true;
    public int enforceFramesAfterEnable = 2;

    private Camera _cam;
    private Quaternion _sceneRotation;
    private bool _sceneIsOrtho;
    private float _sceneOrthoSize;
    private float _sceneFov;

    // internal state
    private Vector3 _velXZ = Vector3.zero;
    private float _velY = 0f;
    private bool _forceSnapThisFrame = false;

    private Vector3 _sceneStartCamPos;
    private bool _didComputeOffsetOnce = false;

    // ✅ NEW: track if offset was auto-computed previously
    private bool _offsetWasAutoComputed = false;

    // ✅ NEW: scene tracking so we can reset auto offset on scene change
    private int _lastSceneHandle = -1;

    private Coroutine _settleRoutine;
    private Coroutine _delayedAcquireRoutine;
    private Coroutine _enforceRoutine;

    private void Awake()
    {
        _cam = GetComponent<Camera>();

        CaptureSceneDefaultsForThisScene(forceResetAutoOffset: true);
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // If we got enabled again in the SAME scene, still force snap.
        _forceSnapThisFrame = true;

        if (enforceSceneCameraSettings)
            StartEnforceRoutine();

        if (_delayedAcquireRoutine != null) StopCoroutine(_delayedAcquireRoutine);
        _delayedAcquireRoutine = StartCoroutine(DelayedAcquire());
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void Start()
    {
        if (enforceSceneCameraSettings)
            StartEnforceRoutine();

        TryAutoFindTarget(force: true);

        if (_delayedAcquireRoutine != null) StopCoroutine(_delayedAcquireRoutine);
        _delayedAcquireRoutine = StartCoroutine(DelayedAcquire());

        // Initial offset computation (only once, only if still zero)
        if (target != null && autoComputeOffsetIfZero && offset == Vector3.zero && !_didComputeOffsetOnce)
        {
            if (useDefaultIsometricOffsetIfOffsetZero)
                offset = defaultIsometricOffset;
            else
                offset = _sceneStartCamPos - GetFocusPoint();

            _didComputeOffsetOnce = true;
            _offsetWasAutoComputed = true;
        }

        if (useDefaultIsometricOffsetIfOffsetZero && offset == Vector3.zero && !_didComputeOffsetOnce)
        {
            offset = defaultIsometricOffset;
            _didComputeOffsetOnce = true;
            _offsetWasAutoComputed = true;
        }
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        // ✅ If we moved scenes (Hub -> Chapter / return from minigame),
        // re-capture defaults AND reset auto-computed offset.
        CaptureSceneDefaultsForThisScene(forceResetAutoOffset: true);

        // re-acquire target and settle snap
        TryAutoFindTarget(force: true);
        StartSettleSnapRoutine();
    }

    private void CaptureSceneDefaultsForThisScene(bool forceResetAutoOffset)
    {
        Scene s = SceneManager.GetActiveScene();
        _lastSceneHandle = s.handle;

        if (_cam == null) _cam = GetComponent<Camera>();

        // capture what THIS scene wants the camera to be (as currently configured)
        _sceneRotation = transform.rotation;

        if (_cam != null)
        {
            _sceneIsOrtho = _cam.orthographic;
            _sceneOrthoSize = _cam.orthographicSize;
            _sceneFov = _cam.fieldOfView;
        }

        _sceneStartCamPos = transform.position;

        // ✅ Reset only if the offset was auto-generated previously (so inspector offsets aren't destroyed)
        if (forceResetAutoOffset && _offsetWasAutoComputed)
        {
            offset = Vector3.zero;
            _didComputeOffsetOnce = false;
        }

        _forceSnapThisFrame = true;
    }

    private IEnumerator DelayedAcquire()
    {
        yield return null;

        if (enforceSceneCameraSettings)
            ApplySceneCameraSettings();

        TryAutoFindTarget(force: true);

        if (target != null)
            StartSettleSnapRoutine();
    }

    private void LateUpdate()
    {
        // Safety: if scene changed without activeSceneChanged firing (rare), detect it
        int currentHandle = SceneManager.GetActiveScene().handle;
        if (currentHandle != _lastSceneHandle)
        {
            CaptureSceneDefaultsForThisScene(forceResetAutoOffset: true);
        }

        TryAutoFindTarget(force: false);
        if (!target) return;

        // If offset still zero, compute once using scene-start position
        if (autoComputeOffsetIfZero && offset == Vector3.zero && !_didComputeOffsetOnce)
        {
            offset = useDefaultIsometricOffsetIfOffsetZero
                ? defaultIsometricOffset
                : (_sceneStartCamPos - GetFocusPoint());

            _didComputeOffsetOnce = true;
            _offsetWasAutoComputed = true;
            _forceSnapThisFrame = true;
        }

        Vector3 focusPoint = GetFocusPoint();
        Vector3 desired = focusPoint + offset;

        if (avoidClipping)
            desired = ApplyClipAvoidance(focusPoint, desired);

        bool shouldSnap =
            _forceSnapThisFrame ||
            !smoothFollow ||
            (transform.position - desired).sqrMagnitude > snapDistance * snapDistance;

        if (shouldSnap)
        {
            transform.position = desired;
            _forceSnapThisFrame = false;
            _velXZ = Vector3.zero;
            _velY = 0f;
            return;
        }

        Vector3 current = transform.position;

        float tXZ = 1f - Mathf.Exp(-Mathf.Max(0f, smoothTime) * Time.deltaTime);
        float tY = dampVertical
            ? 1f - Mathf.Exp(-Mathf.Max(0f, verticalDampTime) * Time.deltaTime)
            : tXZ;

        current.x = Mathf.Lerp(current.x, desired.x, tXZ);
        current.z = Mathf.Lerp(current.z, desired.z, tXZ);
        current.y = Mathf.Lerp(current.y, desired.y, tY);

        transform.position = current;
    }

    private Vector3 GetFocusPoint()
    {
        if (!target) return transform.position;
        return target.position + focusOffset;
    }

    private Vector3 ApplyClipAvoidance(Vector3 from, Vector3 desired)
    {
        Vector3 toDesired = desired - from;
        float dist = toDesired.magnitude;

        if (dist <= 0.0001f) return desired;

        Vector3 dir = toDesired / dist;

        if (Physics.SphereCast(from, clipSphereRadius, dir, out RaycastHit hit, dist, clipMask, QueryTriggerInteraction.Ignore))
        {
            float safeDist = Mathf.Max(minDistanceFromTarget, hit.distance - clipPadding);
            return from + dir * safeDist;
        }

        return desired;
    }

    private void TryAutoFindTarget(bool force)
    {
        if (!autoFindTarget) return;
        if (target != null) return;

        if (!force && Time.time < _nextRefindTime) return;
        _nextRefindTime = Time.time + Mathf.Max(0.05f, refindInterval);

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            bool recalc = autoComputeOffsetIfZero && offset == Vector3.zero && !_didComputeOffsetOnce;
            SetTarget(playerObj.transform, snap: false, recalcOffset: recalc);
            StartSettleSnapRoutine();
        }
    }

    private void StartSettleSnapRoutine()
    {
        if (!snapAfterSettle) { ForceSnap(); return; }

        if (_settleRoutine != null)
            StopCoroutine(_settleRoutine);

        _settleRoutine = StartCoroutine(SnapWhenSettled());
    }

    private IEnumerator SnapWhenSettled()
    {
        int frames = Mathf.Max(0, settleFixedFrames);
        for (int i = 0; i < frames; i++)
            yield return new WaitForFixedUpdate();

        if (!target) yield break;

        if (enforceSceneCameraSettings)
            ApplySceneCameraSettings();

        Rigidbody rb = target.GetComponent<Rigidbody>();

        if (waitForGroundedBeforeSnap)
        {
            float timeout = 1.0f;
            float t = 0f;
            while (t < timeout && target != null && !IsGrounded())
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        if (waitForLowVelocityBeforeSnap && rb != null)
        {
            float timeout = 1.0f;
            float t = 0f;

            while (t < timeout && target != null && GetRigidbodySpeed_NoObsolete(rb) > settledVelocity)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        ForceSnap();
    }

    // ✅ Avoids rb.velocity obsolete warnings by using reflection for BOTH velocity and linearVelocity.
    private static float GetRigidbodySpeed_NoObsolete(Rigidbody rb)
    {
        if (rb == null) return 0f;

        Vector3 v;
        if (TryGetVector3Property(rb, "linearVelocity", out v)) return v.magnitude;
        if (TryGetVector3Property(rb, "velocity", out v)) return v.magnitude;

        return 0f;
    }

    private static bool TryGetVector3Property(object obj, string propName, out Vector3 value)
    {
        value = default;
        try
        {
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
            if (prop == null) return false;

            object raw = prop.GetValue(obj, null);
            if (raw is Vector3 vec)
            {
                value = vec;
                return true;
            }
        }
        catch { }
        return false;
    }

    private bool IsGrounded()
    {
        Vector3 focus = GetFocusPoint();
        return Physics.Raycast(focus, Vector3.down, groundedCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    // ✅ IMPORTANT CHANGE:
    // Recalculate uses CURRENT camera position (works even if scene-start pos is stale).
    public void RecalculateOffset()
    {
        if (target != null)
        {
            offset = transform.position - GetFocusPoint();
            _didComputeOffsetOnce = true;
            _offsetWasAutoComputed = true;
        }
    }

    public void ForceSnap()
    {
        _forceSnapThisFrame = true;
    }

    public void SetTarget(Transform newTarget, bool snap = true, bool recalcOffset = false)
    {
        target = newTarget;

        if (recalcOffset)
            RecalculateOffset();

        if (snap)
            ForceSnap();
    }

    // ===========================
    // ✅ Camera settings enforcement
    // ===========================
    private void StartEnforceRoutine()
    {
        if (_enforceRoutine != null) StopCoroutine(_enforceRoutine);
        _enforceRoutine = StartCoroutine(EnforceForAFewFrames());
    }

    private IEnumerator EnforceForAFewFrames()
    {
        int frames = Mathf.Max(0, enforceFramesAfterEnable);

        ApplySceneCameraSettings();
        yield return null;

        for (int i = 0; i < frames; i++)
        {
            ApplySceneCameraSettings();
            yield return null;
        }
    }

    public void ApplySceneCameraSettings()
    {
        if (!enforceSceneCameraSettings) return;

        if (enforceRotation)
            transform.rotation = _sceneRotation;

        if (enforceProjection && _cam != null)
        {
            _cam.orthographic = _sceneIsOrtho;

            if (_sceneIsOrtho)
                _cam.orthographicSize = _sceneOrthoSize;
            else
                _cam.fieldOfView = _sceneFov;
        }
    }

    private void OnValidate()
    {
        if (smoothTime < 0f) smoothTime = 0f;
        if (verticalDampTime < 0f) verticalDampTime = 0f;
        if (snapDistance < 0f) snapDistance = 0f;
        if (refindInterval < 0.05f) refindInterval = 0.05f;
        if (clipSphereRadius < 0.01f) clipSphereRadius = 0.01f;
        if (minDistanceFromTarget < 0f) minDistanceFromTarget = 0f;
        if (clipPadding < 0f) clipPadding = 0f;
        if (settleFixedFrames < 0) settleFixedFrames = 0;
        if (groundedCheckDistance < 0.1f) groundedCheckDistance = 0.1f;
        if (settledVelocity < 0f) settledVelocity = 0f;
        if (enforceFramesAfterEnable < 0) enforceFramesAfterEnable = 0;
    }
}
