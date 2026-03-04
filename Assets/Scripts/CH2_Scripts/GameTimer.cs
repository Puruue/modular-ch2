using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timeText;        // Optional: live timer
    public TextMeshProUGUI completionTimeText;
    public TextMeshProUGUI bestTimeText;

    [Header("Settings")]
    public string levelID = "IntroHeap";    // Change per scene

    private float startTime;
    private bool timerRunning = false;

    void Start()
    {
        StartTimer();
    }

    void Update()
    {
        if (timerRunning && timeText != null)
        {
            float currentTime = Time.time - startTime;
            timeText.text = "Time: " + FormatTime(currentTime);
        }
    }

    public void StartTimer()
    {
        startTime = Time.time;
        timerRunning = true;
    }

    public void StopTimer()
    {
        if (!timerRunning) return;

        timerRunning = false;

        float finalTime = Time.time - startTime;

        // Show final time
        if (completionTimeText != null)
            completionTimeText.text = "Time: " + FormatTime(finalTime);

        // Handle best time
        string key = "BestTime_" + levelID;
        float bestTime = PlayerPrefs.GetFloat(key, Mathf.Infinity);

        if (finalTime < bestTime)
        {
            PlayerPrefs.SetFloat(key, finalTime);
            bestTime = finalTime;
        }

        if (bestTimeText != null)
            bestTimeText.text = "Best: " + FormatTime(bestTime);
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 100) % 100);

        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }
}