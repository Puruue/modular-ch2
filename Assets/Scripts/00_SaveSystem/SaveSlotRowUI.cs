using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class SaveSlotRowUI : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    [Header("UI")]
    public Button button;

    public TMP_Text slotNumberText;
    public TMP_Text playerNameText;
    public TMP_Text saveLabelText;
    public TMP_Text timeText;

    public RawImage thumbnail;

    [Header("Safety")]
    [Tooltip("Prevents double-trigger if both Button.onClick and PointerClick fire in the same frame.")]
    public bool debounceSameFrame = true;

    [Tooltip("Debug logs to verify whether the slot is receiving pointer/submit and whether the action is null.")]
    public bool debugClicks = false;

    private Action _onClick;
    private bool _hooked;

    private int _lastInvokeFrame = -9999;

    private void Awake()
    {
        if (button == null)
            button = GetComponentInChildren<Button>(true);

        HookButtonOnce();
    }

    private void OnEnable()
    {
        HookButtonOnce();
    }

    private void HookButtonOnce()
    {
        if (_hooked) return;
        if (button == null) return;

        // Add ONE listener that always calls the current Action reference
        button.onClick.AddListener(InvokeActionSafe);
        _hooked = true;
    }

    // =========================================================
    // Robust invocation (shared by Button + Pointer + Submit)
    // =========================================================
    private void InvokeActionSafe()
    {
        // Respect interactable state the same way Button would
        if (button != null && !button.IsInteractable())
        {
            if (debugClicks) Debug.Log($"[SaveSlotRowUI] '{name}' click ignored (button not interactable).");
            return;
        }

        if (debounceSameFrame)
        {
            int f = Time.frameCount;
            if (_lastInvokeFrame == f)
            {
                if (debugClicks) Debug.Log($"[SaveSlotRowUI] '{name}' click debounced (same frame).");
                return;
            }
            _lastInvokeFrame = f;
        }

        if (debugClicks)
            Debug.Log($"[SaveSlotRowUI] '{name}' InvokeActionSafe() actionNull={_onClick == null}");

        _onClick?.Invoke();
    }

    // =========================================================
    // Mouse path (bypasses Button.onClick timing issues)
    // =========================================================
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only respond to left click
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        if (debugClicks)
            Debug.Log($"[SaveSlotRowUI] '{name}' OnPointerClick()");

        InvokeActionSafe();
    }

    // =========================================================
    // Keyboard/Gamepad submit path
    // =========================================================
    public void OnSubmit(BaseEventData eventData)
    {
        if (debugClicks)
            Debug.Log($"[SaveSlotRowUI] '{name}' OnSubmit()");

        InvokeActionSafe();
    }

    // =========================================================
    // Binding
    // =========================================================
    public void Bind(int slotIndex, SaveData data, bool clickable)
    {
        if (slotNumberText) slotNumberText.text = slotIndex.ToString();

        if (data == null)
        {
            if (playerNameText) playerNameText.text = "(Empty)";
            if (saveLabelText) saveLabelText.text = "";
            if (timeText) timeText.text = "";
            if (thumbnail) thumbnail.texture = null;
        }
        else
        {
            if (playerNameText) playerNameText.text = string.IsNullOrEmpty(data.playerName) ? "(No Name)" : data.playerName;
            if (saveLabelText) saveLabelText.text = data.saveLabel;

            if (timeText)
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(data.realWorldUnixSeconds).LocalDateTime;
                timeText.text = dt.ToString("yyyy-MM-dd HH:mm");
            }

            if (thumbnail)
            {
                var tex = SaveSystem.LoadThumbnail(slotIndex);
                thumbnail.texture = tex;
            }
        }

        if (button) button.interactable = clickable;
    }

    public void SetOnClick(Action action)
    {
        _onClick = action;

        if (debugClicks)
            Debug.Log($"[SaveSlotRowUI] '{name}' SetOnClick() actionNull={_onClick == null}");
    }
}