using UnityEngine;

public class AssessmentUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;            // Keep this as AssessmentUIRoot GameObject (your current setup)
    public AssessmentUIRoot uiRoot;    // The script on AssessmentUIRoot

    [Header("Music")]
    public AssessmentMusicOverride musicOverride;

    public void OpenAssessment()
    {
        if (root != null)
            root.SetActive(true);

        if (musicOverride != null)
            musicOverride.StartAssessmentMusic();

        if (uiRoot != null)
            uiRoot.BeginAssessmentFromTrigger();
    }

    // Optional manual close (if you add a close button)
    public void CloseAssessmentManually()
    {
        if (uiRoot != null)
            uiRoot.CloseAndRestore();

        StopAssessmentMusicOnly();

        if (root != null)
            root.SetActive(false);
    }

    // ✅ used by AssessmentUIRoot on finish (safe, no disabling here)
    public void StopAssessmentMusicOnly()
    {
        if (musicOverride != null)
            musicOverride.StopAssessmentMusic();
    }
}
