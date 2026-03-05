using UnityEngine;

public class EndingManager : MonoBehaviour
{
    public DialogueManager dialogueManager;

    void Start()
    {
        if (StoryFlags.instance == null)
        {
            Debug.LogError("StoryFlags missing!");
            return;
        }

        if (StoryFlags.instance.currentRoute == Route.Good)
        {
            PlayGoodEnding();
        }
        else
        {
            PlayBadEnding();
        }
    }

    void PlayGoodEnding()
    {
        DialogueLine[] goodEnding =
        {
            new DialogueLine("Faith", "Sad_Faith", "Whew..."),
            new DialogueLine("Faith", "Faith_cropped", "Thank you for helping with all of this today."),

            new DialogueLine("Mouse", "Mouse_Neutral", "You look less stressed."),

            new DialogueLine("Faith", "Sad_Faith", "I think I am."),

            new DialogueLine("Faith", "Faith_cropped",
            "When my agent told me about this role... I thought this was finally it."),

            new DialogueLine("Faith", "Faith_cropped",
            "But the place feels like home now."),

            new DialogueLine("Faith", "Surprised_Faith",
            "And the script doesn't feel so scary anymore."),

            new DialogueLine("Mouse", "Mouse_Excited", "Go get that role."),

            new DialogueLine("Faith", "Faith_cropped", "I will."),

            new DialogueLine("", "", "A few weeks later..."),

            new DialogueLine("", "",
            "News headline: 'Breakout Actress Faith Castillo Steals the Show in Upcoming Film.'"),

            new DialogueLine("", "", "\"Leap of Faith!\"")
        };

        dialogueManager.StartDialogue(goodEnding);
    }

    void PlayBadEnding()
    {
        DialogueLine[] badEnding =
        {
            new DialogueLine("Faith", "Sad_Faith", "Thanks for helping today."),

            new DialogueLine("Mouse", "Mouse_Neutral", "You don't sound too happy."),

            new DialogueLine("Faith", "Sad_Faith", "I just... I don't think I'm ready."),

            new DialogueLine("Mouse", "Mouse_Neutral", "Ready for what?"),

            new DialogueLine("Faith", "Faith_Sad", "The audition."),

            new DialogueLine("Faith", "Faith_Sad",
            "Everything happened too fast. Moving here. Memorizing lines."),

            new DialogueLine("Faith", "Faith_cropped",
            "Maybe I grabbed the opportunity too quickly."),

            new DialogueLine("", "", "A week later..."),

            new DialogueLine("", "",
            "Message: 'Thank you for auditioning. We regret to inform you...'"),

            new DialogueLine("Faith", "Faith_Sad", "...I knew it.")

        };

        dialogueManager.StartDialogue(badEnding);
    }
}