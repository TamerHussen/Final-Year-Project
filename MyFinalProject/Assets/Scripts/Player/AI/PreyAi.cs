using UnityEngine;

public class PreyAi : MonoBehaviour
{
    [Header("Reference")]
    public Transform predator;
    public CharacterController controller;

    [Header("Movement")]
    public float moveSpeed = 2.75f;
    public float turnSpeed = 180f;
    public float changeDirectionInterval = 2f;


    [Header("Radius")]
    public float fleeRadius = 22f;
    public float panicRadius = 10f;
    public float hideSearchRadius = 16f;

    [Header("Escape Routing")]
    public float zigzagStrength = 0.35f;
    public float zigzagFrequency = 1.8f;
    public float wallAvoidLookAhead = 2.5f;

    [Header("Gait")]
    public bool isCrouching = false;
    public bool isSprinting = false;
    public float gaitChangeInterval = 3f;

    [Header("Hiding State")]
    public float hiddenTimer = 0f;
    public bool isExposed = false;
    public bool inSoftObj = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip WalkSFX;
    public AudioClip SprintSFX;
    public AudioClip PanicSFX;
    public AudioClip HideSFX;

    public Animator animator;

    private TrailMarker trail;
    private Vector3 moveDirection;
    private float timer;
    private float gaitTimer = 0f;
    private float velocityY = 0f;

    private Vector3 lastSafeHidingSpot = Vector3.zero;
    private bool hasSafeSpotMemory = false;
    private bool wasInPanicLastFrame = false;
    private bool wasHidingLastFrame = false;
    private float footstepTimer = 0f;
    private float footstepInterval = 0.4f;

    void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        trail = GetComponent<TrailMarker>();
        ChooseNewDirection();
        timer = changeDirectionInterval;
        gaitTimer = gaitChangeInterval * Random.value;
    }

    void Update()
    {
        HandleHiddenTimer();

        float distToPred = predator != null ? Vector3.Distance(transform.position, predator.position) : float.MaxValue;

        bool predatorVisible = predator != null && CheckIfPredatorVisible(distToPred);
        bool inPanic = distToPred < panicRadius;
        bool inFleeZone = distToPred < fleeRadius;

        // flee logic so the training prey ai runs away from the ml agent
        if (predator != null && inFleeZone)
        {
            HandleFleeLogic(distToPred, predatorVisible, inPanic);
        }
        else
        {
            HandleWander();
        }

        // panic sound effect trigger
        if (inPanic && !wasInPanicLastFrame)
        {
            PlaySound(PanicSFX);
            if (animator != null)
            {
                animator.SetTrigger("onPanic");
            }
        }
        wasInPanicLastFrame = inPanic;

        // hiding sound effect trigger
        bool isHiding = inSoftObj && isCrouching;
        if (isHiding && !wasHidingLastFrame)
        {
            PlaySound(HideSFX);
        }
        wasHidingLastFrame = isHiding;

        ApplyMovement();
        EmitSounds();
        UpdateAnimator();
    }

    // flee logic
    void HandleFleeLogic(float distToPred, bool predatorVisible, bool inPanic)
    {
        // panic mode
        if (inPanic)
        {
            SetScentMasked(false);

            Vector3 away = (transform.position - predator.position).normalized;
            away.y = 0;

            float zigzag = Mathf.Sin(Time.time * zigzagFrequency) * zigzagStrength;
            moveDirection = (away + Vector3.Cross(Vector3.up, away) * zigzag).normalized;

            isSprinting = true;
            isCrouching = false;
            return;
        }

        // hide mode
        bool predatorHasLos = PredatorCanSeeUs();
        bool shouldStayHidden = inSoftObj && !isExposed && !predatorHasLos && (distToPred > 10f || !predatorVisible);

        // prey hides when predator isnt too close and isnt exposed
        if (shouldStayHidden)
        {
            SetScentMasked(true); // no trail when hidden

            // sneak away when predator enters same bush
            if (distToPred < 4.5f)
            {
                Vector3 awayDir = (transform.position - predator.position).normalized;
                awayDir.y = 0;
                moveDirection = awayDir;

                isSprinting = false;
                isCrouching = true;

            }
            else
            {
                // stay still when predator not in bush
                moveDirection = Vector3.zero;
                isSprinting = false;
                isCrouching = true;

                // face away from predator so its easier to run away when needed
                Vector3 facingDir = (transform.position - predator.position).normalized;
                facingDir.y = 0;
                if (facingDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(facingDir);
                }

                // remeber safe zone
                lastSafeHidingSpot = transform.position;
                hasSafeSpotMemory = true;
            }
            return;
        }


        // run to find hiding spot
        SetScentMasked(false);
        isSprinting = true;
        isCrouching = false;

        Vector3 dirAway = (transform.position - predator.position).normalized;
        dirAway.y = 0;
        moveDirection = dirAway;

        // look for hidin spot
        Transform bestCover = FindBestCover(predatorVisible);
        if (bestCover != null)
        {
            Vector3 dirToCover = (bestCover.position - transform.position).normalized;
            dirToCover.y = 0;

            // smooth transition from fleeing to finding cover
            float coverWeight = predatorVisible ? 1.8f : 1.2f;
            moveDirection = (moveDirection + dirToCover * coverWeight).normalized;
        }
        else if (hasSafeSpotMemory && distToPred > 12f && !predatorVisible)
        {
            // look for last safe spot if cant find one near
            Vector3 dirToMemory = (lastSafeHidingSpot - transform.position).normalized;
            dirToMemory.y = 0;
            if (dirToMemory.sqrMagnitude > 0.1f)
            {
                moveDirection = (moveDirection + dirToMemory * 0.8f).normalized;
            }

            float mildZigzag = Mathf.Sin(Time.time * zigzagFrequency * 0.7f) * (zigzagStrength * 0.6f);

            moveDirection = (moveDirection + Vector3.Cross(Vector3.up, moveDirection) * mildZigzag).normalized;
        }
    }

    // cover search
    Transform FindBestCover(bool predatorVisible)
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, hideSearchRadius);
        Transform bestObj = null;
        float bestScore = float.MaxValue;

        foreach (var col in nearby)
        {
            if (!col.CompareTag("SoftObj")) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);

            // score = cover distance
            float score = dist;

            if (predator != null)
            {
                Vector3 predToCover = col.transform.position - predator.position;

                float alignment = Vector3.Dot(predToCover.normalized, (transform.position - predator.position).normalized);
                score -= alignment * 4f;
            }
            if (score < bestScore)
            {
                bestScore = score;
                bestObj = col.transform;
            }
        }
        return bestObj;
    }

    // wander
    void HandleWander()
    {
        SetScentMasked(false);

        timer -= Time.deltaTime;
        gaitTimer -= Time.deltaTime;

        if (gaitTimer <= 0f)
        {
            float r = Random.value;
            // crouch
            if (r < 0.15f)
            {
                isCrouching = true;
                isSprinting = false;
            }
            // walk
            else if (r < 0.55f)
            {
                isCrouching = false;
                isSprinting = false;
            }
            // sprint
            else
            {
                isCrouching = false;
                isSprinting = true;
            }
            gaitTimer = gaitChangeInterval * (0.5f + Random.value);
        }

        if (timer <= 0f)
        {
            ChooseNewDirection();
            timer = changeDirectionInterval;
        }
    }

    // line of sight - check if prey can see predator
    bool CheckIfPredatorVisible(float distToPred)
    {
        Vector3 dirToPred = (predator.position - transform.position).normalized;
        // prevent raycast from clipping
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToPred, out RaycastHit hit, distToPred))
        {
            if (hit.collider.CompareTag("SoftObj") || hit.collider.CompareTag("SolidObj") || hit.collider.CompareTag("Walls"))
                return false; // line of sight block
        }
        return true;
    }

    // check if predator is looking towards prey
    bool PredatorCanSeeUs()
    {
        if (predator == null) return false;
        Vector3 eyePos = predator.position + Vector3.up * 1.0f;
        Vector3 ourPos = transform.position + Vector3.up * 0.5f;
        Vector3 dir = (ourPos - eyePos).normalized;
        float dist = Vector3.Distance(eyePos, ourPos);

        if (Physics.Raycast(eyePos, dir, out RaycastHit hit, dist))
        {
            if (hit.collider.CompareTag("SoftObj") || hit.collider.CompareTag("SolidObj") || hit.collider.CompareTag("Walls"))
                return false;
        }
        return true;
    }

    void ApplyMovement()
    {
        if (isCrouching && moveDirection.sqrMagnitude < 0.01f)
        {
            if (controller.isGrounded)
            {
                velocityY = -2f;
            }
            else
            {
                velocityY -= 9.81f * Time.deltaTime;
            }
            controller.Move(new Vector3(0, velocityY, 0) * Time.deltaTime);

            return;
        }

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // prevent sticking to the wall and solidobj
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, moveDirection, out RaycastHit wallHit, wallAvoidLookAhead))
            {
                if (wallHit.collider.CompareTag("SolidObj") || wallHit.collider.CompareTag("Walls"))
                {
                    Vector3 avoidDir = wallHit.normal;
                    avoidDir.y = 0;
                    moveDirection = Vector3.ProjectOnPlane(moveDirection, avoidDir).normalized;
                }
            }
        }

        Vector3 targetDir = new Vector3(moveDirection.x, 0, moveDirection.z);
        if (targetDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        float speed = moveSpeed;
        if (isSprinting) speed *= 2.0f;
        if (isCrouching) speed *= 0.5f;

        if (controller.isGrounded)
        {
            velocityY = -2f;
        }
        else
        {
            velocityY -= 9.81f * Time.deltaTime;
        }

        Vector3 move = transform.forward * speed;
        move.y = velocityY;
        controller.Move(move * Time.deltaTime);
    }
    // scent masking
    void SetScentMasked(bool masked)
    {
        if (trail != null) trail.isScentMasked = masked;
    }

    // sound emission
    void EmitSounds()
    {
        float speed = controller.velocity.magnitude;

        if (!isCrouching)
        {
            if (isSprinting && speed > 0.2f)
            {
                SoundEmitter.Emit(transform.position, Mathf.Clamp01(speed / 4f * 1.5f), SoundEmitter.SoundSource.Prey);
            }
            else if (speed > 0.1f)
            {
                SoundEmitter.Emit(transform.position, Mathf.Clamp01(speed / 4f), SoundEmitter.SoundSource.Prey);
            }

            // footstep audio
            footstepTimer += Time.deltaTime;
            float interval = isSprinting ? footstepInterval * 0.5f : footstepInterval;
            if (footstepTimer >= interval && speed > 0.2f)
            {
                PlaySound(isSprinting ? SprintSFX : WalkSFX);
                footstepTimer = 0;
            }
        }
        else
        {
            //crouch has chance to emit weak sound if moving
            if (speed > 0.05f && Random.value < 0.02f)
                SoundEmitter.Emit(transform.position, 0.05f, SoundEmitter.SoundSource.Prey);
        }
    }

    // animator
    void UpdateAnimator()
    {
        if (animator == null) return;
        float speed = controller.velocity.magnitude;
        animator.SetBool("isMoving", speed > 0.1f);
        animator.SetBool("isSprinting", isSprinting);
        animator.SetBool("isCrouching", isCrouching);
        animator.SetBool("isHiding", inSoftObj && isCrouching);
        animator.SetFloat("moveSpeed", speed);
    }

    // hiding timer
    void HandleHiddenTimer()
    {
        if (inSoftObj)
        {
            hiddenTimer += Time.deltaTime;
            if (hiddenTimer > 10f)
            {
                isExposed = true;
                // i might add debug or damage later
            }
        }
        else
        {
            hiddenTimer = 0f;
            isExposed = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("SoftObj"))
        {
            inSoftObj = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SoftObj"))
        {
            inSoftObj = false;
        }
    }

    // direction helper
    void ChooseNewDirection()
    {
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
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
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, fleeRadius);
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, panicRadius);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, hideSearchRadius);

        if (hasSafeSpotMemory)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(lastSafeHidingSpot, 0.4f);
            Gizmos.DrawLine(transform.position, lastSafeHidingSpot);
        }
    }
}
