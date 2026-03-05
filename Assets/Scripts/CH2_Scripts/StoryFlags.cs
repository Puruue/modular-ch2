using UnityEngine;

public enum Route
{
    Neutral,
    Good,
    Bad
}

public class StoryFlags : MonoBehaviour
{
    public static StoryFlags instance;

    public Route currentRoute = Route.Neutral;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}