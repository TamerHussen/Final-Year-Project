using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class PredatorAgent : Agent
{
    [Header("References")]
    public Transform player;
    public TrailMarker preyTrail;
    public PlayerMovement playerMovement;
    public PreyAi preyAi;
    public CharacterController predatorController;
    public Animator animator;

    [Header("Movement Settings")]
    public float baseSpeed = 3.5f;
    public float turnSpeed = 250f;
    private bool isCrouching = false;
    private float smoothedTurn = 0f; // stop jittering
    private float currentCCSpeed = 0f;

    [Header("Vision Settings")]
    public float rayDistance = 45f; // better eyesight
    public float penaltyDistance = 3.5f; // no touching obj
    public float eyeHeight = 1.0f;
    public float targetHeight = 1.0f;
    public LayerMask visionMask; // walls, player, obstacles

    [Header("Stalking Settings")]
    public float recognitionThreshold = 0.7f;
    public float headstartDuration = 15f; // give prey headstart
    private float visionRecognitionTimer = 0f;
    private bool preyIsRecognised = false;

    [Header("Familiar Settings")]
    public List<GameObject> groundFamiliarPrefabs; // grounded familiars
    public List<GameObject> skyFamiliarPrefabs; // flying familiars
    public float timeBeforeSummon = 12f; // how long it cant see player before spawning familiars
    public float familiarCooldown = 45f; // prevent spamming
    public int maxActiveFamiliars = 3;
    public float familiarCooldownRemaining = 0f;
    private int activeFamiliarCount = 0;
    private bool hasFoundTrailThisEpisode = false; // stop summoning when trail already found
    private bool nextSummonIsAerial = false;

    [Header("Taunt Settings")]
    public float tauntDuration = 2.5f; // how long predator stands still
    private float tauntTimer = 0f;

    private float continousLosTimer = 0f; // prevent familiar summoning during chase
    private float losGracePeriod = 8f;
    private float timeSinceLastLos = 0f;

    private List<GameObject> activeFamiliars = new List<GameObject>(); // track the active familiars

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip RecogniseTargetSFX;
    public AudioClip LostTargetSFX;
    public AudioClip SummonFamiliarSFX;
    public AudioClip CatchPreySFX;

    private float lastDistanceToPlayer;
    private float lastDistanceToScent = Mathf.Infinity;
    private float timeSinceSeen = 0f;
    private float episodeTimer = 0f; // internal timer
    private float lastSoundDistanceSq = Mathf.Infinity; // tracking soumd

    private Vector3 lastKnownScentPos = Vector3.zero;
    private bool hasScentLead = false;

    private BehaviorParameters behaviorParameters;

    // used for debug UI
    public float TimeSinceSeen => timeSinceSeen;
    public float LastDistanceToScent => lastDistanceToScent;
    public float LastDistanceToPlayer => lastDistanceToPlayer;
    public bool IsRecognised => preyIsRecognised;
    public float FamiliarCooldDownRemaining => familiarCooldownRemaining;
    public int ActiveFamiliarCount => activeFamiliarCount;

    private Vector3 EyePos => transform.position + Vector3.up * eyeHeight;

    private bool IsInferenceMode =>
        behaviorParameters != null &&
        behaviorParameters.BehaviorType == BehaviorType.InferenceOnly;

    private void Start()
    {
        behaviorParameters = GetComponent<BehaviorParameters>();
    }

    public override void OnEpisodeBegin()
    {
        timeSinceSeen = 0f;
        episodeTimer = 0f;
        familiarCooldownRemaining = 0f;
        activeFamiliarCount = 0;
        hasFoundTrailThisEpisode = false;
        hasScentLead = false;
        lastKnownScentPos = Vector3.zero;
        lastDistanceToScent = Mathf.Infinity;
        lastSoundDistanceSq = Mathf.Infinity;
        visionRecognitionTimer = 0f;
        preyIsRecognised = false;
        smoothedTurn = 0f;
        continousLosTimer = 0f;
        timeSinceLastLos = losGracePeriod;
        nextSummonIsAerial = false;
        tauntTimer = 0f;

        // remove familairs from previous episode
        foreach (var f in activeFamiliars)
        {
            if (f != null) Destroy(f);
        }
        activeFamiliars.Clear();

        SoundEmitter.ResetSound();

        if (preyTrail != null)
        {
            preyTrail.ResetTrail();
        }

        // randomises respawn for better ml agent learning
        if (!IsInferenceMode && MapRandomiser.instance != null)
        {
            MapRandomiser.instance.Randomise(this);
            Physics.SyncTransforms();
        }

        lastDistanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isSprinting", false);
            animator.SetBool("isCrouching", false);
        }


        // disable teleport
        if (predatorController != null) predatorController.enabled = false;

        transform.position = MapRandomiser.instance.GetValidSpawnLocation(true);

        currentCCSpeed = 0f;
        smoothedTurn = 0f;

        if (predatorController != null) predatorController.enabled = true;

    }

    // ------------------------------------------------------------
    //  Collect Observations
    // ------------------------------------------------------------
    public override void CollectObservations(VectorSensor sensor)
    {
        float dist = Vector3.Distance(transform.position, player.position);

        // player direction and distance
        sensor.AddObservation(dist / 50f); // 1 obs
        sensor.AddObservation(transform.InverseTransformDirection((player.position - transform.position).normalized)); // 3 obs
        sensor.AddObservation(predatorController.velocity / 10f); // 3 obs

        // vision and recognition state
        bool visible = CheckLineOfSight();
        sensor.AddObservation(visible ? 1f : 0f); // 1 obs
        sensor.AddObservation(preyIsRecognised ? 1f : visionRecognitionTimer / recognitionThreshold); // 1 obs

        // predator state
        sensor.AddObservation(isCrouching ? 1f : 0f); // 1 obs
        sensor.AddObservation(Mathf.Clamp01(timeSinceSeen / 15f)); // 1 obs
        sensor.AddObservation(Mathf.Clamp01(familiarCooldownRemaining / familiarCooldown)); // 1 obs
        sensor.AddObservation(hasFoundTrailThisEpisode ? 1f : 0f); // 1 obs

        // prey state
        sensor.AddObservation(preyAi != null && preyAi.isSprinting ? 1f : 0f); // 1 obs
        sensor.AddObservation(preyAi != null && preyAi.isCrouching ? 1f : 0f); // 1 obs
        sensor.AddObservation(preyAi != null && preyAi.isExposed ? 1f : 0f); // 1 obs
        sensor.AddObservation(preyAi != null && preyAi.inSoftObj ? 1f : 0f); // 1 obs

        // sound
        Vector3 soundDir = SoundEmitter.LastSoundPos - transform.position;
        sensor.AddObservation(soundDir.sqrMagnitude < 0.01f ? Vector3.zero : transform.InverseTransformDirection(soundDir.normalized)); // 3 obs
        sensor.AddObservation(Mathf.Clamp01(SoundEmitter.LastSoundVolume)); // 1 obs

        // recent scent trail
        if (preyTrail != null && preyTrail.MainTrail != null)
        {
            int count = preyTrail.MainTrail.Count;
            for (int i = 0; i < 3; i++)
            {
                int index = count - 1 - i;
                if (index < 0)
                {
                    sensor.AddObservation(Vector3.zero); // 3 obs
                    sensor.AddObservation(0f); // 1 obs
                }
                else
                {
                    Vector3 scentDir = preyTrail.MainTrail[index] - transform.position;
                    sensor.AddObservation(transform.InverseTransformDirection(scentDir.normalized));
                    sensor.AddObservation(Mathf.Clamp01(scentDir.magnitude / rayDistance));
                }

            }
        }
        else
        {
            // fallback
            for (int i = 0; i < 3; i++)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(0f);
            }
        }

        // Scent Trail points for familiar
        if (preyTrail != null && preyTrail.FamiliarTrail != null && preyTrail.FamiliarTrail.Count > 0)
        {
            Vector3 familiarPoint = preyTrail.FamiliarTrail[preyTrail.FamiliarTrail.Count - 1];
            // position tracking for scouts
            sensor.AddObservation(transform.InverseTransformDirection(familiarPoint - transform.position)); // 3 obs
        }
        else
        {
            // fallback
            sensor.AddObservation(Vector3.zero); // 3 obs

        }

        // Add Raycasts
        AddRaycastObservations(sensor); // 54 obs

    }

    void AddRaycastObservations(VectorSensor sensor)
    {
        Vector3[] rays =
        {
            transform.forward,
            Quaternion.Euler(0,-15,0) * transform.forward, Quaternion.Euler(0,15,0) * transform.forward, // denser near front 
            Quaternion.Euler(0,-35,0) * transform.forward, Quaternion.Euler(0,35,0) * transform.forward,
            Quaternion.Euler(0,-55,0) * transform.forward, Quaternion.Euler(0,55,0) * transform.forward,
            Quaternion.Euler(0,-75,0) * transform.forward, Quaternion.Euler(0,75,0) * transform.forward // wider spread
        };

        foreach (var dir in rays)
        {
            if (Physics.Raycast(EyePos, dir, out RaycastHit hit, rayDistance, visionMask))
            {
                sensor.AddObservation(hit.distance / rayDistance);
                // Hit types
                sensor.AddObservation(hit.collider.CompareTag("Player") ? 1f : 0f);
                sensor.AddObservation(hit.collider.CompareTag("SolidObj") ? 1f : 0f);
                sensor.AddObservation(hit.collider.CompareTag("SoftObj") ? 1f : 0f);
                sensor.AddObservation(hit.collider.CompareTag("Animal") ? 1f : 0f); // distraction animals
                sensor.AddObservation(hit.collider.CompareTag("Walls") ? 1f : 0f);
            }
            else
            {
                sensor.AddObservation(1f); // max distance
                sensor.AddObservation(0f);  // 0 for tags = nothing hit
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f); // extra hiding spots like crates or trunks
                sensor.AddObservation(0f); // boundary walls
            }
        }
    }

    void Update()
    {
        episodeTimer += Time.deltaTime;
        if (GameManager.instance != null && GameManager.instance.gameUI != null)
        {
            float remaining = headstartDuration - episodeTimer;
            GameManager.instance.gameUI.UpdateHeadStartUI(remaining);
        }
        UpdateAnimator();
    }

    void UpdateAnimator()
    {
        if (animator == null || predatorController == null) return;
        float speed = predatorController.velocity.magnitude;

        animator.SetBool("isMoving", speed > 0.1f);
        animator.SetBool("isSprinting", speed > baseSpeed * 1f);
        animator.SetBool("isCrouching", isCrouching);
        animator.SetFloat("moveSpeed", speed);
    }

    // used for Debug ui
    public bool HasLineOfSight()
    {
        return CheckLineOfSight();
    }

    // line of sight
    bool CheckLineOfSight()
    {
        Vector3 targetPos = player.position + Vector3.up * targetHeight;
        Vector3 dirToPlayer = targetPos - EyePos;

        if (dirToPlayer.sqrMagnitude < 0.00001f) return false;
        if (Physics.Raycast(EyePos, dirToPlayer.normalized, out RaycastHit hit, rayDistance, visionMask))
        {
            return hit.collider.CompareTag("Player");
        }
        return false;
    }

    // ------------------------------------------------------------
    // Movement + Full Reward System
    // ------------------------------------------------------------
    public override void OnActionReceived(ActionBuffers actions)
    {
        episodeTimer += Time.fixedDeltaTime;

        // headstart
        if (episodeTimer < headstartDuration)
        {
            predatorController.Move(new Vector3(0, -9.81f * Time.fixedDeltaTime, 0));
            return;
        }

        float forward = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f); // move forward and backwards but stops it from moonwalking
        // float strafe = Mathf.Clamp(actions.ContinuousActions[1], -0.2f, 0.2f); // move left/right and limiting the strafing
        float turn = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f); // rotate

        // smooth turning no jittering
        smoothedTurn = Mathf.Lerp(smoothedTurn, turn, Time.fixedDeltaTime * 10f);

        if (tauntTimer > 0f)
        {
            tauntTimer -= Time.fixedDeltaTime;

            forward = 0f;
            turn = 0f;
            smoothedTurn = 0f;
        }

        // recognition state
        float recognitionModulator = 1f;
        bool hasLOS = CheckLineOfSight();

        if (hasLOS)
        {
            continousLosTimer += Time.fixedDeltaTime;
            timeSinceLastLos = 0f;

            if (!preyIsRecognised)
            {
                visionRecognitionTimer += Time.fixedDeltaTime;
                recognitionModulator = 0.2f + (visionRecognitionTimer / recognitionThreshold) * 0.8f; // 20% to 100%

                if (visionRecognitionTimer >= recognitionThreshold)
                {
                    preyIsRecognised = true;
                    tauntTimer = tauntDuration;

                    PlaySound(RecogniseTargetSFX);
                    if (animator != null) animator.SetTrigger("onRecognise");
                    Debug.Log("Prey found - Taunt then - ATTTAAACCCCKKKKK!!");
                }
            }
        }
        else
        {
            continousLosTimer = 0f;
            timeSinceLastLos += Time.fixedDeltaTime;

            if (preyIsRecognised)
            {
                PlaySound(LostTargetSFX);
                if (animator != null) animator.SetTrigger("onLostTarget");
            }
            visionRecognitionTimer = 0f;
            preyIsRecognised = false;
        }

        // Movement
        UpdateCCSpeed(forward, recognitionModulator);

        Vector3 moveCC = transform.forward * forward * currentCCSpeed;
        moveCC.y -= 9.81f; // gravity

        if (predatorController != null && predatorController.enabled)
        {
            predatorController.Move(moveCC * Time.fixedDeltaTime);
        }

        // turning
        transform.Rotate(0, smoothedTurn * turnSpeed * Time.fixedDeltaTime, 0f);

        // animation
        if (animator != null)
        {
            animator.SetBool("isMoving", forward > 0.1f);
            animator.SetBool("isSprinting", preyIsRecognised && forward > 0.5f);
            animator.SetBool("isCrouching", isCrouching);
            animator.SetFloat("moveSpeed", predatorController.velocity.magnitude);
        }

        // --------- REWARD SYSTEM ---------

        float currentDist = Vector3.Distance(transform.position, player.position);

        if (!IsInferenceMode)
        {
            // Small time penalty
            AddReward(-0.0002f);

            // visual hunting stalking and striking
            if (hasLOS)
            {
                timeSinceSeen = 0f;

                // stalking phase
                if (!preyIsRecognised)
                    AddReward(0.003f); // being a good boy

                // striking phase
                if (preyIsRecognised)
                {
                    AddReward(0.005f);
                    if (currentDist < lastDistanceToPlayer) AddReward(0.004f);
                    if (currentDist > lastDistanceToPlayer) AddReward(-0.002f);
                }

                bool preyExposed = preyAi != null && preyAi.isExposed;
                if (preyExposed && currentDist < 8f) AddReward(0.002f);

            }
            // tracking phase
            else
            {
                timeSinceSeen += Time.fixedDeltaTime;
                familiarCooldownRemaining -= Time.fixedDeltaTime;

                // scent cloud reward
                if (preyTrail != null && preyTrail.MainTrail.Count > 0)
                {
                    hasFoundTrailThisEpisode = true;

                    float closestDist = float.MaxValue;

                    foreach (Vector3 point in preyTrail.MainTrail)
                    {
                        float d = Vector3.Distance(transform.position, point);
                        if (d < closestDist)
                        {
                            closestDist = d;
                        }
                    }

                    float scentRadius = 6f;
                    if (closestDist < scentRadius)
                    {
                        float scentStrength = 1f - (closestDist / scentRadius);
                        AddReward(0.002f * scentStrength);
                    }

                    // rewarded for getting near newest scent
                    Vector3 freshestPoint = preyTrail.MainTrail[preyTrail.MainTrail.Count - 1];
                    float distToFreshest = Vector3.Distance(transform.position, freshestPoint);
                    if (distToFreshest < lastDistanceToScent) AddReward(0.003f);
                    lastDistanceToScent = distToFreshest;
                    lastKnownScentPos = freshestPoint;
                    hasScentLead = true;
                }

                // reward for moving towards sound
                Vector3 soundDir = SoundEmitter.LastSoundPos - transform.position;
                float soundDistSq = soundDir.sqrMagnitude;
                if (soundDistSq < lastSoundDistanceSq && SoundEmitter.LastSoundVolume > 0.1f)
                    AddReward(0.001f);
                lastSoundDistanceSq = soundDistSq;

                TrySummonFamiliar();
                if (hasScentLead && currentDist < lastDistanceToPlayer) AddReward(0.001f);
            }


            // penalties
            if (forward > 0.5f && predatorController.velocity.sqrMagnitude < 0.1f)
            {
                AddReward(-0.0005f);
            }

            if (StepCount >= MaxStep)
            {
                // taking too long
                AddReward(-0.1f);
                EndEpisode();
            }

            ApplyProximityPenalty();
        }
        else
        {
            if (hasLOS)
            {
                timeSinceSeen = 0f;
                timeSinceLastLos = 0f;
            }
            else
            {
                timeSinceSeen += Time.fixedDeltaTime;
                timeSinceLastLos += Time.fixedDeltaTime;
                familiarCooldownRemaining -= Time.fixedDeltaTime;
                TrySummonFamiliar();
            }
        }

        lastDistanceToPlayer = currentDist;
    }

    void TrySummonFamiliar()
    {
        // summoning condition:
        // - must be without los for timebeforesummon seconds
        // - cooldown must have ended
        // - must not be at capacity
        // - must not currently be in LOS
        // - LOS must not be continouse for a while

        bool geniunelyLost = timeSinceSeen > timeBeforeSummon;
        bool pastGracePeriod = timeSinceLastLos > losGracePeriod;
        bool cooldownClear = familiarCooldownRemaining <= 0f;
        bool underCap = activeFamiliarCount < maxActiveFamiliars;

        //familiar summoning
        if (geniunelyLost && pastGracePeriod && cooldownClear && underCap)
        {
            SummonFamiliar();
        }
    }

    // summon familiars
    private void SummonFamiliar()
    {
        // alternate between ground and aerial summons
        bool hasSky = skyFamiliarPrefabs != null && skyFamiliarPrefabs.Count > 0;
        bool hasGround = groundFamiliarPrefabs != null && groundFamiliarPrefabs.Count > 0;

        // list for familairs
        List<GameObject> pool;

        if (hasSky && (nextSummonIsAerial || !hasGround))
        {
            // if no trail lead send aerail scouts
            pool = skyFamiliarPrefabs;
        }
        else if (hasGround)
        {
            // if trail lead send ground scouts
            pool = groundFamiliarPrefabs;
        }
        else
        {
            return;
        }

        nextSummonIsAerial = !nextSummonIsAerial;

        if (pool.Count == 0) return;

        GameObject prefab = pool[Random.Range(0, pool.Count)];
        Vector3 spawnPos = transform.position + transform.forward * 2f + Vector3.up * 1.5f;
        GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);

        // destroy familiars if episode ends before despawn
        activeFamiliars.Add(spawned);
        activeFamiliarCount++;
        familiarCooldownRemaining = familiarCooldown;

        // pentality to prevent spamming familiars
        if (!IsInferenceMode) AddReward(-0.1f);

        PlaySound(SummonFamiliarSFX);
        Debug.Log($"SEND OUT THE BEAST. familiar deployed: [{(pool == skyFamiliarPrefabs ? "SKY" : "GROUND")}] - {activeFamiliarCount} active");

    }

    // count changes when familiars despawn
    public void OnFamiliarDespawned(GameObject familiar)
    {
        activeFamiliars.Remove(familiar);
        activeFamiliarCount = Mathf.Max(0, activeFamiliarCount - 1);
    }

    void UpdateCCSpeed(float forward, float recognitionModulator)
    {
        float speed = baseSpeed;
        if (isCrouching) speed *= 0.5f;
        if (preyIsRecognised) speed *= 1.5f;
        currentCCSpeed = speed * recognitionModulator;
    }

    // the closer the ml agent is to the solid objects and walls the higher the penalty
    private void ApplyProximityPenalty()
    {
        Vector3[] penaltyDirs = { transform.forward, transform.right, -transform.right };

        foreach (var dir in penaltyDirs)
        {
            if (Physics.Raycast(EyePos, dir, out RaycastHit hit, penaltyDistance, visionMask))
            {
                if (hit.collider.CompareTag("SolidObj") || hit.collider.CompareTag("Walls"))
                {
                    float closeness = 1f - (hit.distance / penaltyDistance);
                    AddReward(-0.05f * closeness); // plz stop hiiting obj
                }
            }
        }
    }

    // Trigger when catching the prey
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlaySound(CatchPreySFX);
            if (animator != null) animator.SetTrigger("onCatch");

            if (IsInferenceMode)
            {
                GameManager.instance?.OnPlayerCaught();
            }
            else
            {
                AddReward(1.0f);  // success catch
                Debug.Log("prey caught - episode over");
                EndEpisode();
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) // add model asset if starting without training to avoid error
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = 0f; // Input.GetAxis("Mouse X");
        continuousActionsOut[2] = Input.GetAxis("Horizontal");
    }

    // audio helper
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // gizmos
    private void DrawVisionCone()
    {
        Gizmos.color = new Color(1f, preyIsRecognised ? 0f : 0.5f, 0f, 0.25f); // transparent colour - red = charginng, amber = saw prey, orange = in LOS
        float halfFov = 30f;
        int segments = 16;
        Vector3 origin = EyePos;

        // draws the circle at max distance
        if (visionRecognitionTimer > 0f)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
            float scaledDist = recognitionThreshold * rayDistance * (visionRecognitionTimer / recognitionThreshold);

            for (int i = -segments / 2; i < segments / 2; i++)
            {
                float angleA = halfFov * (i / (float)segments);
                float angleB = halfFov * ((i + 1) / (float)segments);

                Vector3 dirA = Quaternion.Euler(0, angleA, 0) * transform.forward;
                Vector3 dirB = Quaternion.Euler(0, angleB, 0) * transform.forward;

                Gizmos.DrawLine(origin, origin + dirA * scaledDist);
                Gizmos.DrawLine(origin + dirA * scaledDist, origin + dirB * scaledDist);
            }
        }

        // last known scent
        if (hasScentLead)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lastKnownScentPos, 0.4f);
            Gizmos.DrawLine(transform.position, lastKnownScentPos);
        }
    }

    private void OnDrawGizmos()
    {
        if (player == null) return;

        // vision cone
        DrawVisionCone();

        // raycast direction
        Vector3[] rays =
        {
            transform.forward,
            Quaternion.Euler(0,-15,0) * transform.forward, Quaternion.Euler(0,15,0) * transform.forward, // denser near front
            Quaternion.Euler(0,-35,0) * transform.forward, Quaternion.Euler(0,35,0) * transform.forward,
            Quaternion.Euler(0,-55,0) * transform.forward, Quaternion.Euler(0,55,0) * transform.forward,
            Quaternion.Euler(0,-75,0) * transform.forward, Quaternion.Euler(0,75,0) * transform.forward  // wider spread
        };

        foreach (var dir in rays)
        {
            if (Physics.Raycast(EyePos, dir, out RaycastHit hit, rayDistance, visionMask))
            {
                // hit type by colour
                if (hit.collider.CompareTag("Player")) Gizmos.color = Color.magenta;
                else if (hit.collider.CompareTag("SolidObj") || hit.collider.CompareTag("Walls")) Gizmos.color = Color.red; // solid obstructions
                else Gizmos.color = Color.cyan; // terrain shape

                Gizmos.DrawLine(EyePos, hit.point);
                Gizmos.DrawSphere(hit.point, 0.2f);
            }
            else
            {
                Gizmos.color = Color.yellow; // shows the line of sight
                Gizmos.DrawRay(EyePos, dir * rayDistance);
            }
        }

        // hearing radius
        Gizmos.color = new Color(0f, 0f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, 12f);

        // cloud trail line
        if (preyTrail != null && preyTrail.MainTrail.Count > 0)
        {
            float closestDist = float.MaxValue;
            Vector3 closestPoint = transform.position;
            foreach (Vector3 point in preyTrail.MainTrail)
            {
                float dist = Vector3.Distance(transform.position, point);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPoint = point;
                }
            }
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, closestPoint);
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f); // transparent radius around main trail points
            Gizmos.DrawWireSphere(closestPoint, 6f);
        }
    }
}
