using UnityEngine;

public class CharacterSelectionButtons : MonoBehaviour
{
    [Header("Scene Names")]
    public string backScene = "02_CharacterCustomization";
    public string chooseScene = "04_gamehub";

    public void Back()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(backScene);
    }

    public void Choose()
    {
        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(chooseScene);
    }
}
