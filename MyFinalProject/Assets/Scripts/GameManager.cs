using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Predator References")]
    public PredatorAgent MLPredator; // machine learning agent predator
    public PredatorBT BTPredator; // behaviour tree predator

    [Header("Player Reference")]
    public PlayerMovement player;
    public Animator playerAnimator;

    [Header("Session Settings")]
    public float survivalTargetTime = 180; // 3 minutes
    public bool useTimedEscape = true;

    [Header("Escape Zone")]
    public GameObject escapeZone;
    public float escapeZoneApearTime = 90f;
    public float escapeZoneMinDistFromPlayer = 20f;

    [Header("References")]
    public GameUI gameUI;
    public PauseUI pauseUI;
    public MapRandomiser mapRandomiser;
    public BiomeRandomiser biomeRandomiser;

    [Header("Headstart")]
    public float headstartDuraction = 15f;

    private float currentHeadstart;

    private float sessionTimer = 0f;
    private bool sessionActive = false;
    private bool playerDead = false;
    private bool playerEscaped = false;
    private bool useBTPredator = false;

    public float SessionTimer => sessionTimer;
    public bool SessionActive => sessionActive;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // mode from main menu
        useBTPredator = PlayerPrefs.GetInt("UseBTPredator", 0) == 1;

        if (MLPredator != null) MLPredator.gameObject.SetActive(!useBTPredator);
        if (BTPredator != null) BTPredator.gameObject.SetActive(useBTPredator);

        if (mapRandomiser != null && player != null)
        {
            Transform predatorTransform = useBTPredator ? BTPredator?.transform : MLPredator?.transform;
            if (predatorTransform != null)
            {
                mapRandomiser.RandomiseForGameplay(player.transform, predatorTransform);
            }
        }

        biomeRandomiser?.RandomiseBiome();

        currentHeadstart = headstartDuraction;
        StartSession();
    }

    void StartSession()
    {
        sessionActive = true;
        playerDead = false;
        playerEscaped = false;
        sessionTimer = 0f;

        if (escapeZone != null) escapeZone.SetActive(false);
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!sessionActive || playerDead || playerEscaped) return;

        // pause input
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }

        // headstart counter
        if (currentHeadstart > 0f)
        {
            currentHeadstart -= Time.deltaTime;
            gameUI?.UpdateHeadStartUI(currentHeadstart);

            sessionTimer = 0f;
        }
        else
        {
            gameUI.UpdateHeadStartUI(0f);

            sessionTimer += Time.deltaTime;
            gameUI?.UpdateTimer(sessionTimer);

            // reveal escape zone
            if (useTimedEscape && escapeZone != null && !escapeZone.activeSelf && sessionTimer >= escapeZoneApearTime)
            {
                PlaceEscapeZone();
                escapeZone.SetActive(true);
                gameUI?.ShowEscapeNotification();
            }
        }
    }

    void PlaceEscapeZone()
    {
        if (mapRandomiser == null || player == null) return;

        Vector3 playerPos = player.transform.position;
        Vector3 bestSpot = Vector3.zero;
        float bestDist = 0f;

        // choose from 20 spots
        for (int i = 0; i < 20; i++)
        {
            Vector3 candidate = mapRandomiser.GetTerrainPoint();
            float dist = Vector3.Distance(candidate, playerPos);
            if (dist > escapeZoneMinDistFromPlayer && dist > bestDist)
            {
                bestDist = dist;
                bestSpot = candidate;
            }
        }
        if (bestSpot != Vector3.zero)
            escapeZone.transform.position = bestSpot;
    }

    public void OnPlayerCaught()
    {
        if (playerDead || playerEscaped) return;

        playerDead = true;
        sessionActive = false;

        // play death animation
        if (playerAnimator != null)
            playerAnimator.SetTrigger("onDeath");

        // freeze player input
        if (player != null)
            player.enabled = false;

        // show game over
        Invoke(nameof(ShowGameOver), 2.5f);
    }

    public void OnPlayerEscaped()
    {
        if (playerDead || playerEscaped) return;

        playerEscaped = true;
        sessionActive = false;

        Time.timeScale = 0f;

        gameUI?.ShowEscapeScreen(SessionTimer);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowGameOver()
    {
        Time.timeScale = 0f;

        gameUI?.ShowGameOver(SessionTimer);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void TogglePause()
    {
        if (playerDead || playerEscaped) return;

        bool pausing = Time.timeScale > 0f;

        if (pausing)
        {
            Time.timeScale = 0f;
            sessionActive = false;
            AudioListener.pause = true;
            pauseUI?.Show();
            Object.FindFirstObjectByType<MusicManager>()?.OnPause();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            sessionActive = true;
            AudioListener.pause = false;
            pauseUI?.Hide();
            Object.FindFirstObjectByType<MusicManager>()?.OnResume();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        LoadingScreen.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
