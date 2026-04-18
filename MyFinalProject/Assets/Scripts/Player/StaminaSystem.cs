using UnityEngine;

public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float sprintDrainRate = 20f; // per second while sprinting
    public float recoveryRate = 12f; // per second while not sprinting
    public float recoveryDelay = 2.5f; // rest before recovery
    public float exhaustionThreshold = 10f; // below this, cant sprint

    [Header("References")]
    public PlayerMovement playerMovement;
    public GameUI gameUI;

    private float currentStamina;
    private float recoveryTimer = 0f;
    private bool isExhausted = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentStamina = maxStamina;
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        bool sprinting = IsSprinting();
        if (sprinting && !isExhausted)
        {
            currentStamina -= sprintDrainRate * Time.deltaTime;
            recoveryTimer = recoveryDelay;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true;
            }
        }
        else
        {
            recoveryTimer -= Time.deltaTime;
            if (recoveryTimer <= 0f)
            {
                currentStamina += recoveryRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }

            // recover from exhaustion
            if (isExhausted && currentStamina >= exhaustionThreshold * 2f)
                isExhausted = false;
        }

        // show current stamina
        gameUI?.UpdateStamina(currentStamina / maxStamina);
    }

    // player movement check
    public bool CanSprint() => !isExhausted && currentStamina > exhaustionThreshold;
    public float StaminaNormalised => currentStamina / maxStamina;

    bool IsSprinting()
    {
        // only use stamina when moving fast
        if (playerMovement == null) return false;
        return playerMovement.characterController != null
            && playerMovement.characterController.velocity.magnitude > playerMovement.walkSpeed * 1.1f;
    }
}
