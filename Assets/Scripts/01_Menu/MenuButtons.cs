using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    [Header("Scene Names")]
    public string characterCustomizationScene = "02_CharacterCustomization";

    [Header("Settings UI")]
    [Tooltip("Root GameObject of the Settings UI")]
    public GameObject settingsUI;

    [Header("Load Game UI")]
    [Tooltip("Root GameObject of the Load Game UI")]
    public GameObject loadGameUI;

    [Header("Optional UI Scripts (recommended)")]
    [Tooltip("Assign if your Load menu uses the LoadMenuUI script (Load-only).")]
    public LoadMenuUI loadMenuScript;

    [Tooltip("Assign if your Settings UI uses a controller script with OpenMenu().")]
    public SaveLoadMenuUI saveLoadMenuScript;

    [Header("New Game Reset")]
    [Tooltip("If true, clears the introPlayed flag so the intro can play again in a fresh run.")]
    public bool resetIntroPlayedOnNewGame = false;

    private const string PREF_INTRO_PLAYED = "introPlayed";

    public void NewGame()
    {
        // ✅ FIX: Clear runtime objective progress so returning to menu doesn't keep old objectives.
        if (ObjectiveManager.Instance != null)
        {
            // Using default profile unless you later implement per-slot new game.
            ObjectiveManager.Instance.ResetForNewGame("default");
        }

        // Optional: reset intro flag if you want a true "fresh start"
        if (resetIntroPlayedOnNewGame)
        {
            PlayerPrefs.DeleteKey(PREF_INTRO_PLAYED);
            PlayerPrefs.Save();
        }

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(characterCustomizationScene);
        else
            Debug.LogWarning("MenuButtons: SceneTransition.Instance is missing.");
    }

    public void LoadGame()
    {
        // ✅ Preferred path: call the script that actually builds rows and pulls save data.
        if (loadMenuScript != null)
        {
            loadMenuScript.Open();
            return;
        }

        // ✅ Fallback: if you're still using SaveLoadMenuUI for load-only
        if (saveLoadMenuScript != null)
        {
            saveLoadMenuScript.OpenLoadOnly();
            return;
        }

        // ⚠️ Last resort: only enables the object (may NOT build rows depending on your script)
        if (loadGameUI == null)
        {
            Debug.LogWarning("MenuButtons: Load Game UI is not assigned.");
            return;
        }

        loadGameUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OpenSettings()
    {
        // ✅ If you have a script controlling open/close, use it.
        if (saveLoadMenuScript != null)
        {
            saveLoadMenuScript.OpenMenu();
            return;
        }

        if (settingsUI == null)
        {
            Debug.LogWarning("MenuButtons: Settings UI is not assigned.");
            return;
        }

        settingsUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Quit()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.QuitGame();
        else
            Application.Quit();
    }
}
