using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class UIManager_TrueHeap : MonoBehaviour
{
    [Header("Refs")]
    public HeapManager heapManager;
    public Transform numbersContainer;
    public GameObject numberButtonPrefab;
    public TextMeshProUGUI statusText;

    [Header("Completion UI")]
    public GameObject completionPanel;

    [Header("Swap Animation")]
    [Tooltip("Seconds to animate a swap between two nodes.")]
    public float swapAnimDuration = 0.28f;

    [Header("Proceed UI")]
    public GameObject proceedPanel;

    [Header("Tutorial UI")]
    public GameObject tutorialPanel;
    public CanvasGroup continuePrompt;

    private bool tutorialActive = true;

    [Tooltip("If true, uses a soft ease in/out feel.")]
    public bool useEaseInOut = true;

    [Tooltip("If true, prevents clicks while nodes are animating.")]
    public bool blockInputDuringSwap = true;

    private int currentParent;
    private int heapSize;
    private bool heapCompleted = false;
    private bool _isSwapping = false;

    // UI cache (IMPORTANT: we no longer destroy/recreate each refresh)
    private readonly List<Button> _nodeButtons = new List<Button>();
    private readonly List<Image> _nodeImages = new List<Image>();
    private readonly List<TextMeshProUGUI> _nodeTexts = new List<TextMeshProUGUI>();
    private readonly List<RectTransform> _nodeRects = new List<RectTransform>();

    // Mapping: heap index -> which UI object represents that node
    // We swap these references when animating to keep indices correct.
    private readonly List<int> _uiIndexAtHeapIndex = new List<int>();

    void Start()
    {
        if (completionPanel != null)
            completionPanel.SetActive(false);

        if (proceedPanel != null)
        proceedPanel.SetActive(false);

        heapManager.GenerateRandomHeap(7);

        heapSize = heapManager.heap.Count;
        currentParent = heapSize / 2 - 1;

        BuildUIOnce(heapSize);
        RefreshUI();
        UpdateStatus();

        // --- TUTORIAL START ---
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialActive = true;
        }
        else
        {
            tutorialActive = false;
        }
    }

    // =========================
    // UI BUILD ONCE (no more destroy/recreate)
    // =========================
    void BuildUIOnce(int count)
    {
        // If already built for the right size, do nothing
        if (_nodeButtons.Count == count && _uiIndexAtHeapIndex.Count == count)
            return;

        // Clear old children (only when rebuilding for different size)
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

            // ui index == heap index initially
            _uiIndexAtHeapIndex.Add(i);
        }
    }

    // Compute the anchored position for a given heap index
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
        if (heapManager == null || heapManager.heap == null) return;

        // Update positions + labels based on mapping
        for (int heapIndex = 0; heapIndex < heapManager.heap.Count; heapIndex++)
        {
            int uiIndex = _uiIndexAtHeapIndex[heapIndex];

            // Ensure node sits at its heap index position
            _nodeRects[uiIndex].anchoredPosition = GetNodePosition(heapIndex);

            // Ensure text shows heap value at that index
            _nodeTexts[uiIndex].text = heapManager.heap[heapIndex].ToString();
        }

        // Apply colors + clickable logic
        ApplyHighlightAndClicks();
    }

    void ApplyHighlightAndClicks()
    {
        // Clear listeners each refresh
        for (int ui = 0; ui < _nodeButtons.Count; ui++)
        {
            _nodeButtons[ui].onClick.RemoveAllListeners();
            _nodeButtons[ui].interactable = false;
            _nodeImages[ui].color = Color.white;
        }

        if (heapCompleted) return;
        if (blockInputDuringSwap && _isSwapping) return;

        int left = 2 * currentParent + 1;
        int right = 2 * currentParent + 2;

        int largest = currentParent;

        if (left < heapSize && heapManager.heap[left] > heapManager.heap[largest])
            largest = left;

        if (right < heapSize && heapManager.heap[right] > heapManager.heap[largest])
            largest = right;

        bool swapNeeded = (largest != currentParent);

        // Highlight current parent (orange)
        if (currentParent >= 0 && currentParent < heapSize)
        {
            int parentUI = _uiIndexAtHeapIndex[currentParent];
            _nodeImages[parentUI].color = new Color(1f, 0.75f, 0.3f);
        }

        // Highlight children (green)
        if (left < heapSize)
        {
            int leftUI = _uiIndexAtHeapIndex[left];
            _nodeImages[leftUI].color = new Color(0.6f, 1f, 0.6f);
        }

        if (right < heapSize)
        {
            int rightUI = _uiIndexAtHeapIndex[right];
            _nodeImages[rightUI].color = new Color(0.6f, 1f, 0.6f);
        }

        if (swapNeeded)
        {
            // children clickable
            if (left < heapSize)
            {
                int heapIndex = left;
                int uiIndex = _uiIndexAtHeapIndex[heapIndex];
                _nodeButtons[uiIndex].interactable = true;
                _nodeButtons[uiIndex].onClick.AddListener(() => OnNodeClicked(heapIndex));
            }
            if (right < heapSize)
            {
                int heapIndex = right;
                int uiIndex = _uiIndexAtHeapIndex[heapIndex];
                _nodeButtons[uiIndex].interactable = true;
                _nodeButtons[uiIndex].onClick.AddListener(() => OnNodeClicked(heapIndex));
            }
        }
        else
        {
            // only parent clickable to confirm
            int heapIndex = currentParent;
            int uiIndex = _uiIndexAtHeapIndex[heapIndex];
            _nodeButtons[uiIndex].interactable = true;
            _nodeButtons[uiIndex].onClick.AddListener(() => OnNodeClicked(heapIndex));
        }
    }

    void OnNodeClicked(int index)
    {

        if (tutorialActive)
            return;

        if (heapCompleted)
            return;

        if (blockInputDuringSwap && _isSwapping)
            return;

        int left = 2 * currentParent + 1;
        int right = 2 * currentParent + 2;

        int largest = currentParent;

        if (left < heapSize && heapManager.heap[left] > heapManager.heap[largest])
            largest = left;

        if (right < heapSize && heapManager.heap[right] > heapManager.heap[largest])
            largest = right;

        bool swapNeeded = (largest != currentParent);

        // --------------------
        // No swap needed
        // --------------------
        if (!swapNeeded)
        {
            if (index == currentParent)
            {
                currentParent--;

                if (currentParent < 0)
                {
                    CompleteHeap();
                    return;
                }

                statusText.text = "Correct. This page is already properly placed. Moving on to the next. If there is a higher page number below the orange page, select it. Otherwise confirm by selecting the orange page.";
                RefreshUI();
            }
            else
            {
                statusText.text = "No swap needed. Confirm by selecting the orange script.";
                ApplyHighlightAndClicks();
            }

            return;
        }

        // --------------------
        // Swap needed
        // --------------------
        if (index == largest)
        {
            // Animate swap between currentParent and largest
            StartCoroutine(SwapAndContinue(currentParent, largest));
        }
        else
        {
            statusText.text = "Look carefully. Which page below has the higher page number?";
        }
    }

    IEnumerator SwapAndContinue(int a, int b)
    {
        _isSwapping = true;
        ApplyHighlightAndClicks();

        // UI indices for those heap indices
        int uiA = _uiIndexAtHeapIndex[a];
        int uiB = _uiIndexAtHeapIndex[b];

        RectTransform rectA = _nodeRects[uiA];
        RectTransform rectB = _nodeRects[uiB];

        Vector2 startA = rectA.anchoredPosition;
        Vector2 startB = rectB.anchoredPosition;

        Vector2 targetA = GetNodePosition(b);
        Vector2 targetB = GetNodePosition(a);

        float t = 0f;
        float dur = Mathf.Max(0.01f, swapAnimDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float eased = useEaseInOut ? EaseInOut(t) : t;

            rectA.anchoredPosition = Vector2.Lerp(startA, targetA, eased);
            rectB.anchoredPosition = Vector2.Lerp(startB, targetB, eased);

            yield return null;
        }

        rectA.anchoredPosition = targetA;
        rectB.anchoredPosition = targetB;

        // Swap heap VALUES (this is the actual algorithm swap)
        int temp = heapManager.heap[a];
        heapManager.heap[a] = heapManager.heap[b];
        heapManager.heap[b] = temp;

        // Swap UI mapping so future highlights/clicks follow the heap indices
        int tempUI = _uiIndexAtHeapIndex[a];
        _uiIndexAtHeapIndex[a] = _uiIndexAtHeapIndex[b];
        _uiIndexAtHeapIndex[b] = tempUI;

        // Continue heapify down
        currentParent = b;

        statusText.text = "Correct. The page with the higher page number moves up. Now check the replaced page again. If there is a higher page number below the orange page, select it. Otherwise confirm by selecting the orange page.";

        _isSwapping = false;
        RefreshUI();
    }

    float EaseInOut(float x)
    {
        x = Mathf.Clamp01(x);
        // smoothstep
        return x * x * (3f - 2f * x);
    }

    void CompleteHeap()
    {
        heapCompleted = true;

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);

            CanvasGroup cg = completionPanel.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = 1f;
        }

        if (proceedPanel != null)
            proceedPanel.SetActive(false);

        statusText.text = "All scripts are organized. Give them to Faith.";
        ApplyHighlightAndClicks();

        // 🔥 Auto move to proceed panel after 3 seconds
        StartCoroutine(ShowProceedAfterDelay(3f));
    }

    IEnumerator ShowProceedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (completionPanel != null)
            completionPanel.SetActive(false);

        if (proceedPanel != null)
            proceedPanel.SetActive(true);
    }

    public void ShowProceedPanel()
    {
        if (completionPanel != null)
            completionPanel.SetActive(false);

        if (proceedPanel != null)
            proceedPanel.SetActive(true);
    }

    public void OnReplayGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void CloseTutorial()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        tutorialActive = false;

        statusText.text = "Compare the pages of scripts. If there is a higher page number below the orange page, select it. If not, confirm by selecting the orange page.";

        RefreshUI();
    }

    public void OnGiveScripts()
    {
        SceneManager.LoadScene("05_After_TrueHeap");
    }

    void UpdateStatus()
    {
        if (heapCompleted)
            statusText.text = "All scripts are organized!";
        else
            statusText.text = "Compare the pages of scripts. If there is a higher page number below the orange page, select it. If not, confirm by selecting the orange page.";
    }
}