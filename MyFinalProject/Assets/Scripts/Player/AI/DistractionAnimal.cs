using UnityEngine;

public class DistractionAnimal : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.2f;
    public float turnSpeed = 90f;
    public float wanderInterval = 3f;
    public float wanderRadius = 25f;

    [Header("Fleeing")]
    public float fleeRadius = 12f;
    public float fleeSpeed = 10f;
    public float fleeCoastDuration = 4f;
    public LayerMask threatMask;

    [Header("Sound")]
    public float soundEmitInterval = 5f;
    public float fleeEmitVolume = 0.8f;
    public float idleEmitVolume = 0.2f;

    [Header("Audio 3D Settings")]
    public float audioMinDistance = 3f;
    public float audioMaxDistance = 20f;

    [Header("Variety Settings")]
    public Renderer monsterRenderer;
    public Light glowLight;
    public bool randomiseOnStart = true;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip IdleSFX;
    public AudioClip fleeSFX;

    public Animator animator;

    private CharacterController controller;
    private Vector3 spawnPos;
    private Vector3 moveDir;
    private Vector3 lastFleeDir;
    private float wanderTimer = 0f;
    private float soundTimer = 0f;
    private float velocityY = 0f;
    private float fleeCoastTimer = 0f;
    private bool isFleeing = false;
    private bool wasFleeingLastFrame = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        spawnPos = transform.position;
        PickWanderDir();
        wanderTimer = wanderInterval * Random.value;
        soundTimer = soundEmitInterval * Random.value;

        if (audioSource != null)
        {
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = audioMinDistance;
            audioSource.maxDistance = audioMaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.dopplerLevel = 0f;
        }

        if (randomiseOnStart) ApplyRandomVariety();
    }

    // Update is called once per frame
    void Update()
    {
        soundTimer += Time.deltaTime;
        wanderTimer += Time.deltaTime;

        Transform threat = FindNearbyThreat();
        bool threatNearby = threat != null;

        if (threatNearby)
        {
            // threat nearby
            fleeCoastTimer = fleeCoastDuration;
            lastFleeDir = (transform.position - threat.position).normalized;
            lastFleeDir.y = 0;
            isFleeing = true;
        }
        else if (fleeCoastTimer > 0f)
        {
            // threat gone by keep running incase
            fleeCoastTimer -= Time.deltaTime;
            isFleeing = true;
        }
        else
        {
            isFleeing = false;
        }

        // started to fler
        if (isFleeing && !wasFleeingLastFrame)
        {
            PlaySound(fleeSFX);
            SoundEmitter.Emit(transform.position, fleeEmitVolume, SoundEmitter.SoundSource.Animal);
        }
        wasFleeingLastFrame = isFleeing;

        // movements
        if (isFleeing)
        {
            moveDir = threatNearby
                ? (transform.position - threat.position).normalized
                : lastFleeDir;
            moveDir.y = 0;
        }
        else
        {
            HandleWander();
        }

        ApplyMovement(isFleeing ? fleeSpeed : moveSpeed);
        HandleSound();
        UpdateAnimator();
    }

    // random size, and light colour and intensity
    public void ApplyRandomVariety()
    {
        if (monsterRenderer == null) return;

        // random light colour
        Color randomColor = Color.HSVToRGB(Random.value, 0.7f, 1f);

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        monsterRenderer.GetPropertyBlock(propBlock);

        propBlock.SetColor("_BaseColor", randomColor);
        propBlock.SetColor("_EmissionColor", randomColor * 2f);

        monsterRenderer.SetPropertyBlock(propBlock);

        // random light colour and intensity
        if (glowLight != null)
        {
            glowLight.color = randomColor;
            glowLight.intensity = Random.Range(0.5f, 2f);
        }

        // random size
        float randomScale = Random.Range(1.5f, 4f);
        transform.localScale = Vector3.one * randomScale;

    }

    // threat detection
    Transform FindNearbyThreat()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, fleeRadius, threatMask);
        if (hits.Length == 0) return null;

        Transform closest = null;
        float closestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit.transform;
            }
        }
        return closest;
    }

    // wander
    void HandleWander()
    {
        if (wanderTimer >= wanderInterval)
        {
            PickWanderDir();
            wanderTimer = 0f;
        }

        // stay in spawn radius
        float distFromSpawn = Vector3.Distance(transform.position, spawnPos);
        if (distFromSpawn > wanderRadius)
        {
            Vector3 back = (spawnPos - transform.position).normalized;
            back.y = 0;
            moveDir = (moveDir + back * 2f).normalized;
        }

        // avoid wall
        if (Physics.Raycast(transform.position, moveDir, out RaycastHit wallHit, 2f))
        {
            if (wallHit.collider.CompareTag("SolidObj") || wallHit.collider.CompareTag("Walls"))
            {
                Vector3 avoidDir = wallHit.normal;
                avoidDir.y = 0;
                moveDir = (moveDir + avoidDir * 2f).normalized;
            }
        }
    }

    // movement
    void ApplyMovement(float speed)
    {
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDir.x, 0, moveDir.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded) velocityY = -2f;
        else velocityY -= 9.81f * Time.deltaTime;

        Vector3 move = transform.forward * speed;
        move.y = velocityY;
        controller.Move(move * Time.deltaTime);
    }

    // sound
    void HandleSound()
    {
        if (soundTimer < soundEmitInterval) return;

        if (isFleeing)
        {
            // fleeing animal causes loud distraction
            SoundEmitter.Emit(transform.position, fleeEmitVolume, SoundEmitter.SoundSource.Animal);
            PlaySound(fleeSFX);
        }
        else
        {
            // idle animal causes quieter distraction
            SoundEmitter.Emit(transform.position, idleEmitVolume, SoundEmitter.SoundSource.Animal);
            PlaySound(IdleSFX);
        }
        soundTimer = 0;
    }

    // reset animal spawn
    public void ResetAnimal()
    {
        spawnPos = transform.position;
        fleeCoastTimer = 0f;
        isFleeing = false;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        moveDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    // animator
    void UpdateAnimator()
    {
        if (animator == null) return;
        float speed = controller.velocity.magnitude;
        animator.SetBool("isMoving", speed > 0.1f);
        animator.SetBool("isFleeing", isFleeing);
        animator.SetFloat("moveSpeed", speed);
    }

    // direction helper
    void PickWanderDir()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        moveDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
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
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, fleeRadius);
        Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
        Gizmos.DrawWireSphere(spawnPos, wanderRadius);
        Gizmos.color = new Color(1f, 1f, 0f, 0.08f);
        Gizmos.DrawWireSphere(transform.position, audioMaxDistance);
    }
}
