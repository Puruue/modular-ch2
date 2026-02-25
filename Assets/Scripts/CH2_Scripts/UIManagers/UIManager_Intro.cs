using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class UIManager_Intro : MonoBehaviour
{
    [Header("Refs")]
    public HeapManager heapManager;
    public Transform numbersContainer;
    public GameObject numberButtonPrefab;
    public TextMeshProUGUI statusText;
    public CanvasGroup continuePrompt;

    [Header("Instruction UI")]
    public GameObject instructionPanel;

    [Header("Swap Animation")]
    [Tooltip("Seconds to animate a swap between two nodes.")]
    public float swapAnimDuration = 0.28f;

    [Header("Completion UI")]
    public GameObject completionPanel;

    [Header("Proceed UI")]
    public GameObject proceedPanel;

    [Tooltip("If true, uses a soft ease in/out feel.")]
    public bool useEaseInOut = true;

    [Tooltip("If true, prevents clicks while nodes are animating.")]
    public bool blockInputDuringSwap = true;

    private bool _isSwapping = false;

    // Gameplay state
    private int selectedIndex = -1;
    private bool heapIsComplete = false;

    // UI cache (built once, not destroyed every refresh)
    private readonly List<Button> _nodeButtons = new List<Button>();
    private readonly List<Image> _nodeImages = new List<Image>();
    private readonly List<TextMeshProUGUI> _nodeTexts = new List<TextMeshProUGUI>();
    private readonly List<RectTransform> _nodeRects = new List<RectTransform>();

    // Mapping: heap index -> ui index representing that node
    private readonly List<int> _uiIndexAtHeapIndex = new List<int>();

    public void ShowInstructions()
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(true);
    }

    public void HideInstructions()
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(false);
    }

    void Start()
    {
        heapManager.GenerateRandomHeap(7);

        BuildUIOnce(heapManager.heap.Count);
        RefreshUI();
        UpdateStatus();
    }

    // =========================
    // UI BUILD ONCE
    // =========================
    void BuildUIOnce(int count)
    {
        if (_nodeButtons.Count == count && _uiIndexAtHeapIndex.Count == count)
            return;

        foreach (Transform child in numbersContainer)
            Destroy(child.gameObject);

        _nodeButtons.Clear();
        _nodeImages.Clear();
        _nodeTexts.Clear();
        _nodeRects.Clear();
        _uiIndexAtHeapIndex.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject btnGO = Instantiate(numberButtonPrefab, numbersContainer);

            Button b = btnGO.GetComponent<Button>();
            Image img = btnGO.GetComponent<Image>();
            TextMeshProUGUI txt = btnGO.GetComponentInChildren<TextMeshProUGUI>();
            RectTransform rect = btnGO.GetComponent<RectTransform>();

            _nodeButtons.Add(b);
            _nodeImages.Add(img);
            _nodeTexts.Add(txt);
            _nodeRects.Add(rect);

            _uiIndexAtHeapIndex.Add(i); // ui index == heap index initially
        }
    }

    Vector2 GetNodePosition(int heapIndex)
    {
        float verticalSpacing = 120f;
        float baseHorizontalSpacing = 500f;

        int level = Mathf.FloorToInt(Mathf.Log(heapIndex + 1, 2));
        int levelStartIndex = (int)Mathf.Pow(2, level) - 1;
        int indexInLevel = heapIndex - levelStartIndex;
        int nodesInLevel = (int)Mathf.Pow(2, level);

        float horizontalSpacing = baseHorizontalSpacing / (level + 1);
        float xPos = (indexInLevel - (nodesInLevel - 1) / 2f) * horizontalSpacing;
        float yPos = -level * verticalSpacing;

        return new Vector2(xPos, yPos);
    }

    public void RefreshUI()
    {
        // Update positions + labels based on mapping
        for (int heapIndex = 0; heapIndex < heapManager.heap.Count; heapIndex++)
        {
            int uiIndex = _uiIndexAtHeapIndex[heapIndex];

            _nodeRects[uiIndex].anchoredPosition = GetNodePosition(heapIndex);
            _nodeTexts[uiIndex].text = heapManager.heap[heapIndex].ToString();
        }

        ApplyHighlightAndClicks();
    }

    void ApplyHighlightAndClicks()
    {
        // Clear all
        for (int ui = 0; ui < _nodeButtons.Count; ui++)
        {
            _nodeButtons[ui].onClick.RemoveAllListeners();
            _nodeButtons[ui].interactable = false;
            _nodeImages[ui].color = Color.white;

            if (heapIsComplete)
                _nodeImages[ui].color = new Color(0.6f, 1f, 0.6f);
        }

        if (heapIsComplete) return;
        if (blockInputDuringSwap && _isSwapping) return;

        for (int heapIndex = 0; heapIndex < heapManager.heap.Count; heapIndex++)
        {
            int localHeapIndex = heapIndex;
            int uiIndex = _uiIndexAtHeapIndex[localHeapIndex];

            _nodeButtons[uiIndex].interactable = true;
            _nodeButtons[uiIndex].onClick.AddListener(() => OnNodeClicked(localHeapIndex));

            // selected highlight
            if (localHeapIndex == selectedIndex)
                _nodeImages[uiIndex].color = new Color(1f, 1f, 0.5f);
        }
    }

    void OnNodeClicked(int index)
    {

        if (heapIsComplete)
            return;

        if (blockInputDuringSwap && _isSwapping)
            return;

        // First click selects
        if (selectedIndex == -1)
        {
            selectedIndex = index;
            ApplyHighlightAndClicks();
            return;
        }

        // Clicking selected unselects
        if (selectedIndex == index)
        {
            selectedIndex = -1;
            ApplyHighlightAndClicks();
            return;
        }

        // Second click -> swap with animation
        StartCoroutine(SwapIndicesAnimated(selectedIndex, index));
        selectedIndex = -1;
        ApplyHighlightAndClicks();
    }

    IEnumerator SwapIndicesAnimated(int a, int b)
    {
        _isSwapping = true;
        ApplyHighlightAndClicks();

        // UI indices representing these heap indices
        int uiA = _uiIndexAtHeapIndex[a];
        int uiB = _uiIndexAtHeapIndex[b];

        RectTransform rectA = _nodeRects[uiA];
        RectTransform rectB = _nodeRects[uiB];

        Vector2 startA = rectA.anchoredPosition;
        Vector2 startB = rectB.anchoredPosition;

        Vector2 targetA = GetNodePosition(b);
        Vector2 targetB = GetNodePosition(a);

        float dur = Mathf.Max(0.01f, swapAnimDuration);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float eased = useEaseInOut ? EaseInOut(t) : Mathf.Clamp01(t);

            rectA.anchoredPosition = Vector2.Lerp(startA, targetA, eased);
            rectB.anchoredPosition = Vector2.Lerp(startB, targetB, eased);

            yield return null;
        }

        rectA.anchoredPosition = targetA;
        rectB.anchoredPosition = targetB;

        // Swap heap values (actual data)
        int temp = heapManager.heap[a];
        heapManager.heap[a] = heapManager.heap[b];
        heapManager.heap[b] = temp;

        // Swap mapping so each heap index keeps the correct UI object
        int tempUI = _uiIndexAtHeapIndex[a];
        _uiIndexAtHeapIndex[a] = _uiIndexAtHeapIndex[b];
        _uiIndexAtHeapIndex[b] = tempUI;

        // Validate completion
        heapIsComplete = IsMinHeapValid();
        if (heapIsComplete)
            CompleteIntroHeap();

        _isSwapping = false;

        // Update labels (positions already correct, mapping swapped)
        RefreshUI();
        UpdateStatus();
    }

    float EaseInOut(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x); // smoothstep
    }

    void UpdateStatus()
    {
        if (statusText == null)
            return;

        if (heapIsComplete)
            statusText.text = "All boxes are safely stacked!";
        else
            statusText.text = "These boxes are stacked carelessly. Make sure lighter (lower number) boxes are always placed above heavier (higher number) ones so nothing gets crushed.";
    }

    void CompleteIntroHeap()
    {
        StartCoroutine(ShowCompletionThenProceed());
    }

    IEnumerator ShowCompletionThenProceed()
    {
        if (completionPanel != null)
        {
            completionPanel.SetActive(true);

            CanvasGroup cg = completionPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        // Wait 2 seconds
        yield return new WaitForSeconds(2f);

        if (completionPanel != null)
            completionPanel.SetActive(false);

        if (proceedPanel != null)
            proceedPanel.SetActive(true);
    }

    IEnumerator GoToPostScene()
    {
        CanvasGroup cg = completionPanel.GetComponent<CanvasGroup>();
        RectTransform rect = completionPanel.GetComponent<RectTransform>();

        cg.alpha = 0f;
        rect.localScale = Vector3.zero;

        float durationIn = 0.6f;
        float t = 0f;

        // 🎬 Dramatic entrance
        while (t < 1f)
        {
            t += Time.deltaTime / durationIn;

            float eased = EaseOutBack(t);

            rect.localScale = Vector3.one * eased;
            cg.alpha = Mathf.Clamp01(t);

            yield return null;
        }

        rect.localScale = Vector3.one;

        // Fade in continue prompt
        float promptFade = 0f;
        while (promptFade < 1f)
        {
            promptFade += Time.deltaTime;
            continuePrompt.alpha = promptFade;
            yield return null;
        }

        // ✨ Blinking effect loop while waiting for E (New Input System)
        while (Keyboard.current == null || 
            !Keyboard.current.eKey.wasPressedThisFrame)
        {
            continuePrompt.alpha = 0.5f + Mathf.PingPong(Time.time * 2f, 0.5f);
            yield return null;
        }

        // 🎬 Dramatic exit
        float durationOut = 0.5f;
        t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / durationOut;

            float eased = 1f - EaseInBack(t);

            rect.localScale = Vector3.one * eased;
            cg.alpha = 1f - t;

            yield return null;
        }

        SceneManager.LoadScene("03_Faith_PostMinHeapDialogue");
    }

    bool IsMinHeapValid()
    {
        for (int i = 0; i < heapManager.heap.Count; i++)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;

            if (left < heapManager.heap.Count && heapManager.heap[i] > heapManager.heap[left])
                return false;

            if (right < heapManager.heap.Count && heapManager.heap[i] > heapManager.heap[right])
                return false;
        }

        return true;
    }

    public void OnAskFaith()
    {
        SceneManager.LoadScene("03_Faith_PostMinHeapDialogue");
    }

    public void OnRetry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }    

    float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    float EaseInBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return c3 * x * x * x - c1 * x * x;
    }

}