using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class QuickSortManager : MonoBehaviour
{
    [Header("Zones")]
    public Transform propsContainer;
    public Transform leftZone;
    public Transform pivotZone;
    public Transform rightZone;

    public GameObject propPrefab;
    public PropDatabase propDatabase;

    [Header("UI")]
    public GameObject classificationPanel;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI stageText;

    [Header("Completion UI")]
    public CanvasGroup completionCanvasGroup;
    public TextMeshProUGUI finalTimeText;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    [Header("Proceed UI")]
    public GameObject proceedPanel;
    public CanvasGroup proceedCanvasGroup;

    private bool sortingComplete = false;

    // ✅ NEW UI STATE FLAGS
    private bool completionVisible = false;
    private bool proceedVisible = false;

    private List<int> numbers = new List<int>();
    private Stack<(int low, int high)> rangeStack = new Stack<(int, int)>();

    private int currentLow;
    private int currentHigh;

    private int pivotValue;
    private int pivotIndex;

    private PropItem currentPivot;
    private PropItem selectedItem;

    private List<int> leftPartition = new List<int>();
    private List<int> rightPartition = new List<int>();

    private float timer = 0f;
    private bool timerRunning = false;

    void Start()
    {
        classificationPanel.SetActive(false);

        if (completionCanvasGroup != null)
        {
            completionCanvasGroup.alpha = 0f;
            completionCanvasGroup.interactable = false;
            completionCanvasGroup.blocksRaycasts = false;
        }

        numbers = GenerateUniqueNumbers(6, 1, 20);

        rangeStack.Push((0, numbers.Count - 1));

        StartCoroutine(ProcessNextRange());

        if (proceedCanvasGroup != null)
        {
            proceedCanvasGroup.alpha = 0f;
            proceedCanvasGroup.interactable = false;
            proceedCanvasGroup.blocksRaycasts = false;
        }

        if (proceedPanel != null)
            proceedPanel.SetActive(false);
    }

    List<int> GenerateUniqueNumbers(int count, int min, int max)
    {
        HashSet<int> set = new HashSet<int>();

        while (set.Count < count)
        {
            set.Add(Random.Range(min, max));
        }

        return new List<int>(set);
    }

    void Update()
    {
        if (timerRunning)
        {
            timer += Time.deltaTime;
            if (timerText != null)
                timerText.text = timer.ToString("F2");
        }

        if (!sortingComplete || Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            // FIRST PRESS → show Proceed Panel
            if (completionVisible && !proceedVisible)
            {
                ShowProceedPanel();
            }
            // SECOND PRESS → continue
            else if (proceedVisible)
            {
                ProceedToNextScene();
            }
        }
    }

    IEnumerator ProcessNextRange()
    {
        if (rangeStack.Count == 0)
        {
            sortingComplete = true;
            timerRunning = false;

            ClearZones();
            SpawnFullArray();

            stageText.text = "Everything is finally in the right place. Sort Complete!";

            ShowCompletionPanel();

            yield break;
        }

        var range = rangeStack.Pop();
        currentLow = range.low;
        currentHigh = range.high;

        if (currentLow >= currentHigh)
        {
            yield return StartCoroutine(ProcessNextRange());
            yield break;
        }

        ClearZones();

        int groupSize = currentHigh - currentLow + 1;
        stageText.text = $"We’re focusing on a group of {groupSize} props. Choose one to guide the rest.";

        SpawnCurrentSubarray();

        timerRunning = true;
    }

    void SpawnCurrentSubarray()
    {
        float spacing = 220f;
        int count = currentHigh - currentLow + 1;
        float totalWidth = (count - 1) * spacing;

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(propPrefab, propsContainer);
            RectTransform rect = obj.GetComponent<RectTransform>();

            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(200, 200);

            float xPos = (i * spacing) - (totalWidth / 2f);
            rect.anchoredPosition = new Vector2(xPos, 0);

            Sprite randomSprite = propDatabase.propSprites[
                Random.Range(0, propDatabase.propSprites.Count)
            ];

            obj.GetComponent<PropItem>().Initialize(
                numbers[currentLow + i],
                randomSprite
            );
        }
    }

    void SpawnFullArray()
    {
        foreach (Transform child in propsContainer)
            Destroy(child.gameObject);

        float spacing = 220f;
        float totalWidth = (numbers.Count - 1) * spacing;

        for (int i = 0; i < numbers.Count; i++)
        {
            GameObject obj = Instantiate(propPrefab, propsContainer);
            RectTransform rect = obj.GetComponent<RectTransform>();

            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(200, 200);

            float xPos = (i * spacing) - (totalWidth / 2f);
            rect.anchoredPosition = new Vector2(xPos, 0);

            Sprite randomSprite = propDatabase.propSprites[
                Random.Range(0, propDatabase.propSprites.Count)
            ];

            obj.GetComponent<PropItem>().Initialize(
                numbers[i],
                randomSprite
            );
        }
    }

    public void OnPropClicked(PropItem item)
    {
        if (sortingComplete)
            return;

        if (currentPivot == null)
        {
            SetPivot(item);
        }
        else if (item != currentPivot)
        {
            selectedItem = item;
            classificationPanel.SetActive(true);
        }
    }

    void SetPivot(PropItem item)
    {
        currentPivot = item;
        pivotValue = item.value;

        item.transform.SetParent(pivotZone, false);

        RectTransform rect = item.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(200, 200);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image img = item.GetComponent<Image>();
        if (img != null)
            img.color = new Color(1f, 0.8f, 0.2f);

        leftPartition.Clear();
        rightPartition.Clear();

        stageText.text = "Now compare the others to this one.";
    }

    public void ChooseLess()
    {
        if (selectedItem == null) return;

        if (selectedItem.value < pivotValue)
        {
            PlaceItem(selectedItem, leftZone, leftPartition);
        }
    }

    public void ChooseGreater()
    {
        if (selectedItem == null) return;

        if (selectedItem.value > pivotValue)
        {
            PlaceItem(selectedItem, rightZone, rightPartition);
        }
    }

    void PlaceItem(PropItem item, Transform zone, List<int> targetList)
    {
        item.transform.SetParent(zone, false);
        targetList.Add(item.value);

        classificationPanel.SetActive(false);
        selectedItem = null;

        CheckPartitionComplete();
    }

    void CheckPartitionComplete()
    {
        if (propsContainer.childCount == 0)
        {
            StartCoroutine(PartitionCompleteSequence());
        }
    }

    IEnumerator PartitionCompleteSequence()
    {
        timerRunning = false;

        stageText.text = "Good. Now this group is organized. Time to fix another one.";
        yield return new WaitForSeconds(1f);

        int index = currentLow;

        foreach (int val in leftPartition)
            numbers[index++] = val;

        numbers[index] = pivotValue;
        pivotIndex = index;
        index++;

        foreach (int val in rightPartition)
            numbers[index++] = val;

        rangeStack.Push((pivotIndex + 1, currentHigh));
        rangeStack.Push((currentLow, pivotIndex - 1));

        yield return new WaitForSeconds(1f);

        StartCoroutine(ProcessNextRange());
    }

    void ClearZones()
    {
        foreach (Transform child in propsContainer)
            Destroy(child.gameObject);

        foreach (Transform child in leftZone)
            Destroy(child.gameObject);

        foreach (Transform child in pivotZone)
            Destroy(child.gameObject);

        foreach (Transform child in rightZone)
            Destroy(child.gameObject);

        currentPivot = null;
    }

    void ShowCompletionPanel()
    {
        if (finalTimeText != null)
            finalTimeText.text = "Final Time: " + timer.ToString("F2") + "s";

        completionVisible = true;

        StartCoroutine(FadeInCompletion());
    }

    IEnumerator FadeInCompletion()
    {
        float elapsed = 0f;

        completionCanvasGroup.alpha = 0f;
        completionCanvasGroup.interactable = false;
        completionCanvasGroup.blocksRaycasts = false;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            completionCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        completionCanvasGroup.alpha = 1f;
        completionCanvasGroup.interactable = true;
        completionCanvasGroup.blocksRaycasts = true;
    }

    void ShowProceedPanel()
    {
        proceedVisible = true;
        proceedPanel.SetActive(true);
        StartCoroutine(FadeInProceedPanel());
    }

    public void ReplayLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitLevel()
    {
        SceneManager.LoadScene("07_Ending");
    }

    IEnumerator FadeInProceedPanel()
    {
        float elapsed = 0f;

        proceedCanvasGroup.alpha = 0f;
        proceedCanvasGroup.interactable = false;
        proceedCanvasGroup.blocksRaycasts = false;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            proceedCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        proceedCanvasGroup.alpha = 1f;
        proceedCanvasGroup.interactable = true;
        proceedCanvasGroup.blocksRaycasts = true;
    }

    void ProceedToNextScene()
    {
        SceneManager.LoadScene("06_After_QuickSort");
    }
}