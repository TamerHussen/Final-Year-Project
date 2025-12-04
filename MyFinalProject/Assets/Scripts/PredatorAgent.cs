using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody))]
public class PredatorAgent : Agent
{
    [Header("Target")]
    public Transform player;           // The prey
    public float catchDistance = 1.5f; // How close counts as catching

    [Header("Movement")]
    public float moveSpeed = 10f;
    public float rotateSpeed = 180f;

    private Rigidbody rb;
    private float timeSinceLastSeen = 0f;
    private float episodeTimer = 0f;
    public float maxEpisodeTime = 30f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset internal state
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        timeSinceLastSeen = 0f;
        episodeTimer = 0f;

        // Reset predator position
        transform.localPosition = new Vector3(
            Random.Range(-5f, 5f),
            1f,
            Random.Range(-5f, 5f)
        );

        // Optionally reset player here if needed
    }

    // OBSERVATIONS
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        bool isPlayerVisible = HasLineOfSight();

        // Observations
        sensor.AddObservation(distance);
        sensor.AddObservation(toPlayer.normalized);

        sensor.AddObservation(rb.linearVelocity);

        sensor.AddObservation(isPlayerVisible ? 1f : 0f);
        sensor.AddObservation(timeSinceLastSeen);
    }

    bool HasLineOfSight()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, 50f))
        {
            return hit.transform == player;
        }
        return false;
    }

    // ACTIONS → MOVEMENT
    public override void OnActionReceived(ActionBuffers actions)
    {
        float forward = actions.ContinuousActions[0]; // -1 to 1
        float strafe = actions.ContinuousActions[1]; // -1 to 1
        float turn = actions.ContinuousActions[2]; // -1 to 1

        // Apply movement
        Vector3 move = (transform.forward * forward + transform.right * strafe) * moveSpeed;
        rb.AddForce(move, ForceMode.Acceleration);

        // Apply turning
        transform.Rotate(Vector3.up * turn * rotateSpeed * Time.deltaTime);

        // Rewards ---------------------------------------------------
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        // Small time penalty
        AddReward(-0.0005f);

        // Reward for closing distance
        AddReward(-(distance * 0.01f));

        // Distance bonus if predator sees the player
        if (HasLineOfSight())
            AddReward(+0.002f);
        else
            timeSinceLastSeen += Time.deltaTime;

        // Catching logic
        if (distance < catchDistance)
        {
            AddReward(+1f);
            EndEpisode();
        }

        // Episode time
        episodeTimer += Time.deltaTime;
        if (episodeTimer >= maxEpisodeTime)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    // HEURISTIC for testing without training
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;

        c[0] = Input.GetAxis("Vertical");       // forward/back
        c[1] = Input.GetAxis("Horizontal");     // strafe
        c[2] = Input.GetKey(KeyCode.Q) ? -1 :
               Input.GetKey(KeyCode.E) ? 1 : 0; // turn left/right
    }
}
