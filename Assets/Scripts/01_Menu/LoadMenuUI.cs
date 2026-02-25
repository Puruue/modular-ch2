using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class LoadMenuUI : MonoBehaviour, IProfileLoadTarget
{
    [Header("Root")]
    public GameObject root;

    [Header("Title")]
    public TMP_Text titleText;

    [Header("Slots UI")]
    public GameObject slotsRoot;
    public Transform slotsParent;
    public SaveSlotRowUI slotPrefab;

    [Tooltip("Assign CanvasGroup on ScrollView/Viewport/Content (recommended).")]
    public CanvasGroup slotsCanvasGroup;

    [Header("Buttons")]
    public Button closeButton;

    [Header("Popup")]
    public ConfirmDialogUI confirmDialog;

    [Header("Settings")]
    public int maxSlots = 5;

    [Header("Selection Fix")]
    public GameObject firstSelectedOnOpen;

    [Header("Modal Robustness")]
    [Tooltip("If true, modal is opened on next frame + forced to front (fixes first-click-in-scene issue).")]
    public bool openModalNextFrame = true;

    [Tooltip("Extra safety retry if modal didn't appear (rare build timing).")]
    [Range(0, 2)] public int modalShowRetries = 1;

    private readonly List<SaveSlotRowUI> rows = new();
    private bool _showingModal = false;
    private int _pendingSlotToLoad = -1;

    private Coroutine _selectRoutine;
    private Coroutine _showModalRoutine;

    private void Awake()
    {
        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (root) root.SetActive(false);
        if (confirmDialog) confirmDialog.HideImmediate();

        BindConfirmDialogTarget();
    }

    private void OnEnable()
    {
        BindConfirmDialogTarget();
    }

    public void BindConfirmDialogTarget()
    {
        if (confirmDialog == null) return;
        if (confirmDialog.loadMenuUI == null)
            confirmDialog.loadMenuUI = this;
    }

    // IProfileLoadTarget
    public void OpenLoadOnly() => Open();

    public void Open()
    {
        BindConfirmDialogTarget();

        if (root) root.SetActive(true);
        if (titleText) titleText.text = "Load Game";

        Time.timeScale = 0f;

        _showingModal = false;
        _pendingSlotToLoad = -1;

        BuildRows();

        SetSlotsInteractable(true);

        ForceSelectNow();
        StartSelectNextFrameSafe();
    }

    public void Close()
    {
        StopSelectRoutineSafe();
        StopShowModalRoutineSafe();

        if (confirmDialog) confirmDialog.HideImmediate();

        if (root) root.SetActive(false);

        Time.timeScale = 1f;

        // Clear selection so it doesn't stick across opens
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void BuildRows()
    {
        foreach (var r in rows) if (r) Destroy(r.gameObject);
        rows.Clear();

        if (!slotPrefab || !slotsParent) return;

        for (int slot = 1; slot <= maxSlots; slot++)
        {
            SaveData data = SaveSystem.Load(slot);

            var row = Instantiate(slotPrefab, slotsParent);
            rows.Add(row);

            bool clickable = (data != null);
            row.Bind(slot, data, clickable);

            int captured = slot;
            row.SetOnClick(() => OnSlotClicked(captured));
        }
    }

    private void OnSlotClicked(int slot)
    {
        if (_showingModal) return;

        SaveData data = SaveSystem.Load(slot);
        if (data == null) return;

        ShowConfirmLoadModal(slot);
    }

    private void ShowConfirmLoadModal(int slot)
    {
        _showingModal = true;
        _pendingSlotToLoad = slot;

        SetSlotsInteractable(false);

        if (openModalNextFrame)
        {
            StopShowModalRoutineSafe();
            _showModalRoutine = StartCoroutine(ShowConfirmLoadModalRoutine(slot));
        }
        else
        {
            ShowConfirmNow(slot, 0);
        }
    }

    private IEnumerator ShowConfirmLoadModalRoutine(int slot)
    {
        // Wait one frame so UI/layout/hierarchy is fully stable in the new scene
        yield return null;
        Canvas.ForceUpdateCanvases();

        // Show (with optional retry)
        for (int attempt = 0; attempt <= modalShowRetries; attempt++)
        {
            ShowConfirmNow(slot, attempt);

            // Wait a frame and see if it actually became visible
            yield return null;

            if (IsConfirmShowing())
            {
                _showModalRoutine = null;
                yield break;
            }
        }

        // If we got here, modal did not appear -> don't soft-lock the slots
        Debug.LogWarning("[LoadMenuUI] Confirm dialog did not appear (first open timing). Re-enabling slots.");
        _showingModal = false;
        _pendingSlotToLoad = -1;
        SetSlotsInteractable(true);
        ForceSelectNow();

        _showModalRoutine = null;
    }

    private void ShowConfirmNow(int slot, int attempt)
    {
        if (confirmDialog == null)
        {
            Debug.LogWarning("[LoadMenuUI] confirmDialog missing. Loading directly.");
            _showingModal = false;
            DoLoad(slot);
            return;
        }

        // FORCE FRONT: avoids “shown but behind something”
        BringDialogToFront(confirmDialog);

        confirmDialog.ShowConfirmCancel(
            "Confirm",
            $"Are you sure you want to load Slot {slot}?",
            "Yes",
            "No",
            onYes: () => ShowLoadedModal(slot),
            onNo: () => HideModalImmediate()
        );

        if (attempt > 0)
            Debug.Log($"[LoadMenuUI] Retried showing confirm dialog (attempt {attempt}).");
    }

    private void ShowLoadedModal(int slot)
    {
        _showingModal = true;

        if (confirmDialog)
        {
            BringDialogToFront(confirmDialog);

            confirmDialog.ShowOkOnly(
                "Loaded!",
                $"SaveSlot{slot} loaded!",
                "OK",
                () =>
                {
                    HideModalImmediate();
                    DoLoad(slot);
                }
            );
        }
        else
        {
            HideModalImmediate();
            DoLoad(slot);
        }
    }

    private void HideModalImmediate()
    {
        _showingModal = false;
        _pendingSlotToLoad = -1;

        if (confirmDialog) confirmDialog.HideImmediate();

        SetSlotsInteractable(true);

        ForceSelectNow();
        StartSelectNextFrameSafe();
    }

    private void SetSlotsInteractable(bool on)
    {
        if (slotsCanvasGroup)
        {
            slotsCanvasGroup.interactable = on;
            slotsCanvasGroup.blocksRaycasts = on;
        }
    }

    private void DoLoad(int slot)
    {
        Close();
        SaveGameManager.Instance?.LoadFromSlot(slot);
    }

    // =========================================================
    // Helpers: Confirm visibility + bring to front
    // =========================================================
    private bool IsConfirmShowing()
    {
        if (confirmDialog == null) return false;

        if (confirmDialog.root != null)
            return confirmDialog.root.activeInHierarchy;

        return confirmDialog.gameObject.activeInHierarchy;
    }

    private void BringDialogToFront(ConfirmDialogUI dlg)
    {
        if (dlg == null) return;

        // If it’s under a common Canvas, last sibling makes it render on top.
        // (This fixes “opened but invisible behind” problems.)
        var t = (dlg.root != null) ? dlg.root.transform : dlg.transform;
        t.SetAsLastSibling();
    }

    private void StopShowModalRoutineSafe()
    {
        if (_showModalRoutine != null)
        {
            StopCoroutine(_showModalRoutine);
            _showModalRoutine = null;
        }
    }

    // =========================================================
    // Selection Fix
    // =========================================================
    private void ForceSelectNow()
    {
        Canvas.ForceUpdateCanvases();

        var es = EventSystem.current;
        if (es == null) return;
        if (_showingModal) return;

        GameObject target = PickBestSelectable();
        if (target == null) return;

        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(target);
    }

    private GameObject PickBestSelectable()
    {
        if (firstSelectedOnOpen != null && firstSelectedOnOpen.activeInHierarchy)
            return firstSelectedOnOpen;

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] == null) continue;
            var b = rows[i].GetComponentInChildren<Button>(true);
            if (b && b.interactable) return b.gameObject;
        }

        if (closeButton && closeButton.gameObject.activeInHierarchy)
            return closeButton.gameObject;

        return null;
    }

    private void StartSelectNextFrameSafe()
    {
        StopSelectRoutineSafe();
        if (!isActiveAndEnabled) return;
        _selectRoutine = StartCoroutine(SelectNextFrameRoutine());
    }

    private void StopSelectRoutineSafe()
    {
        if (_selectRoutine == null) return;
        StopCoroutine(_selectRoutine);
        _selectRoutine = null;
    }

    private IEnumerator SelectNextFrameRoutine()
    {
        yield return null;
        ForceSelectNow();
        _selectRoutine = null;
    }
}