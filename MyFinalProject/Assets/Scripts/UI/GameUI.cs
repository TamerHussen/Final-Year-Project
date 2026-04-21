using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Stamina")]
    public Slider staminaBar;
    public Image staminaFill;
    public Color staminaFull = new Color(0.2f, 0.8f, 0.3f);
    public Color staminaLow = new Color(0.9f, 0.2f, 0.1f);

    [Header("Stress Overlay")]
    public Image stressVignette;
    public float maxVignetteAlpha = 0.7f;

    [Header("Heartbeat")]
    public AudioSource heartbeatSource;
    public AudioClip[] heartbeatClip;
    public float maxHeartbeatVolume = 0.9f;
    public float heartbeatNearDistance = 8f; // loud when predator close
    public float heartbeatFarDistance = 25f; // quiet when predator far

    [Header("Stress Vignette Range")]
    public float vignetteNearDistance = 5f;
    public float vignetteFarDistance = 45f;
    private float heartbeatTimer = 0f;
    private float currentHeartRate = 1.5f;

    [Header("Timer")]
    public Text timerText; // how long needed to survive
    public Text timerLabel;

    [Header("Notifications")]
    public GameObject escapeNotification; // exit apeared
    public Text escapeNotifText;
    public float notifDisplayTime = 4f;
    private float notifTimer = 0f;
    private bool notifActive = false;

    [Header("Game Over Screen")]
    public GameObject gameOverScreen;
    public Text gameOverTitle;
    public Text survivalTimeText;
    public Button restartButton;
    public Button menuButton;

    [Header("Escape Screen")]
    public GameObject escapeScreen;
    public Text escapeTimeText;
    public Button escapeRestartButton;
    public Button escapeMenuButton;

    [Header("Headstart")]
    public GameObject headstartPanel;
    public Text headstartText;

    [Header("Ammo UI")]
    public Text ammoText;

    private Transform predatorTransform;
    private Transform playerTransform;

    private float currentStamina = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // find predator dynamically
        GameObject predObj = GameObject.FindGameObjectWithTag("Predator");
        if (predObj != null) predatorTransform = predObj.transform;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        // buttons
        restartButton?.onClick.AddListener(() => GameManager.instance.RestartScene());
        menuButton?.onClick.AddListener(() => GameManager.instance.GoToMainMenu());
        escapeRestartButton?.onClick.AddListener(() => GameManager.instance.RestartScene());
        escapeMenuButton?.onClick.AddListener(() => GameManager.instance.GoToMainMenu());

        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (escapeScreen != null) escapeScreen.SetActive(false);
        if (escapeNotification != null) escapeNotification.SetActive(false);

        // stress vignette
        if (stressVignette != null)
        {
            Color c = stressVignette.color;
            c.a = 0f;
            stressVignette.color = c;
        }

        // heartbeat
        if (heartbeatSource != null && heartbeatClip != null)
        {
            heartbeatSource.loop = false;
            heartbeatSource.volume = 0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance == null || !GameManager.instance.SessionActive) return;

        UpdateProximityEffects();
        UpdateHeartbeat();
        UpdateNotification();
    }

    public void UpdateAmmoUI(int current, int max)
    {
        if (ammoText != null)
        {
            ammoText.text = "Rocks: " + current + " / " + max;
        }
    }

    public void UpdateStamina(float value01)
    {
        currentStamina = Mathf.Clamp01(value01);
        if (staminaBar != null) staminaBar.value = currentStamina;
        if (staminaFill != null)
            staminaFill.color = Color.Lerp(staminaLow, staminaFull, currentStamina);
    }

    void UpdateProximityEffects()
    {
        if (predatorTransform == null || playerTransform == null) return;
        float dist = Vector3.Distance(playerTransform.position, predatorTransform.position);

        // stress vignette darkens
        if (stressVignette != null)
        {
            float t = 1f - Mathf.Clamp01((dist - vignetteNearDistance) / (vignetteFarDistance - vignetteNearDistance));
            Color c = stressVignette.color;
            c.a = Mathf.Lerp(c.a, t * maxVignetteAlpha, Time.deltaTime * 2.5f);
            stressVignette.color = c;
        }
    }

    void UpdateHeartbeat()
    {
        if (heartbeatSource == null || predatorTransform == null || playerTransform == null) return;

        float dist = Vector3.Distance(playerTransform.position, predatorTransform.position);

        // heartbeat volume increase the closer the predator is
        float targetVolume = dist < heartbeatFarDistance
            ? maxHeartbeatVolume * (1f - Mathf.Clamp01((dist - heartbeatNearDistance) / (heartbeatFarDistance - heartbeatNearDistance)))
            : 0f;

        heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, targetVolume, Time.deltaTime * 2f);

        // heart rate increases the closer the predator is
        currentHeartRate = Mathf.Lerp(0.8f, 3.5f, 1f - Mathf.Clamp01((dist - heartbeatNearDistance) / (heartbeatFarDistance - heartbeatNearDistance)));

        // play beat on interval
        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer <= 0f && heartbeatSource.volume > 0.05f && heartbeatClip.Length > 0)
        {
            int randomeIdx = Random.Range(0, heartbeatClip.Length);
            heartbeatSource.PlayOneShot(heartbeatClip[randomeIdx], heartbeatSource.volume);
            heartbeatTimer = 1f / currentHeartRate;
        }
    }

    public void UpdateTimer(float seconds)
    {
        if (timerText == null) return;
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{mins:00}:{secs:00}";
    }

    public void ShowEscapeNotification()
    {
        if (escapeNotification == null) return;
        escapeNotification.SetActive(true);
        if (escapeNotifText != null)
            escapeNotifText.text = "The Escape has arrived";
        notifActive = true;
        notifTimer = notifDisplayTime;
    }

    void UpdateNotification()
    {
        if (!notifActive) return;
        notifTimer -= Time.deltaTime;
        if (notifTimer <= 0f)
        {
            notifActive = false;
            if (escapeNotification != null) escapeNotification.SetActive(false);
        }
    }

    public void ShowGameOver(float survivalTime)
    {
        if (gameOverScreen == null) return;
        gameOverScreen.SetActive(true);
        if (gameOverTitle != null)
            gameOverTitle.text = "haha got caught";
        if (survivalTimeText != null)
        {
            int mins = Mathf.FloorToInt(survivalTime / 60f);
            int secs = Mathf.FloorToInt(survivalTime % 60f);
            survivalTimeText.text = $"you survived for {mins:00}:{secs:00}";
        }
    }

    public void ShowEscapeScreen(float survivalTime)
    {
        if (escapeScreen == null) return;
        escapeScreen.SetActive(true);
        if (escapeTimeText != null)
        {
            int mins = Mathf.FloorToInt(survivalTime / 60f);
            int secs = Mathf.FloorToInt(survivalTime % 60f);
            escapeTimeText.text = $"you survived {mins:00}:{secs:00} and escaped";
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void UpdateHeadStartUI(float remainingTime)
    {
        if (remainingTime > 0)
        {
            headstartPanel.SetActive(true);
            headstartText.text = "PREDATOR RELEASING IN: " + Mathf.CeilToInt(remainingTime).ToString() + "s";
        }
        else
        {
            headstartPanel.SetActive(false);
        }
    }
}
