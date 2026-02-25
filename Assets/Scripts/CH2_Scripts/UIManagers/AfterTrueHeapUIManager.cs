using UnityEngine;

public class AfterTrueHeapUIManager : MonoBehaviour
{
    public DialogueManager dialogueManager;

    [Header("Proceed UI")]
    public GameObject proceedPanel;

    void Start()
    {
        if (proceedPanel != null)
            proceedPanel.SetActive(false);

        DialogueLine[] reflectionLines = {

            new DialogueLine("Faith", "Faith_cropped",
                "So... the structure changes depending on what we prioritize."),

            new DialogueLine("Mouse", "Neautral_MOUSE",
                "Exactly."),

            new DialogueLine("Faith", "Faith_cropped",
                "So organizing isn’t about instinct."),

            new DialogueLine("Faith", "Faith_cropped",
                "It’s about choosing a structure and committing to its rules that it makes the process of organizing smoother."),

            new DialogueLine("Mouse", "Excited_MOUSE",
                "And now that you’ve seen both min-heap and max-heap..."),

            new DialogueLine("Mouse", "Excited_MOUSE",
                "Let’s see if you really understand them."),

            new DialogueLine("", "",
                "Faith crosses her arms, waiting."),

            new DialogueLine("Faith", "Faith_cropped",
                "Go on then."),

            new DialogueLine("Mouse", "Neautral_MOUSE",
                "Time for a quick assessment.")
        };

        dialogueManager.StartDialogue(reflectionLines);

        StartCoroutine(WaitForDialogueEnd());
    }

    System.Collections.IEnumerator WaitForDialogueEnd()
    {
        while (dialogueManager != null && dialogueManager.IsDialogueActive())
            yield return null;

        if (proceedPanel != null)
            proceedPanel.SetActive(true);
    }
}