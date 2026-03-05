using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class QSUIManager : MonoBehaviour
{
    public DialogueManager dialogueManager;

    [Header("First Choice")]
    public GameObject choiceAButton;
    public GameObject choiceBButton;

    [Header("Proceed Panel")]
    public GameObject proceedPanel;

    public GameObject continueButton;

    private bool firstChoicesShown = false;
    private bool waitingForProceed = false;

    void Start()
    {
        // Hide UI at start
        choiceAButton.SetActive(false);
        choiceBButton.SetActive(false);

        if (proceedPanel != null)
            proceedPanel.SetActive(false);

        // Intro reflection dialogue
        DialogueLine[] reflectionLines =
        {
            new DialogueLine("Mouse", "Mouse_Neutral",
                "Looks like she managed to get through organizing those props."),

            new DialogueLine("Mouse", "Mouse_Excited",
                "That should help her stay focused for the audition.")
        };

        dialogueManager.StartDialogue(reflectionLines);
    }

    void Update()
    {
        if (dialogueManager == null) return;

        string currentText = dialogueManager.dialogueText.text;

        // Trigger first choices after the last intro line
        if (!firstChoicesShown && currentText.Contains("That should help her stay focused for the audition."))
        {
            firstChoicesShown = true;

            continueButton.SetActive(false);

            choiceAButton.SetActive(true);
            choiceBButton.SetActive(true);
        }

        // Detect the final dialogue line from either branch
        if (!waitingForProceed)
        {
            if (currentText.Contains("She looks a little more confident now.") ||
                currentText.Contains("Let's see how things turn out for her."))
            {
                waitingForProceed = true;
            }
        }

        // Player presses E to show proceed panel
        if (waitingForProceed && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ShowProceedPanel();
        }
    }

    void ShowProceedPanel()
    {
        if (proceedPanel != null)
            proceedPanel.SetActive(true);

        waitingForProceed = false;
    }

    // Proceed button → Next scene
    public void GoToNextScene()
    {
        SceneManager.LoadScene("07_AfterQuickSort"); 
        // change this to whatever your next scene is
    }

    // Replay button → reload current scene
    public void ReplayScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}