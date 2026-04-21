using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Reference")]
    public Camera playerCamera;
    public CharacterController characterController;
    public StaminaSystem staminaSystem;

    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;
    public float sprintSpeed = 6.0f;
    public float gravity = 10f;
    public float jumpForce = 5f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;

    [Header("Camera Settings")]
    public float viewSensitivity = 120f;
    public Transform cameraAnchor;

    [Header("Crouch Settings")]
    public float crouchSpeed = 1.5f;
    private float originalHeight = 2f;
    private float crouchtargetHeight;
    public LayerMask obstacleLayer;

    [Header("Hidding System")]
    public float hiddenTimer = 0f;
    public bool isExposed = false;
    public bool inSoftObj = false;

    [Header("Throwing System")]
    public GameObject throwablePrefab;
    public Transform throwPoint;
    public float throwForce = 15f;
    public int maxAmmo = 3;
    public float ammoRechargeRate = 5f;
    private float currentAmmo;
    private float rechargeTimer = 0f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] WalkSFX;
    public AudioClip[] SprintSFX;
    public AudioClip JumpSFX;
    public AudioClip LandSFX;
    public AudioClip CrouchSFX;
    public AudioClip StandUpSFX;
    public AudioClip ExposedSFX;

    public Animator animator;

    private bool canStandUp = true;
    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;

    private Vector2 moveInput;   // Input System movement
    private Vector2 lookInput;   // Input System look
    private Vector3 moveDirection;
    private float xRotation;

    private float currentZRotation = 0f;
    private float targetZRotation = 0f;
    private float tiltSpeed = 5f;

    private float footstepTimer = 0f;
    private float footstepInterval = 0.5f;

    private bool wasGroundedLastFrame = false;
    private bool wasExposedLastFrame = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        originalHeight = characterController.height;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        viewSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 120f);

        originalHeight = characterController.height;
        crouchtargetHeight = originalHeight / 2f;

        currentAmmo = maxAmmo;
        FindFirstObjectByType<GameUI>().UpdateAmmoUI((int)currentAmmo, maxAmmo);
    }

    public void UpdateSensitivity(float newSensitivity)
    {
        viewSensitivity = newSensitivity;
    }

    void Update()
    {
        HandleView();
        HandleMovement();
        HandleHeadTilt();
        HandleHiddenTimer();
        UpdateAnimator();

        if (characterController.height < originalHeight)
            CheckObstaclesAbove();

        rechargeTimer += Time.deltaTime;
        if (rechargeTimer >= ammoRechargeRate && currentAmmo < maxAmmo)
        {
            currentAmmo++;
            rechargeTimer = 0f;

            FindFirstObjectByType<GameUI>().UpdateAmmoUI((int)currentAmmo, maxAmmo);
        }
    }

    void LateUpdate()
    {
        if (cameraAnchor != null)
        {
            playerCamera.transform.position = cameraAnchor.position;
        }
    }

    // HIDDING SYSTEM
    void HandleHiddenTimer()
    {
        if (inSoftObj)
        {
            hiddenTimer += Time.deltaTime;
            if (hiddenTimer > 10f)
            {
                if (!wasExposedLastFrame)
                {
                    PlaySound(ExposedSFX);
                }
                isExposed = true;
                // i might add debug or damage later
            }
        }
        else
        {
            hiddenTimer = 0f;
            isExposed = false;
        }
        wasExposedLastFrame = isExposed;
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

    // INPUT SYSTEM CALLBACKS
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && CheckIfGrounded() && canStandUp)
        {
            StartCoroutine(ExecuteDelayedJump());
        }
    }

    public void OnSprint(InputValue value)
    {
        if (value.isPressed)
        {
            isSprinting = !isSprinting;

            if (isSprinting && isCrouching && canStandUp)
            {
                ToggleCrouch(false);
            }
        }
    }

    public void OnCrouch(InputValue value)
    {
        if (value.isPressed)
        {
            if (!isCrouching)
            {
                ToggleCrouch(true);
                isSprinting = false;
            }
            else if (canStandUp)
            {
                ToggleCrouch(false);
            }
        }
    }

    // throw object
    public void OnFire(InputValue value)
    {
        if (value.isPressed && currentAmmo >= 1)
        {
            currentAmmo--;

            FindFirstObjectByType<GameUI>().UpdateAmmoUI((int)currentAmmo, maxAmmo);


            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.4f, 0.5f, 0));
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(100f);
            }

            Vector3 throwDirection = (targetPoint - throwPoint.position).normalized;

            // spawn and throw
            GameObject rock = Instantiate(throwablePrefab, throwPoint.position, Quaternion.LookRotation(throwDirection));
            Rigidbody rb = rock.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
            }
        }
    }

    private void ToggleCrouch(bool state)
    {
        isCrouching = state;
        crouchtargetHeight = state ? originalHeight - 1f : originalHeight;
        PlaySound(state ? CrouchSFX : StandUpSFX);

        StopAllCoroutines();
        StartCoroutine(ChangeHeightSmoothly());
    }

    // MOVEMENT
    private void HandleMovement()
    {
        Vector3 horizontalMovement = new Vector3(moveInput.x, 0f, moveInput.y);
        horizontalMovement = transform.TransformDirection(horizontalMovement);

        if (isSprinting && staminaSystem != null && !staminaSystem.CanSprint())
        {
            isSprinting = false;
        }

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        horizontalMovement *= speed;

        bool groundThisFrame = characterController.isGrounded;

        if (groundThisFrame)
        {
            if (!wasGroundedLastFrame && moveDirection.y < -2f)
            {
                PlaySound(LandSFX);
                if (animator != null) animator.SetTrigger("onLand");
            }

            isGrounded = true;
            if (moveDirection.y < 0)
                moveDirection.y = -2f;

        }
        wasGroundedLastFrame = groundThisFrame;

        moveDirection.x = horizontalMovement.x;
        moveDirection.z = horizontalMovement.z;
        moveDirection.y -= gravity * Time.deltaTime; // Apply gravity

        characterController.Move(moveDirection * Time.deltaTime);

        float velo = characterController.velocity.magnitude;

        // footstep audio
        if (!isCrouching && isGrounded && velo > 0.2f)
        {
            footstepTimer += Time.deltaTime;
            float interval = isSprinting ? footstepInterval * 0.5f : footstepInterval;
            if (footstepTimer >= interval) // louder sound when sprinting
            {
                AudioClip[] currentFootstep = isSprinting ? SprintSFX : WalkSFX;

                if (currentFootstep.Length > 0)
                {
                    int randomIndex = Random.Range(0, currentFootstep.Length);
                    PlaySound(currentFootstep[randomIndex]);
                }
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        if (!isCrouching)
        {
            if (isSprinting && velo > 1f) // louder sound when sprinting
                SoundEmitter.Emit(transform.position, velo * 1.5f, SoundEmitter.SoundSource.Player);
            else if (velo > 0.5f) // walking sound
                SoundEmitter.Emit(transform.position, velo, SoundEmitter.SoundSource.Player);
        }
    }

    // check ground
    private bool CheckIfGrounded()
    {
        return Physics.CheckSphere(transform.position, groundCheckDistance, groundLayer);
    }

    // VIEW / CAMERA
    private void HandleView()
    {
        float mouseX = lookInput.x * viewSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * viewSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    // HEAD TILT
    private void HandleHeadTilt()
    {
        if (moveInput.x > 0.1f)
            targetZRotation = -1.5f;
        else if (moveInput.x < -0.1f)
            targetZRotation = 1.5f;
        else
            targetZRotation = 0f;

        currentZRotation = Mathf.Lerp(currentZRotation, targetZRotation, Time.deltaTime * tiltSpeed);

        // only tilt camera not body
        Vector3 camEuler = playerCamera.transform.localEulerAngles;
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, camEuler.y, currentZRotation);
    }

    // ANIMATOR
    private void UpdateAnimator()
    {
        if (animator == null) return;
        Vector3 horizontalVel = characterController.velocity;
        horizontalVel.y = 0f;

        float speed = horizontalVel.magnitude;
        animator.SetBool("isMoving", speed > 0.1f);
        animator.SetBool("isSprinting", isSprinting && speed > 0.5f);
        animator.SetBool("isCrouching", isCrouching);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isHiding", inSoftObj && isCrouching);
        animator.SetBool("isExposed", isExposed);
        animator.SetFloat("moveSpeed", speed);

        TrailMarker trail = GetComponent<TrailMarker>();
        if (trail != null)
        {
            // mask scent when in bush
            trail.isScentMasked = inSoftObj && isCrouching;
        }
    }

    // CROUCH HANDLING
    private IEnumerator ChangeHeightSmoothly()
    {
        float elapsed = 0;
        float startHeight = characterController.height;
        float targetHeight = crouchtargetHeight;

        float startCenterY = startHeight / 2f;
        float targetCenterY = targetHeight / 2f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * crouchSpeed;
            float currentHeight = Mathf.Lerp(startHeight, targetHeight, elapsed);
            float currentCenter = Mathf.Lerp(startCenterY, targetCenterY, elapsed);

            characterController.height = currentHeight;
            characterController.center = new Vector3(0, currentCenter, 0);
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = new Vector3(0, targetCenterY, 0);

    }

    // JUMP HANDLE
    private IEnumerator ExecuteDelayedJump()
    {
        if (animator != null)
        {
            animator.ResetTrigger("onJump");
            animator.SetTrigger("onJump");
        }

        yield return new WaitForSeconds(0.15f);

        if (isGrounded)
        {
            moveDirection.y = jumpForce;
            isGrounded = false;
            PlaySound(JumpSFX);
        }
    }

    private void CheckObstaclesAbove()
    {
        Vector3 rayStart = transform.position + Vector3.up * (characterController.height - 0.1f);
        float rayDistance = (originalHeight - characterController.height) + 0.2f;

        canStandUp = !Physics.SphereCast(rayStart, 0.4f, Vector3.up, out RaycastHit hit, rayDistance, obstacleLayer);

        Debug.DrawRay(rayStart, Vector3.up * rayDistance, canStandUp ? Color.green : Color.red);
    }

    // audio helper
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
