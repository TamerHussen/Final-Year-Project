using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using System.Xml.Serialization;

[RequireComponent(typeof(NavMeshAgent))]
public class PredatorBT : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public TrailMarker preyTrail;
    public PreyAi preyAi;
    public Animator animator;

    [Header("Movement Settings")]
    public float baseSpeed = 3f;
    public float recognitionSpeedBoost = 1.9f;
    private bool isStalking = false;

    [Header("Vision Settings")]
    public float rayDistance = 45f; // better eyesight
    public float eyeHeight = 1.0f;
    public float targetHeight = 1.0f;
    public LayerMask visionMask; // walls, player, obstacles

    [Header("Stalking Settings")]
    public float recognitionThreshold = 0.7f;
    public float tauntDuration = 2.5f;

    [Header("Headstart")]
    public float headstartDuration = 15f; // give prey headstart

    [Header("Wander Settings")]
    public float wanderRadius = 20f;
    public float wanderInterval = 4f;

    [Header("Familiar Settings")]
    public List<GameObject> groundFamiliarPrefabs; // grounded familiars
    public List<GameObject> skyFamiliarPrefabs; // flying familiars
    public float timeBeforeSummon = 12f; // how long it cant see player before spawning familiars
    public float losGracePeriod = 8f;
    public float familiarCooldown = 45f; // prevent spamming
    public int maxActiveFamiliars = 3;

    [Header("Scent Tracking")]
    public float scentRadius = 6f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip RecogniseTargetSFX;
    public AudioClip LostTargetSFX;
    public AudioClip SummonFamiliarSFX;
    public AudioClip CatchPreySFX;

    private float stunTimer = 0f;

    // runtime state
    private NavMeshAgent navAgent;
    private BTNode rootNode;

    private float episodeTimer = 0f; // internal timer
    private float visionRecognitionTimer = 0f;
    private bool preyIsRecognised = false;
    private float tauntTimer = 0f;

    private float timeSinceSeen = 0f;
    private float timeSinceLastLos = 0f;
    private float familiarCooldownRemaining = 0f;
    private int activeFamiliarCount = 0;
    private bool nextSummonIsAerial = false;
    private List<GameObject> activeFamiliars = new List<GameObject>(); // track the active familiars

    private float wanderTimer = 0f;
    private Vector3 wanderTarget = Vector3.zero;
    private bool hasWanderTarget = false;

    private Vector3 lastSoundTarget = Vector3.zero;
    private float lastSoundVolume = 0f;

    private float headstartTimer;

    // used for debug UI
    public float TimeSinceSeen => timeSinceSeen;
    public bool IsRecognised => preyIsRecognised;
    public int ActiveFamiliarCount => activeFamiliarCount;
    public float FamiliarCooldDownRemaining => familiarCooldownRemaining;

    public string CurrentBehaviour { get; private set; } = "HEADSTART";

    private Vector3 EyePos => transform.position + Vector3.up * eyeHeight;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        headstartTimer = headstartDuration;

        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = baseSpeed;
        navAgent.angularSpeed = 250f;
        navAgent.acceleration = 12f;
        navAgent.stoppingDistance = 1.2f;

        BuildTree();
        ResetState();
    }

    // Update is called once per frame
    void Update()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f && navAgent != null)
            {
                navAgent.isStopped = false;
                navAgent.speed = baseSpeed;
            }
            return;
        }

        episodeTimer += Time.deltaTime;

        UpdateTimers();
        rootNode.Evalute();
        UpdateAnimator();
    }

    // stun logic
    public void ApplyStun(float duration)
    {
        stunTimer = duration;
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
        }
    }

    // ==================================
    //      build behaviour tree
    // ==================================

    void BuildTree()
    {
        // tree is evaluated top to bottom, top has higher priority
        rootNode = new BTSelector(new List<BTNode>
        {
            // Priority 1: headstart
            new BTSequence(new List<BTNode>
            {
                new BTCondition(() => episodeTimer < headstartDuration),
                new BTAction(DoHeadstart)
            }),

            // Priority 2: taunt
            new BTSequence(new List<BTNode>
            {
                new BTCondition(() => tauntTimer > 0f),
                new BTAction(DoTaunt)
            }),

            // Priority 3: strike
            new BTSequence(new List<BTNode>
            {
                new BTCondition(CheckLineOfSight),
                new BTCondition(() => preyIsRecognised),
                new BTAction(DoStrike)
            }),

            // Priority 4: stalk
            new BTSequence(new List<BTNode>
            {
                new BTCondition(CheckLineOfSight),
                new BTAction(DoStalk)
            }),

            // Priority 5: scent tracking
            new BTSequence(new List<BTNode>
            {
                new BTCondition(HasScentTrail),
                new BTAction(DoTrackScent)
            }),

            // Priority 6: sound tracking
            new BTSequence(new List<BTNode>
            {
                new BTCondition(HasRecentSound),
                new BTAction(DoTrackSound)
            }),

            // Priority 7: summon familiar
            new BTSequence(new List<BTNode>
            {
                new BTCondition(CanSummonFamiliar),
                new BTAction(DoSummonFamiliar)
            }),

            // Priority 8: wander
            new BTAction(DoWander)

        });
    }

    // ==================================
    //          timer updates
    // ==================================

    void UpdateTimers()
    {
        bool hasLOS = CheckLineOfSight();

        if (hasLOS)
        {
            timeSinceLastLos = 0f;
            timeSinceSeen = 0f;

            if (!preyIsRecognised)
            {
                visionRecognitionTimer += Time.deltaTime;
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
            timeSinceLastLos += Time.deltaTime;
            timeSinceSeen += Time.deltaTime;

            if (preyIsRecognised)
            {
                PlaySound(LostTargetSFX);
                if (animator != null) animator.SetTrigger("onLostTarget");
            }
            visionRecognitionTimer = 0f;
            preyIsRecognised = false;
        }

        if (!hasLOS) familiarCooldownRemaining -= Time.deltaTime;

        if (SoundEmitter.LastSoundVolume > 0.05f)
        {
            lastSoundTarget = SoundEmitter.LastSoundPos;
            lastSoundVolume = SoundEmitter.LastSoundVolume;
        }
    }

    // ==================================
    //          action nodes
    // ==================================

    // stand still during headstart
    NodeState DoHeadstart()
    {
        CurrentBehaviour = "HEADSTART";
        navAgent.ResetPath();
        navAgent.speed = 0f;
        return NodeState.Running;
    }

    // freeze a little after recognition
    NodeState DoTaunt()
    {
        CurrentBehaviour = "TAUNT";
        tauntTimer -= Time.deltaTime;

        if (tauntTimer == tauntDuration)
        {
            animator.SetTrigger("onTaunt");
        }

        navAgent.ResetPath();
        navAgent.speed = 0f;

        // look at player while taunting
        Vector3 lookDir = (player.position - transform.position);
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lookDir), 250f * Time.deltaTime);
        return NodeState.Running;
    }

    // chase at boosted speed
    NodeState DoStrike()
    {
        CurrentBehaviour = "STRIKING";
        navAgent.speed = baseSpeed * recognitionSpeedBoost;
        navAgent.SetDestination(player.position);
        return NodeState.Running;
    }

    // slowly approach while recognitionTimer builds
    NodeState DoStalk()
    {
        CurrentBehaviour = "STALKING";
        float ramp = 0.2f + (visionRecognitionTimer / recognitionThreshold) * 0.8f;
        navAgent.speed = baseSpeed * ramp;
        navAgent.SetDestination(player.position);
        return NodeState.Running;
    }

    // follow most recent scent trail
    NodeState DoTrackScent()
    {
        CurrentBehaviour = "TRACK_SCENT";
        navAgent.speed = baseSpeed;

        Vector3 freshest = preyTrail.MainTrail[preyTrail.MainTrail.Count - 1];
        navAgent.SetDestination(freshest);
        return NodeState.Running;
    }

    // move towards last heard sound source
    NodeState DoTrackSound()
    {
        CurrentBehaviour = "TRACK_SOUND";
        navAgent.speed = baseSpeed * 0.8f;
        navAgent.SetDestination(lastSoundTarget);
        return NodeState.Running;
    }

    // summon a familiar
    NodeState DoSummonFamiliar()
    {
        CurrentBehaviour = "SUMMONING";
        SummonFamiliar();
        return NodeState.Success;
    }

    // random patrol when completely lost
    NodeState DoWander()
    {
        CurrentBehaviour = "SEARCHING";
        navAgent.speed = baseSpeed * 0.6f;

        wanderTimer -= Time.deltaTime;

        if (!hasWanderTarget || wanderTimer <= 0f || navAgent.remainingDistance < 1.5f)
        {
            // choose new random point
            Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
            randomDir += transform.position;
            randomDir.y = transform.position.y;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                wanderTarget = hit.position;
                navAgent.SetDestination(wanderTarget);
                hasWanderTarget = true;
            }
            wanderTimer = wanderInterval;
        }

        return NodeState.Running;
    }

    // ==================================
    //          condition checks
    // ==================================

    bool CheckLineOfSight()
    {
        if (player == null) return false;
        Vector3 targetPos = player.position + Vector3.up * targetHeight;
        Vector3 dir = (targetPos - EyePos);
        if (dir.sqrMagnitude < 0.00001f) return false;

        if (Physics.Raycast(EyePos, dir.normalized, out RaycastHit hit, rayDistance, visionMask))
            return hit.collider.CompareTag("Player");

        return false;
    }

    bool HasScentTrail()
    {
        return preyTrail != null && preyTrail.MainTrail != null && preyTrail.MainTrail.Count > 0;
    }

    bool HasRecentSound()
    {
        return lastSoundVolume > 0.1f && lastSoundTarget != Vector3.zero;
    }

    bool CanSummonFamiliar()
    {
        bool trulyLost = timeSinceSeen > timeBeforeSummon;
        bool gracePassed = timeSinceLastLos > losGracePeriod;
        bool cooldownDone = familiarCooldownRemaining <= 0f;
        bool underCap = activeFamiliarCount < maxActiveFamiliars;
        return trulyLost && gracePassed && cooldownDone && underCap;
    }

    // ==================================
    //          familiar summoning
    // ==================================

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

        PlaySound(SummonFamiliarSFX);
        Debug.Log($"SEND OUT THE BEAST. familiar deployed: [{(pool == skyFamiliarPrefabs ? "SKY" : "GROUND")}] - {activeFamiliarCount} active");
    }

    // keep active familair count accurate
    public void OnFamiliarDespawned(GameObject familiar)
    {
        activeFamiliars.Remove(familiar);
        activeFamiliarCount = Mathf.Max(0, activeFamiliarCount - 1);
    }

    // ==================================
    //          catch detection
    // ==================================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlaySound(CatchPreySFX);
            if (animator != null) animator.SetTrigger("onCatch");
            Debug.Log("prey caught - game over");

            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
            }

            if (GameManager.instance != null)
            {
                GameManager.instance.OnPlayerCaught();
            }
        }
    }

    // ==================================
    //          episode reset
    // ==================================

    void ResetState()
    {
        episodeTimer = 0f;
        visionRecognitionTimer = 0f;
        preyIsRecognised = false;
        tauntTimer = 0f;
        timeSinceSeen = 0f;
        timeSinceLastLos = losGracePeriod;
        familiarCooldownRemaining = 0f;
        activeFamiliarCount = 0;
        nextSummonIsAerial = false;
        wanderTimer = 0f;
        hasWanderTarget = false;
        lastSoundTarget = Vector3.zero;
        lastSoundVolume = 0f;
        CurrentBehaviour = "HEADSTART";

        foreach (var familiar in activeFamiliars)
            if (familiar != null) Destroy(familiar);
        activeFamiliars.Clear();

        SoundEmitter.ResetSound();

        if (preyTrail != null) preyTrail.ResetTrail();
    }

    // ==================================
    //              animator
    // ==================================

    void UpdateAnimator()
    {
        if (animator == null) return;
        float speed = navAgent.velocity.magnitude;

        isStalking = !preyIsRecognised && speed > 0.1f;

        animator.SetBool("isMoving", speed > 0.1f);
        animator.SetBool("isSprinting", preyIsRecognised && speed > 1f);
        animator.SetBool("isCrouching", isStalking);
        animator.SetFloat("moveSpeed", speed);
    }

    // audio helper
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // ==================================
    //              gizmos
    // ==================================

    private void OnDrawGizmos()
    {
        if (player == null) return;

        // vision
        Gizmos.color = preyIsRecognised ? Color.red : Color.yellow;
        Gizmos.DrawRay(EyePos, (player.position - EyePos).normalized * rayDistance);

        // wander target
        if (hasWanderTarget)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(wanderTarget, 0.4f);
        }

        // sound target
        if (lastSoundVolume > 0.1f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(lastSoundTarget, 0.4f);
        }
    }
}
