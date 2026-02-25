using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class RoomCullingZone : MonoBehaviour
{
    [Tooltip("Objects in this room that should be culled (disabled until player enters).")]
    public GameObject[] roomObjects;

    [Header("Stability")]
    [Tooltip("Prevents rapid on/off spam when the player quickly crosses room triggers.")]
    public float minSwitchInterval = 0.12f;

    [Tooltip("Wait 1 frame before applying activation to avoid mid-frame movement/phasing.")]
    public bool delayActivationByOneFrame = true;

    [Header("Start / Teleport Safety")]
    [Tooltip("Re-check player bounds after a couple frames (helps when coming from Hub / teleports).")]
    public bool recheckPlayerOnStart = true;

    [Range(0, 5)]
    [Tooltip("How many frames after Start to re-check player is inside this zone.")]
    public int startRecheckFrames = 2;

    [Header("Debug")]
    public bool verboseLogs = false;

    private static readonly List<RoomCullingZone> allZones = new();
    private static RoomCullingZone activeZone;
    private static float lastSwitchTime;

    private Collider _col;
    private Coroutine _pendingActivate;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col) _col.isTrigger = true;

        if (!allZones.Contains(this))
            allZones.Add(this);

        SetRoomObjectsActive(false);
    }

    private void Start()
    {
        if (recheckPlayerOnStart)
            StartCoroutine(RecheckPlayerInsideAfterFrames());

        TryActivateIfPlayerInside();
    }

    private IEnumerator RecheckPlayerInsideAfterFrames()
    {
        for (int i = 0; i < startRecheckFrames; i++)
            yield return null;

        TryActivateIfPlayerInside();
    }

    private void TryActivateIfPlayerInside()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && _col != null)
        {
            if (_col.bounds.Contains(player.transform.position))
            {
                if (verboseLogs) Debug.Log($"[RoomCullingZone] Player detected inside '{name}'. Activating.");
                RequestActivate();
            }
        }
    }

    private void OnDestroy()
    {
        allZones.Remove(this);
        if (activeZone == this) activeZone = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        RequestActivate();
    }

    // ✅ teleport-safe activation
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (activeZone != this)
            RequestActivate();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (activeZone == this)
            StartCoroutine(DeferDeactivateIfStillActive());
    }

    private IEnumerator DeferDeactivateIfStillActive()
    {
        yield return null;
        if (activeZone == this)
        {
            if (verboseLogs) Debug.Log($"[RoomCullingZone] Player left active zone '{name}'. Deactivating.");
            SetRoomObjectsActive(false);
            activeZone = null;
        }
    }

    private void RequestActivate()
    {
        if (activeZone == this) return;

        if (Time.unscaledTime - lastSwitchTime < minSwitchInterval)
        {
            if (_pendingActivate != null) StopCoroutine(_pendingActivate);
            _pendingActivate = StartCoroutine(ActivateAfterDelay(minSwitchInterval - (Time.unscaledTime - lastSwitchTime)));
            return;
        }

        if (_pendingActivate != null) StopCoroutine(_pendingActivate);

        if (delayActivationByOneFrame)
            _pendingActivate = StartCoroutine(ActivateNextFrame());
        else
            ActivateThisZoneNow();
    }

    private IEnumerator ActivateNextFrame()
    {
        yield return null;
        ActivateThisZoneNow();
    }

    private IEnumerator ActivateAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
        if (delayActivationByOneFrame) yield return null;
        ActivateThisZoneNow();
    }

    private void ActivateThisZoneNow()
    {
        lastSwitchTime = Time.unscaledTime;
        activeZone = this;

        for (int i = 0; i < allZones.Count; i++)
        {
            var zone = allZones[i];
            if (!zone) continue;

            if (zone != this)
                zone.SetRoomObjectsActive(false);
        }

        SetRoomObjectsActive(true);

        if (verboseLogs) Debug.Log($"[RoomCullingZone] Activated room '{name}'.");
    }

    private void SetRoomObjectsActive(bool active)
    {
        if (roomObjects == null) return;
        for (int i = 0; i < roomObjects.Length; i++)
        {
            var obj = roomObjects[i];
            if (obj) obj.SetActive(active);
        }
    }
}
