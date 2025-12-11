using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public CharacterController characterController;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float sprintSpeed = 4f;
    public float gravity = 10f;
    public float jumpForce = 5f;

    [Header("Camera Settings")]
    public float viewSensitivity = 120f;

    [Header("Crouch Settings")]
    public float crouchSpeed = 5f;
    private float originalHeight = 2f;
    private float targetHeight;

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
    private float rotationSpeed = 5f;

    private Vector3 previousPosition;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        originalHeight = characterController.height;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleView();
        HandleMovement();
        HandleHeadTilt();

        if (characterController.height < originalHeight)
            CheckObstaclesAbove();
    }

    // INPUT SYSTEM CALLBACKS

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && isGrounded)
        {
            moveDirection.y = jumpForce;
            isGrounded = false;
        }
    }

    public void OnSprint(InputAction.CallbackContext ctx)
    {
        isSprinting = ctx.ReadValue<float>() > 0.5f;
    }

    public void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            if (!isCrouching)
            {
                targetHeight = originalHeight - 1f;
                isCrouching = true;
            }
            else if (canStandUp)
            {
                targetHeight = originalHeight;
                isCrouching = false;
            }

            StopAllCoroutines();
            StartCoroutine(ChangeHeightSmoothly());
        }
    }

    // MOVEMENT

    private void HandleMovement()
    {
        Vector3 horizontalMovement = new Vector3(moveInput.x, 0f, moveInput.y);
        horizontalMovement = transform.TransformDirection(horizontalMovement);

        float speed = isSprinting ? sprintSpeed : walkSpeed;

        horizontalMovement *= speed;

        if (characterController.isGrounded)
            isGrounded = true;

        moveDirection.x = horizontalMovement.x;
        moveDirection.z = horizontalMovement.z;

        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;

        characterController.Move(moveDirection * Time.deltaTime);

        previousPosition = transform.position;


        if (!isCrouching)
        {
            float movementSpeeed = characterController.velocity.magnitude; // footsteps equal sound

            if (movementSpeeed > 0.5f) // walking sound
                SoundEmitter.Emit(transform.position, movementSpeeed);


            if (isSprinting && movementSpeeed > 1f) // louder sound when sprinting
                SoundEmitter.Emit(transform.position, movementSpeeed * 1.5f);
        }


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

        currentZRotation = Mathf.Lerp(currentZRotation, targetZRotation, Time.deltaTime * rotationSpeed);
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, currentZRotation);
    }

    // CROUCH HANDLING

    private IEnumerator ChangeHeightSmoothly()
    {
        float elapsed = 0;
        float startHeight = characterController.height;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * crouchSpeed;
            characterController.height = Mathf.Lerp(startHeight, targetHeight, elapsed);
            yield return null;
        }

        characterController.height = targetHeight;
    }

    private void CheckObstaclesAbove()
    {
        if (Physics.Raycast(transform.position, Vector3.up, out _, 1.5f))
            canStandUp = false;
        else
            canStandUp = true;
    }
}
