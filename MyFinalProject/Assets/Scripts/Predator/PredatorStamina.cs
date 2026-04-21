using UnityEngine;
using UnityEngine.AI;

public class PredatorStamina : MonoBehaviour
{
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float sprintDrainRate = 18f;
    public float recoveryRate = 10f;
    public float recoveryDelay = 2f;
    public float exhaustionThreshold = 15f;

    [Header("Speed Penalty")]
    public float exhaustedSpeedMultiplier = 0.5f;

    private PredatorAgent mlAgent;
    private PredatorBT btAgent;
    private NavMeshAgent navAgent;

    private float originalMLBaseSpeed;
    private float originalBTBaseSpeed;

    private float currentStamina;
    private float recoveryTimer = 0f;
    private bool isExhausted = false;

    public float StaminaNormalised => currentStamina / maxStamina;
    public bool IsExhausted => isExhausted;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentStamina = maxStamina;

        mlAgent = GetComponent<PredatorAgent>();
        btAgent = GetComponent<PredatorBT>();
        navAgent = GetComponent<NavMeshAgent>();

        if (mlAgent != null) originalMLBaseSpeed = mlAgent.baseSpeed;
        if (btAgent != null) originalBTBaseSpeed = btAgent.baseSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        bool isSprinting = CheckIfSprinting();

        if (isSprinting && !isExhausted)
        {
            currentStamina -= sprintDrainRate * Time.deltaTime;
            recoveryTimer = recoveryDelay;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true;
                ApplyExhaustion();
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

            if (isExhausted && currentStamina >= exhaustionThreshold * 2f)
            {
                isExhausted = false;
                RestoreSpeed();
            }
        }

    }

    bool CheckIfSprinting()
    {
        if (mlAgent != null && mlAgent.gameObject.activeInHierarchy)
            return mlAgent.IsRecognised;

        if (btAgent != null && btAgent.gameObject.activeInHierarchy)
            return btAgent.CurrentBehaviour == "STRIKING";

        return false;
    }

    void ApplyExhaustion()
    {

        // sprint when prey/player is seen
        if (mlAgent != null) mlAgent.baseSpeed = originalMLBaseSpeed * exhaustedSpeedMultiplier;
        if (btAgent != null)
        {
            btAgent.baseSpeed = originalBTBaseSpeed * exhaustedSpeedMultiplier;
            if (navAgent != null) navAgent.speed = btAgent.baseSpeed;
        }
    }

    void RestoreSpeed()
    {
        // reduce speed
        if (mlAgent != null) mlAgent.baseSpeed = originalMLBaseSpeed;
        if (btAgent != null)
        {
            btAgent.baseSpeed = originalBTBaseSpeed;
            if (navAgent != null) navAgent.speed = btAgent.baseSpeed;
        }
    }
}
