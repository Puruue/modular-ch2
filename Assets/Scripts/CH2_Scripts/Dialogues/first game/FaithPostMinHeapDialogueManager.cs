using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class FaithPostMinHeapDialogueManager : MonoBehaviour
{
    public DialogueManager dialogueManager;

    void Start()
    {
        DialogueLine[] afterLines = {

            new DialogueLine("Faith", "Faith_cropped", "That... actually worked."),
            new DialogueLine("Faith", "Faith_cropped", "Once the lightest box was on top, everything felt less unstable."),
            new DialogueLine("Mouse", "Neautral_MOUSE", "Because you followed a rule."),
            new DialogueLine("Faith", "Faith_cropped", "I didn’t try to fix everything at once."),
            new DialogueLine("Mouse", "Excited_MOUSE", "And when each small part is correct..."),
            new DialogueLine("Mouse", "Excited_MOUSE", "The whole structure becomes correct."),
            new DialogueLine("Faith", "Faith_cropped", "So structure isn’t about controlling everything."),
            new DialogueLine("Mouse", "Neautral_MOUSE", "Now imagine if instead we prioritize the largest first."),
            new DialogueLine("Faith", "Faith_cropped", "Let the heaviest take control?"),
            new DialogueLine("Mouse", "Excited_MOUSE", "That will be called a max-heap."),
            new DialogueLine("Faith", "Faith_cropped", "Alright then. Let’s try organizing that way next.")
        };

        dialogueManager.StartDialogue(afterLines);
        StartCoroutine(WaitForDialogueEnd());
    }

    IEnumerator WaitForDialogueEnd()
    {
        while (dialogueManager.IsDialogueActive())
            yield return null;

        // Small pause for pacing (optional)
        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene("04_True_Heap");
    }
}