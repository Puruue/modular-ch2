using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChoiceButtonUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Refs")]
    public Button button;
    public TMP_Text label;

    [Header("Optional Animation")]
    public Animator animator; // optional

    private int _choiceIndex;
    private System.Action<int> _onClick;

    // =========================================================
    // ✅ HOVER / SELECT SCALE
    // =========================================================
    [Header("Hover / Select Scale")]
    [Tooltip("Base scale when not hovered/selected.")]
    public float normalScale = 1f;

    [Tooltip("Scale when hovered/selected.")]
    public float hoverScale = 1.06f;

    [Tooltip("How fast scale changes.")]
    public float scaleLerpSpeed = 12f;

    // =========================================================
    // ✅ IDLE WOBBLE (subtle random motion)
    // =========================================================
    [Header("Idle Wobble")]
    public bool enableWobble = true;

    [Tooltip("Random wait time before each wobble event.")]
    public Vector2 wobbleIntervalRange = new Vector2(1.2f, 3.2f);

    [Tooltip("How long a wobble lasts.")]
    public Vector2 wobbleDurationRange = new Vector2(0.35f, 0.75f);

    [Tooltip("Max extra scale during wobble (added on top of current target scale).")]
    public float wobbleScaleAmount = 0.02f;

    [Tooltip("Max rotation (degrees) during wobble.")]
    public float wobbleRotationDegrees = 1.4f;

    [Tooltip("If true, wobble pauses while hovered/selected (keeps hover clean).")]
    public bool pauseWobbleWhileFocused = true;

    // =========================================================
    // runtime
    // =========================================================
    private RectTransform _rt;
    private Vector3 _baseScale;
    private Quaternion _baseRot;

    private bool _focused;              // hovered OR selected
    private float _wobbleScaleOffset;   // animated
    private float _wobbleRotOffset;     // animated (z degrees)

    private Coroutine _wobbleRoutine;

    public void Setup(string text, int choiceIndex, System.Action<int> onClick)
    {
        _choiceIndex = choiceIndex;
        _onClick = onClick;

        if (label) label.text = text;

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    private void Awake()
    {
        _rt = transform as RectTransform;

        // capture "rest" pose (so it returns cleanly)
        _baseScale = transform.localScale;
        _baseRot = transform.localRotation;

        // Ensure normal scale starts correct
        ApplyImmediate(normalScale);

        if (enableWobble)
            _wobbleRoutine = StartCoroutine(WobbleLoop());
    }

    private void OnEnable()
    {
        // restart wobble if needed (in case UI re-enabled)
        if (enableWobble && _wobbleRoutine == null && gameObject.activeInHierarchy)
            _wobbleRoutine = StartCoroutine(WobbleLoop());
    }

    private void OnDisable()
    {
        if (_wobbleRoutine != null)
        {
            StopCoroutine(_wobbleRoutine);
            _wobbleRoutine = null;
        }

        // reset offsets so it doesn't resume mid-wobble
        _wobbleScaleOffset = 0f;
        _wobbleRotOffset = 0f;
        _focused = false;

        ApplyImmediate(normalScale);
    }

    private void Update()
    {
        // Determine target scale based on hover/select
        float target = _focused ? hoverScale : normalScale;

        // Add wobble offset (optional / usually subtle)
        float wobbleExtra = (enableWobble && (!pauseWobbleWhileFocused || !_focused)) ? _wobbleScaleOffset : 0f;

        // Smooth scale
        Vector3 desiredScale = _baseScale * (target + wobbleExtra);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.unscaledDeltaTime * scaleLerpSpeed);

        // Smooth rotation (z only)
        float rot = (enableWobble && (!pauseWobbleWhileFocused || !_focused)) ? _wobbleRotOffset : 0f;
        Quaternion desiredRot = _baseRot * Quaternion.Euler(0f, 0f, rot);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, desiredRot, Time.unscaledDeltaTime * scaleLerpSpeed);
    }

    private void HandleClick()
    {
        if (animator) animator.SetTrigger("Click");
        _onClick?.Invoke(_choiceIndex);
    }

    public void SetInteractable(bool value)
    {
        if (button) button.interactable = value;
    }

    // =========================================================
    // ✅ Hover + Selection support (mouse + keyboard/controller)
    // =========================================================
    public void OnPointerEnter(PointerEventData eventData) => _focused = true;
    public void OnPointerExit(PointerEventData eventData) => _focused = false;

    public void OnSelect(BaseEventData eventData) => _focused = true;
    public void OnDeselect(BaseEventData eventData) => _focused = false;

    // =========================================================
    // ✅ Wobble loop
    // =========================================================
    private IEnumerator WobbleLoop()
    {
        // small random start delay so not all buttons wobble together
        yield return new WaitForSecondsRealtime(Random.Range(0f, 0.6f));

        while (true)
        {
            float wait = Random.Range(Mathf.Min(wobbleIntervalRange.x, wobbleIntervalRange.y),
                                      Mathf.Max(wobbleIntervalRange.x, wobbleIntervalRange.y));
            yield return new WaitForSecondsRealtime(wait);

            // optional: pause wobble while hovered/selected
            if (pauseWobbleWhileFocused && _focused)
                continue;

            float dur = Random.Range(Mathf.Min(wobbleDurationRange.x, wobbleDurationRange.y),
                                     Mathf.Max(wobbleDurationRange.x, wobbleDurationRange.y));

            // pick random wobble target
            float targetScale = Random.Range(0f, wobbleScaleAmount);
            float targetRot = Random.Range(-wobbleRotationDegrees, wobbleRotationDegrees);

            // ease in/out using a simple sine curve
            float t = 0f;
            while (t < dur)
            {
                // if we suddenly focus, optionally stop wobble cleanly
                if (pauseWobbleWhileFocused && _focused)
                    break;

                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);

                // 0→1→0 shape
                float wave = Mathf.Sin(u * Mathf.PI);

                _wobbleScaleOffset = targetScale * wave;
                _wobbleRotOffset = targetRot * wave;

                yield return null;
            }

            // reset offsets after wobble
            _wobbleScaleOffset = 0f;
            _wobbleRotOffset = 0f;
        }
    }

    private void ApplyImmediate(float targetScale01)
    {
        transform.localScale = _baseScale * targetScale01;
        transform.localRotation = _baseRot;
    }
}
