using UnityEngine;

public class PreyAi : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float turnSpeed = 120f;
    public float changeDirectionInterval = 2f;
    public CharacterController controller;

    private Vector3 moveDirection;
    private float timer;

    void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        ChooseNewDirection();
        timer = changeDirectionInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ChooseNewDirection();
            timer = changeDirectionInterval;
        }

        Vector3 targetDir = moveDirection;
        targetDir.y = 0;
        if (targetDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        Vector3 horizontalMove = transform.forward * moveSpeed;
        horizontalMove.y -= 9.81f * Time.deltaTime; // gravity
        controller.Move(horizontalMove * Time.deltaTime);
    }


    void ChooseNewDirection()
    {
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
    }
}
