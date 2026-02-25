using UnityEngine;

public class TutorialSlideshow : MonoBehaviour
{
    public GameObject[] pages;

    public GameObject backButton;
    public GameObject nextButton;
    public GameObject playButton;

    public GameObject heapMinigame;   // 👈 ADD THIS

    private int currentPage = 0;

    void Start()
    {
        ShowPage(0);
    }

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }

    void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }

        backButton.SetActive(index > 0);
        nextButton.SetActive(index < pages.Length - 1);
        playButton.SetActive(index == pages.Length - 1);
    }

    public void StartGame()
    {
        heapMinigame.SetActive(true);  // 👈 Direct reference
        gameObject.SetActive(false);   // Hide tutorial
    }
}