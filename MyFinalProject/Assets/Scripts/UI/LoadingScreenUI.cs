using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScreenUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider progressBar;
    public Text loadingText;
    public Text percentageText;
    public Image backgroundImage;

    [Header("Tips")]
    public string[] tips =
    {
        "Crouch in bushes to mask your scent trail.",
        "Stay still while hiding - moving makes sounds.",
        "The predator summons aerial familairs from above. Look up.",
        "The ML predator learns from experience.",
        "An exit apears after a certain amount of time.",
        "hello",
        "No tips for you.",
        "The game dev is very handsome.",
        "If you look down you can see, you have legs.",
        "If you look up you can see the word gullible.",
        ":)",
        "System Error , Device too good :(",
        "Predator feels bad so he gave you a headstart."
    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (progressBar != null) progressBar.value = 0f;
        if (percentageText != null) percentageText.text = "0%";

        if (loadingText != null)
            loadingText.text = tips[Random.Range(0, tips.Length)];

        string target = LoadingScreen.TargetScene;

        if (string.IsNullOrEmpty(target))
        {
            Debug.LogWarning("[LoadingScreenUI] no target scene set");
            target = "GameScene";
        }

        StartCoroutine(LoadAsync(target));
    }

    IEnumerator LoadAsync(string sceneName)
    {
        yield return new WaitForSeconds(0.1f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float displayProgress = 0f;
        float realProgress = 0f;
        float elapsed = 0f;
        float minimumTime = 2.5f;

        while (realProgress < 1f || elapsed < minimumTime)
        {
            elapsed += Time.unscaledDeltaTime;

            realProgress = Mathf.Clamp01(operation.progress / 0.9f);

            displayProgress = Mathf.Lerp(displayProgress, realProgress, Time.unscaledDeltaTime * 1.8f);

            if (elapsed < minimumTime)
                displayProgress = Mathf.Min(displayProgress, 0.95f);

            if (progressBar != null) progressBar.value = displayProgress;
            if (percentageText != null) percentageText.text = $"{Mathf.RoundToInt(displayProgress * 100f)}%";

            yield return null;

        }
        // fully loaded
        if (progressBar != null) progressBar.value = 1f;
        if (percentageText != null) percentageText.text = "100%";

        yield return new WaitForSeconds(0.2f);
        operation.allowSceneActivation = true;
    }
}
