using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ManualHorizontalScroller : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform viewport;   // GalleryArea
    public RectTransform content;    // PortraitGrid

    [Header("Buttons (optional)")]
    public Button leftButton;
    public Button rightButton;

    [Header("Open Behavior")]
    public bool snapToStartOnEnable = true;

    [Header("Paging")]
    public float pageWidthOverride = 0f;
    public float pageWidthMultiplier = 1f;
    public float pageExtraOffset = 0f;

    [Header("Motion")]
    public float smooth = 18f;
    public float settleThreshold = 0.1f;

    [Header("Clamp")]
    public float edgePadding = 0f;

    [Header("Debug")]
    public bool verboseLogs = true;

    private Vector2 _target;
    private bool _ready;

    void Reset()
    {
        viewport = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (!viewport) viewport = GetComponent<RectTransform>();

        if (leftButton) leftButton.onClick.AddListener(ScrollLeft);
        if (rightButton) rightButton.onClick.AddListener(ScrollRight);

        if (content) _target = content.anchoredPosition;
    }

    void OnEnable()
    {
        _ready = false;
        StopAllCoroutines();
        StartCoroutine(InitAfterLayout());
    }

    IEnumerator InitAfterLayout()
    {
        // Wait for UI + layout groups + instantiation to finish
        yield return null;
        yield return new WaitForEndOfFrame();

        ForceLayoutNow();

        if (!viewport || !content)
        {
            if (verboseLogs) Debug.LogWarning("[Scroller] Missing viewport/content refs. Assign them on GalleryArea.", this);
            yield break;
        }

        // IMPORTANT: if content width is 0 here, it means layout is not configured correctly.
        DiagnoseIfBadLayout("InitAfterLayout");

        _target = content.anchoredPosition;

        if (snapToStartOnEnable)
            JumpToStartImmediate();
        else
            ClampTargetToBounds();

        ApplyImmediate();
        UpdateButtons();

        _ready = true;
    }

    void LateUpdate()
    {
        if (!viewport || !content) return;

        // If content has collapsed (0 width), don't keep lerping — it'll “fight” layout.
        if (content.rect.width <= 0.01f)
        {
            DiagnoseIfBadLayout("LateUpdate");
            return;
        }

        content.anchoredPosition = Vector2.Lerp(
            content.anchoredPosition,
            _target,
            Time.unscaledDeltaTime * Mathf.Max(0.01f, smooth)
        );

        if (Vector2.Distance(content.anchoredPosition, _target) <= settleThreshold)
            content.anchoredPosition = _target;
    }

    public void ScrollLeft() => ScrollPages(-1);
    public void ScrollRight() => ScrollPages(+1);

    public void ScrollPages(int dir)
    {
        if (!viewport || !content)
        {
            if (verboseLogs) Debug.LogWarning("[Scroller] Button clicked but viewport/content is null. Assign refs on GalleryArea.", this);
            return;
        }

        ForceLayoutNow();
        DiagnoseIfBadLayout("ScrollPages");

        float viewportW = viewport.rect.width;
        float contentW = content.rect.width;

        if (verboseLogs)
            Debug.Log($"[Scroller] Click dir={dir} | viewportW={viewportW:F1} contentW={contentW:F1} targetX={_target.x:F1}", this);

        // If content never grows, nothing will move (key diagnostic)
        if (contentW <= viewportW + 0.01f)
        {
            if (verboseLogs)
                Debug.LogWarning("[Scroller] Content width <= viewport width. PortraitGrid is NOT expanding. " +
                                 "Fix: PortraitGrid needs a HorizontalLayoutGroup + ContentSizeFitter(H=Preferred) OR a LayoutElement width. " +
                                 "Also make sure PortraitGrid anchors are NOT stretch.", this);

            UpdateButtons();
            return;
        }

        float step = GetPageStep();
        float deltaX = -step * dir; // moving right reveals later cards => content shifts left

        _target += new Vector2(deltaX, 0f);

        ClampTargetToBounds();
        ApplyImmediateOneFrame();
        UpdateButtons();

        _ready = true;
    }

    float GetPageStep()
    {
        float baseWidth = (pageWidthOverride > 0f) ? pageWidthOverride : viewport.rect.width;
        float mult = Mathf.Max(0.01f, pageWidthMultiplier);
        return baseWidth * mult + pageExtraOffset;
    }

    public void ForceLayoutNow()
    {
        // SAFER: rebuild CONTENT first. Rebuilding viewport can collapse/mess with mask/anchors.
        Canvas.ForceUpdateCanvases();

        if (content)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        // Only rebuild viewport as a LAST resort, and only if it actually has layout components.
        // (This avoids the "viewport rebuild collapses content" bug patterns.)
        if (viewport)
        {
            // If you really want: uncomment only if needed
            // LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
        }

        Canvas.ForceUpdateCanvases();
    }

    public void JumpToStartImmediate()
    {
        if (!viewport || !content) return;

        ForceLayoutNow();
        DiagnoseIfBadLayout("JumpToStartImmediate");

        GetClampRange(out float minX, out float maxX);
        _target = new Vector2(maxX, content.anchoredPosition.y);
    }

    void ClampTargetToBounds()
    {
        GetClampRange(out float minX, out float maxX);
        _target = new Vector2(Mathf.Clamp(_target.x, minX, maxX), _target.y);

        if (verboseLogs)
            Debug.Log($"[Scroller] Clamp range: minX={minX:F1} maxX={maxX:F1} => targetX={_target.x:F1}", this);
    }

    void GetClampRange(out float minX, out float maxX)
    {
        float viewportW = viewport.rect.width;
        float contentW = content.rect.width;

        if (contentW <= viewportW + 0.01f)
        {
            minX = maxX = edgePadding;
            return;
        }

        maxX = edgePadding;
        minX = -(contentW - viewportW) - edgePadding;
    }

    void ApplyImmediate()
    {
        if (!content) return;
        content.anchoredPosition = _target;
    }

    void ApplyImmediateOneFrame()
    {
        if (!content) return;
        content.anchoredPosition = _target;
    }

    void UpdateButtons()
    {
        if (!leftButton && !rightButton) return;
        if (!viewport || !content) return;

        GetClampRange(out float minX, out float maxX);

        bool canGoLeft = _target.x < maxX - 0.01f;
        bool canGoRight = _target.x > minX + 0.01f;

        if (leftButton) leftButton.interactable = canGoLeft;
        if (rightButton) rightButton.interactable = canGoRight;
    }

    // -------------------------------------------------------
    // NEW: diagnostic helper (doesn't change behavior)
    // -------------------------------------------------------
    private void DiagnoseIfBadLayout(string caller)
    {
        if (!verboseLogs || !viewport || !content) return;

        float vw = viewport.rect.width;
        float cw = content.rect.width;

        if (vw <= 0.01f)
        {
            Debug.LogWarning($"[Scroller][{caller}] Viewport width is ~0. This usually means the parent panel is scaled to 0 or disabled, " +
                             $"or anchors are wrong. viewport='{viewport.name}'", this);
        }

        if (cw <= 0.01f)
        {
            Debug.LogWarning($"[Scroller][{caller}] Content width is ~0. This means PortraitGrid is collapsing. " +
                             $"Fix PortraitGrid: add HorizontalLayoutGroup + ContentSizeFitter(H=Preferred), " +
                             $"set child cards to LayoutElement preferred width, and avoid stretch anchors on PortraitGrid. content='{content.name}'", this);
        }
    }
}
