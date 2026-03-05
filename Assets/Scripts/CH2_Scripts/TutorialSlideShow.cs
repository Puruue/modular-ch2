using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TutorialSlideshow : MonoBehaviour
{
    public GameObject[] pages;

    public GameObject backButton;
    public GameObject nextButton;
    public GameObject playButton;

    public GameObject heapMinigame;

    private int currentPage = 0;

    void Start()
    {
        ShowPage(0);
    }

    void Update()
    {
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            GoBackScene();
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentPage == pages.Length - 1)
                StartGame();
            else
                NextPage();
        }

        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            PreviousPage();
        }
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

        // Back button hidden on first page
        backButton.SetActive(index > 0);

        nextButton.SetActive(index < pages.Length - 1);
        playButton.SetActive(index == pages.Length - 1);
    }

    public void StartGame()
    {
        heapMinigame.SetActive(true);
        gameObject.SetActive(false);
    }

    public void GoBackScene()
    {
        SceneManager.LoadScene("02_Faith_Opening");
    }
}