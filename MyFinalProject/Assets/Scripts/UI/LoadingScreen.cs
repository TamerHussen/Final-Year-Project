using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreen : MonoBehaviour
{
    private const string LoadingSceneName = "LoadingScene";

    private static string targetScene = "";

    public static string TargetScene => targetScene;

    public static void LoadScene(string sceneName)
    {
        targetScene = sceneName;
        SceneManager.LoadScene(LoadingSceneName);
    }

}
