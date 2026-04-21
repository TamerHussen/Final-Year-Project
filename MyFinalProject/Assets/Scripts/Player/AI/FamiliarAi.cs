using System.Net;
using UnityEngine;

public class FamiliarAi : MonoBehaviour
{
    [Header("Behaviour Types")]
    public bool isAerialScout = false;
    public bool isWorm = false;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float turnSpeed = 5f;
    public float hoverHeight = 15f; // flight altitude
    public float sweepRadius = 30f; // patrol radius

    [Header("Worm Specific")]
    public Transform wormModel;
    public float wormDiveDepth = -0.8f;
    public float wormSurfaceHeight = 0f;

    [Header("Lifespan & Sound")]
    public float lifespan = 90f; // despawn timer
    public float soundEmitInterval = 3f; // familiar making sound
    public float alertSoundInterval = 1.2f;

    [Header("Detection Settings")]
    public float detectonRadius = 20f; // scan radius
    public LayerMask detectionMask;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip IdleSFX;
    public AudioClip AlertSFX;

    [Header("Aerial Animation Tuning")]
    [Tooltip("playback speed while patrolling")]
    public float patrolAnimSpeed = 0.55f;
    [Tooltip("playback speed once alerted, goes to target")]
    public float alertAnimSpeed = 1.1f;
    [Tooltip("playback speed when circling above target")]
    public float circleAnimSpeed = 0.85f;
    [Tooltip("playback speed while diving toward target")]
    public float diveAnimeSpeed = 1.5f;
    [Tooltip("body tilt")]
    public float maxDiveAmount = 0.75f;
    [Tooltip("distance before switching to circling mode")]
    public float circleRadius = 5f;

    public Animator animator;

    private TrailMarker preyTrail;
    private CharacterController controller;

    private PredatorAgent predatorAgentML;
    private PredatorBT predatorBT;

    private float timer = 0f;
    private float soundTimer = 0f;
    private float spawnDelay = 2.5f; // seconds for how long the summon doesnt move for
    private float spawnDelayTimer = 0f;
    private bool spawnDelayComplete = false;
    private bool isDead = false;

    private int currentPathIndex = 0;
    private Transform preyTransform;

    // flying scout sweep state
    private Vector3 SweepCenter;
    private Vector3 SweepTarget;
    private float sweepTimer = 0f;
    private float sweepInterval = 4f;
    private bool isAlerted = false;
    private Transform alertTarget = null;

    private float smoothedIntensity = 0f;
    private float smoothedBank = 0f;
    private float smoothedDive = 0f;
    private float smoothedAnimSpeed = 0f;

    private enum AerialState { Patrol, Alerted, Circling, Diving }
    private AerialState aerialState = AerialState.Patrol;
    private bool wasAlertedLastFrame = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();

        GameObject activePredator = GameObject.FindGameObjectWithTag("Predator");

        if (activePredator != null)
        {
            predatorAgentML = activePredator.GetComponent<PredatorAgent>();
            predatorBT = activePredator.GetComponent<PredatorBT>();
        }

        // find the player's trail marker in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            preyTrail = player.GetComponent<TrailMarker>();
            preyTransform = player.transform;
        }

        if (isAerialScout)
        {
            SweepCenter = transform.position;
            PickNewSweepTarget();
        }
        else
        {
            // find the closet point on the permanent trail to start tracking
            SnapToClosestTrailPoint();
        }
    }

    // find nearest trail entry point
    void SnapToClosestTrailPoint()
    {
        if (preyTrail == null || preyTrail.FamiliarTrail.Count == 0) return;

        float closest = float.MaxValue;
        for (int i = 0; i < preyTrail.FamiliarTrail.Count; i++)
        {
            float d = Vector3.Distance(transform.position, preyTrail.FamiliarTrail[i]);
            if (d < closest)
            {
                closest = d;
                currentPathIndex = i;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;

        timer += Time.deltaTime;
        soundTimer += Time.deltaTime;

        if (timer >= lifespan)
        {
            Die();
            return;
        }

        // stands still for the first dew seconds, so i can add a summoning animation later
        if (!spawnDelayComplete)
        {
            spawnDelayTimer += Time.deltaTime;
            if (spawnDelayTimer >= spawnDelay)
            {
                spawnDelayComplete = true;
                if (isAerialScout && animator != null)
                    animator.speed = diveAnimeSpeed;
                return;
            }
            if (isAerialScout && animator != null)
                animator.SetFloat("flightIntensity", 0f);
            return;
        }

        if (isAerialScout)
        {
            UpdateAerialScout();
        }
        else
        {
            UpdateGroundTracker();
        }
    }

    void Die()
    {
        isDead = true;

        if (controller != null) controller.enabled = false;

        if (animator != null)
        {
            animator.speed = 0.3f;
            animator.SetTrigger("onDeath");
        }

        Invoke("Despawn", 2.5f);
    }

    // ground tracker logic
    void UpdateGroundTracker()
    {
        if (preyTrail == null) return;

        Vector3 velocity = Vector3.zero;
        float distanceToPlayer = Vector3.Distance(transform.position, preyTransform.position);

        isAlerted = (distanceToPlayer <= detectonRadius);

        if (currentPathIndex < preyTrail.FamiliarTrail.Count)
        {
            // skip ahead logic
            int maxLookAhead = Mathf.Min(currentPathIndex + 20, preyTrail.FamiliarTrail.Count);
            for (int i = currentPathIndex + 1; i < maxLookAhead; i++)
            {
                if (Vector3.Distance(transform.position, preyTrail.FamiliarTrail[i]) < 3f)
                {
                    currentPathIndex = i;
                }
            }

            Vector3 targetNode = preyTrail.FamiliarTrail[currentPathIndex];
            Vector3 dir = targetNode - transform.position;
            dir.y = 0; // ignore the hieght difference

            if (dir.sqrMagnitude < 2f) // if node is reached move to the next one
            {
                currentPathIndex++;
            }
            else
            {
                bool shouldMove = !isWorm || !isAlerted;

                if (shouldMove)
                {
                    // wobble logic
                    Vector3 right = Vector3.Cross(Vector3.up, dir.normalized);
                    Vector3 wobble = right * Mathf.Sin(Time.time * 8f) * 1.2f;
                    Vector3 finalDir = (dir + wobble).normalized;

                    // look at and move toward the next scent node
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(finalDir), Time.deltaTime * turnSpeed);
                    velocity = finalDir * moveSpeed;
                }
            }

            // emit sound to guide the predator
            if (soundTimer >= soundEmitInterval)
            {
                if (isAlerted)
                {
                    SoundEmitter.Emit(transform.position, 1.5f, SoundEmitter.SoundSource.Familiar);

                    PlaySound(AlertSFX);
                }
                else
                {
                    SoundEmitter.Emit(transform.position, 0.8f, SoundEmitter.SoundSource.Familiar);
                    PlaySound(IdleSFX);
                }
                soundTimer = 0f;
            }
        }

        if (isWorm && wormModel != null)
        {
            float targetY = isAlerted ? wormSurfaceHeight : wormDiveDepth;
            Vector3 targetPos = new Vector3(0, targetY, 0);
            wormModel.localPosition = Vector3.Lerp(wormModel.localPosition, targetPos, Time.deltaTime * 5f);
        }

        velocity.y -= 9.81f; // gravity
        if (controller != null)
        {
            controller.Move(velocity * Time.deltaTime);
        }

        if (animator != null)
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);

            animator.SetBool("isMoving", horizontalVelocity.magnitude > 0.1f);
            animator.SetBool("isAlerted", isAlerted);
            animator.SetFloat("moveSpeed", horizontalVelocity.magnitude);
        }
    }

    // ariel scout logic
    void UpdateAerialScout()
    {
        if (!isAlerted) ScanForTargets();

        Vector3 velocity = Vector3.zero;

        if (isAlerted && alertTarget != null)
        {
            // hover above ground
            float distToTarget = Vector3.Distance(transform.position, alertTarget.position);
            Vector3 above = new Vector3(alertTarget.position.x, hoverHeight, alertTarget.position.z);
            Vector3 toAbove = above - transform.position;
            float horizontalDist = new Vector3(toAbove.x, 0, toAbove.z).magnitude;

            if (horizontalDist < circleRadius)
            {
                aerialState = AerialState.Circling;
                Vector3 circleDir = Vector3.Cross(Vector3.up, toAbove.normalized);
                velocity = circleDir * moveSpeed * 0.7f + Vector3.up * (hoverHeight - transform.position.y) * 1.5f;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(new Vector3(circleDir.x, 0, circleDir.z)), Time.deltaTime * turnSpeed);
            }
            else
            {
                bool highEnough = transform.position.y >= hoverHeight * 0.8f;
                aerialState = highEnough ? AerialState.Diving : AerialState.Alerted;

                Vector3 dir = toAbove.normalized;
                float heightFix = (hoverHeight - transform.position.y) * 2f;
                velocity = dir * moveSpeed + Vector3.up * heightFix;

                if (new Vector3(dir.x, 0, dir.z).sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)), Time.deltaTime * turnSpeed);
            }

            HandleAerialSound(alertSoundInterval, alertTarget.position, 1.5f, AlertSFX, true);

        }
        else
        {
            aerialState = AerialState.Patrol;

            sweepTimer += Time.deltaTime;

            Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 flatTarget = new Vector3(SweepTarget.x, 0, SweepTarget.z);
            float distanceToTarget = Vector3.Distance(flatPos, flatTarget);

            if (sweepTimer >= sweepInterval || distanceToTarget < 2f)
            {
                PickNewSweepTarget();
                sweepTimer = 0f;
            }

            Vector3 dir = (SweepTarget - transform.position).normalized;
            float heightFix = (hoverHeight - transform.position.y) * 2f;
            velocity = dir * moveSpeed + Vector3.up * heightFix;

            Vector3 flatDir = new Vector3(dir.x, 0, dir.z);
            if (flatDir.sqrMagnitude > 0.01f && distanceToTarget > 0.5f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(flatDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            }

            HandleAerialSound(soundEmitInterval, transform.position, 0.5f, IdleSFX, false);
        }

        if (isAlerted && !wasAlertedLastFrame)
            FireSpotTrigger();
        wasAlertedLastFrame = isAlerted;

        if (controller != null)
        {
            controller.Move(velocity * Time.deltaTime);
        }

        UpdateAerialAnimator(velocity);
    }

    void UpdateAerialAnimator(Vector3 velocity)
    {
        if (animator == null) return;

        float targetIntensity;
        float targetAnimSpeed;
        float targetDive;

        switch (aerialState)
        {

            case AerialState.Patrol:
                // look for target
                targetIntensity = 0f;
                targetAnimSpeed = patrolAnimSpeed;
                targetDive = 0f;
                break;

            case AerialState.Alerted:
                // spotted target
                targetIntensity = 0.6f;
                targetAnimSpeed = alertAnimSpeed;
                targetDive = 0.2f;
                break;

            case AerialState.Diving:
                // high speed
                targetIntensity = 1f;
                targetAnimSpeed = diveAnimeSpeed;
                targetDive = maxDiveAmount;
                break;

            case AerialState.Circling:
                // stay above target
                targetIntensity = 0.4f;
                targetAnimSpeed = circleAnimSpeed;
                targetDive = 0f;
                break;

            default:
                targetIntensity = 0f;
                targetAnimSpeed = patrolAnimSpeed;
                targetDive = 0f;
                break;
        }

        Vector3 hVel = new Vector3(velocity.x, 0, velocity.z);
        float targetBank = 0f;
        if (hVel.magnitude > 0.5f)
        {
            Vector3 right = transform.right;
            targetBank = Mathf.Clamp(Vector3.Dot(hVel.normalized, right) * 1.5f, -1f, 1f);
        }

        float lerpSpeed = Time.deltaTime * 3f;
        smoothedIntensity = Mathf.Lerp(smoothedIntensity, targetIntensity, lerpSpeed);
        smoothedBank = Mathf.Lerp(smoothedBank, targetBank, lerpSpeed * 2f);
        smoothedDive = Mathf.Lerp(smoothedDive, targetDive, lerpSpeed);
        smoothedAnimSpeed = Mathf.Lerp(smoothedAnimSpeed, targetAnimSpeed, lerpSpeed);

        animator.speed = smoothedAnimSpeed;
        animator.SetFloat("flightIntensity", smoothedIntensity);
        animator.SetFloat("bankAngle", smoothedBank);
        animator.SetFloat("diveAmount", smoothedDive);
        animator.SetFloat("moveSpeed", hVel.magnitude);
        animator.SetBool("isMoving", hVel.magnitude > 0.1f);
        animator.SetBool("isAlerted", isAlerted);
    }

    void FireSpotTrigger()
    {
        if (animator == null) return;
        animator.speed = diveAnimeSpeed * 1.2f;
        animator.SetTrigger("onSpotTarget");
    }

    // aerial scan
    void ScanForTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectonRadius, detectionMask);

        foreach (var hit in hits)
        {
            // player is main target
            if (hit.CompareTag("Player"))
            {
                alertTarget = hit.transform;
                isAlerted = true;
                Debug.Log("aerial scout spotted the player");
                return;
            }

            // animal is decoy
            if (hit.CompareTag("Animal"))
            {
                alertTarget = hit.transform;
                isAlerted = true;
                Debug.Log("aerial scout spotted animal");
                return;
            }
        }

        // nothing found, keep looking
        isAlerted = false;
        alertTarget = null;
    }

    // new sweep point
    void PickNewSweepTarget()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(sweepRadius * 0.3f, sweepRadius);
        SweepTarget = SweepCenter + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
        SweepTarget.y = hoverHeight;
    }

    // despawn
    void Despawn()
    {
        if (predatorAgentML != null && predatorAgentML.gameObject.activeInHierarchy)
        {
            predatorAgentML.OnFamiliarDespawned(gameObject);
        }
        if (predatorBT != null && predatorBT.gameObject.activeInHierarchy)
        {
            predatorBT.OnFamiliarDespawned(gameObject);
        }

        Destroy(gameObject);
    }

    // audio helper
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // aerial helper
    void HandleAerialSound(float interval, Vector3 emitPos, float volume, AudioClip clip, bool alerted)
    {
        if (soundTimer < interval) return;
        SoundEmitter.Emit(emitPos, volume, SoundEmitter.SoundSource.Familiar);
        PlaySound(clip);
        soundTimer = 0f;
    }


    // gizmos
    private void OnDrawGizmosSelected()
    {
        if (isAerialScout)
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, detectonRadius);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, SweepTarget);
            if (isAlerted && alertTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, alertTarget.position);
            }
        }
        else
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
    }
}
