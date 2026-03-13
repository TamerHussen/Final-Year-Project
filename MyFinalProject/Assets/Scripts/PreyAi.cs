using System.Runtime.CompilerServices;
using UnityEngine;

public class PreyAi : MonoBehaviour
{
    public Transform predator;
    public float fleeRadius = 15f;

    public float moveSpeed = 1.5f;
    public float turnSpeed = 120f;
    public float changeDirectionInterval = 2f;
    public CharacterController controller;

    private Vector3 moveDirection;
    private float timer;

    // simple gait state
    private bool isCrouching = false;
    private bool isSprinting = false;
    private float gaitTimer = 0f;
    public float gaitChangeInterval = 3f;

    public float hiddenTimer = 0f;
    public bool isExposed = false;
    public bool inSoftObj = false;

    void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        ChooseNewDirection();
        timer = changeDirectionInterval;
        gaitTimer = gaitChangeInterval * Random.value;
    }

    void Update()
    {
        HandleHiddenTimer();

        // flee logic so the training prey ai runs away from the ml agent
        if (predator != null && Vector3.Distance(transform.position, predator.position) < fleeRadius)
        {
            Vector3 dirAwayFromPredator = (transform.position - predator.position).normalized;
            dirAwayFromPredator.y = 0;
            moveDirection = dirAwayFromPredator;
            isSprinting = true;
            isCrouching = false;

            // let the prey ai to hide in bushes/ softobj
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, 8f); 
            foreach (var obj in nearbyObjects)
            {
                if (obj.CompareTag("SoftObj"))
                {
                    Vector3 dirToHide = (obj.transform.position - transform.position).normalized;
                    dirToHide.y = 0;
                    moveDirection = (moveDirection + (dirToHide * 1.5f)).normalized;
                    break;
                }
            } 
        }

        timer -= Time.deltaTime;
        gaitTimer -= Time.deltaTime;

        if (gaitTimer <= 0f)
        {
            float r = Random.value;
            if (r < 0.15f)
            {
                isCrouching = true;
                isSprinting = false;
            }
            else if (r < 0.5f)
            {
                isCrouching = false;
                isSprinting = false;
            }
            else
            {
                isCrouching = false;
                isSprinting = true;
            }
            gaitTimer = gaitChangeInterval * (0.5f + Random.value);
        }

        if (timer <= 0f && (predator == null || Vector3.Distance(transform.position, predator.position) >= fleeRadius))
        {
            ChooseNewDirection();
            timer = changeDirectionInterval;
        }
        // prevent sticking to the wall and solidobj
        if (Physics.Raycast(transform.position, moveDirection, out RaycastHit wallHit, 3f))
        {
            if (wallHit.collider.CompareTag("SolidObj") || wallHit.collider.CompareTag("Walls"))
            {
                Vector3 avoidDir = wallHit.normal;
                avoidDir.y = 0;
                moveDirection = (moveDirection + (avoidDir*2f)).normalized;
            }
        }

        Vector3 targetDir = moveDirection;
        targetDir.y = 0;
        if (targetDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        float currentSpeed = moveSpeed;
        if (isSprinting) currentSpeed *= 2.0f;
        if (isCrouching) currentSpeed *= 0.5f;

        Vector3 horizontalMove = transform.forward * currentSpeed;
        horizontalMove.y -= 9.81f * Time.deltaTime; // gravity
        controller.Move(horizontalMove * Time.deltaTime);

        float speed = controller.velocity.magnitude;

        if (!isCrouching)
        {
            if (isSprinting)
            {
                if (speed > 0.2f)
                    SoundEmitter.Emit(transform.position, Mathf.Clamp01(speed / 4f * 1.5f));
            }
            else
            {
                if (speed > 0.1f)
                    SoundEmitter.Emit(transform.position, Mathf.Clamp01(speed / 4f));
            }
        }
        else
        {
            //crouch has chance to emit weak sound if moving
            if (speed > 0.05f && Random.value < 0.02f)
                SoundEmitter.Emit(transform.position, 0.05f);
        }

    }

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


    void ChooseNewDirection()
    {
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
    }
}
