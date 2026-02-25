using UnityEngine;
using UnityEngine.SceneManagement;

public class OpeningUIManager : MonoBehaviour
{
    public DialogueManager dialogueManager;

    void Start()
    {
        DialogueLine[] openingLines = {

            new DialogueLine("Mouse", "Excited_MOUSE",
                "Woahhh.. This looks entirely different from the Lizzy's world. It's all... bright and pastel."),

            new DialogueLine("Mouse", "Neautral_MOUSE",
                "In Lizzy's world we organized memories and gave them to Lizzy with selection sort."),

            new DialogueLine("Mouse", "Excited_MOUSE",
                "What could be our next encounter in this world? Let's go roam around!"),

            new DialogueLine("???", "Faith_sil",
                "So this is it."),

            new DialogueLine("", "",
                "You look around for the source of the voice."),

            new DialogueLine("???", "Faith_sil",
                "My own place. A new start."),

            new DialogueLine("Mouse", "SURPRISED_MOUSE",
                "Who is that?"),

            new DialogueLine("???", "Faith_sil",
                "I almost didn’t take the key."),

            new DialogueLine("Mouse", "Neautral_MOUSE",
                "What key?"),

            new DialogueLine("???", "Faith_sil",
                "!?"),

            new DialogueLine("???", "Faith_sil",
                "And you guys are?"),

            new DialogueLine("", "",
                "You and Mouse introduced yourselves, and that you came from a different world too."),

            new DialogueLine("???", "Faith_sil",
                "Heh. As if!"),

            new DialogueLine("Mouse", "SAD_MOUSE",
                "You don't believe us? We're telling the truth! We just came from a whole gloomy world and this robot girl-"),

            new DialogueLine("???", "Faith_sil",
                "No need to dig yourself in too deep, fellas. I was not born yesterday."),

            new DialogueLine("Faith", "Faith_cropped",
                "I'm Faith."),

            new DialogueLine("Faith", "Faith_cropped",
                "You guys are the helpers right? The one my agency hired to help organize my stuff."),

            new DialogueLine("", "",
                "You and Mouse look at each other."),

            new DialogueLine("Mouse", "Neautral_MOUSE",
                "This girl don't seem to care about who we are."),
            
            new DialogueLine("Faith", "Faith_cropped",
                "I do not have time to know about everyone's backstories here. I am already quite overwhelmed moving into a new place."),

            new DialogueLine("Faith", "Faith_cropped",
                "Plus, that play that my agent just thrown me into."),

            new DialogueLine("Faith", "Faith_cropped",
                "If it weren't such a good look on me to take the role of Beth, I would not have taken the bait."),

            new DialogueLine("Faith", "Faith_cropped",
                "Now, I have to learn my script fast and adjust into this place. Hectic!"),

            new DialogueLine("Mouse", "SURPRISED_MOUSE",
                "You're an actress??"),

            new DialogueLine("Faith", "Faith_cropped",
                "Surprised? I have yet to get my break into the forefront of the industry. That's why when my agent pitched this role as an emergency due to the original actress being injured..."),

            new DialogueLine("Faith", "Faith_cropped",
                "In no way would I let this chance slip away!"),

            new DialogueLine("", "",
                "Awed by Faith's determination, you stood in silence. Eyes fixated on her."),

            new DialogueLine("Faith", "Faith_cropped",
                "What are you guys standing there for? You guys weren't hired to just pose there!"),

            new DialogueLine("Mouse", "Neautral_MOUSE",
                "I guess we have no choice but to go along with her, pal."),

            new DialogueLine("", "",
                "You nod"),

            new DialogueLine("Mouse", "Neautral_MOUSE",
                "Let's help her and get out of here."),

            new DialogueLine("Mouse", "Neautral_MOUSE",
                "Same as Lizzy's world."),

            new DialogueLine("Faith", "Faith_cropped",
                "Chop chop!"),

            new DialogueLine("Mouse", "SURPRISED_MOUSE",
                "ALRIGHT! ALRIGHT!"),

            new DialogueLine("Mouse", "SAD_MOUSE",
                "GEEZ!"),

            new DialogueLine("Faith", "Faith_cropped",
                "Let's start with the boxes at the living room.")

        };

        dialogueManager.StartDialogue(openingLines);

        StartCoroutine(WaitForDialogueEnd());
    }

    System.Collections.IEnumerator WaitForDialogueEnd()
    {
        while (dialogueManager.IsDialogueActive())
            yield return null;

        SceneManager.LoadScene("03_Intro_Game");
    }
}