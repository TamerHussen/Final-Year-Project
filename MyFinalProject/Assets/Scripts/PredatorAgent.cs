using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PredatorAgent : Agent
{
    [Header("References")]
    public Transform player;
    public Rigidbody rb;

    [Header("Movement Settings")]
    public float moveForce = 25f;
    public float turnSpeed = 120f;

    [Header("Vision Settings")]
    public float rayDistance = 15f;
    public LayerMask visionMask; // walls + player + obstacles

    private float lastDistanceToPlayer;
    private bool lastSeen = false;
    private float timeSinceSeen = 0f;

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        lastSeen = false;
        timeSinceSeen = 0;

        if (player != null)
            lastDistanceToPlayer = Vector3.Distance(transform.position, player.position);
    }

    // ------------------------------------------------------------
    //  Collect Observations
    // ------------------------------------------------------------
    public override void CollectObservations(VectorSensor sensor)
    {
        if (player == null)
        {
            sensor.AddObservation(0);
            return;
        }

        // Distance + direction to player
        float dist = Vector3.Distance(transform.position, player.position);
        Vector3 direction = (player.position - transform.position).normalized;

        sensor.AddObservation(dist);
        sensor.AddObservation(direction);

        // Agent movement
        sensor.AddObservation(rb.linearVelocity);

        // Line of sight
        bool visible = CheckLineOfSight();
        sensor.AddObservation(visible ? 1 : 0);
        sensor.AddObservation(timeSinceSeen);

        // Add Raycasts
        AddRaycastObservations(sensor);
    }

    void AddRaycastObservations(VectorSensor sensor)
    {
        Vector3[] rays =
        {
            transform.forward,
            Quaternion.Euler(0,-25,0) * transform.forward,
            Quaternion.Euler(0,25,0) * transform.forward,
            Quaternion.Euler(0,-50,0) * transform.forward,
            Quaternion.Euler(0,50,0) * transform.forward
        };

        foreach (var dir in rays)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, rayDistance, visionMask))
            {
                // Distance normalized
                sensor.AddObservation(hit.distance / rayDistance);

                // Hit type
                if (hit.collider.CompareTag("Player"))
                    sensor.AddObservation(2); // player
                else
                    sensor.AddObservation(1); // wall/obstacle
            }
            else
            {
                sensor.AddObservation(1f); // no hit
                sensor.AddObservation(0);  // nothing
            }
        }
    }

    bool CheckLineOfSight()
    {
        if (Physics.Raycast(transform.position, (player.position - transform.position),
            out RaycastHit hit, rayDistance, visionMask))
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
        float forward = actions.ContinuousActions[0]; // move forward/back
        float strafe = actions.ContinuousActions[1]; // move left/right
        float turn = actions.ContinuousActions[2]; // rotate

        // ★ Movement
        Vector3 force = transform.forward * forward + transform.right * strafe;
        rb.AddForce(force * moveForce);

        transform.Rotate(0, turn * turnSpeed * Time.fixedDeltaTime, 0);

        // --------- ⭐ REWARD SYSTEM ---------

        // Small time penalty
        AddReward(-0.0005f);

        float currentDist = Vector3.Distance(transform.position, player.position);

        // Reward getting closer
        float distDelta = (lastDistanceToPlayer - currentDist) * 0.01f;
        AddReward(distDelta);
        lastDistanceToPlayer = currentDist;

        // Vision reward
        bool visible = CheckLineOfSight();

        if (visible)
        {
            AddReward(+0.001f); // maintain LOS
            lastSeen = true;
            timeSinceSeen = 0f;
        }
        else
        {
            if (lastSeen)
                AddReward(-0.002f); // lost target

            lastSeen = false;
            timeSinceSeen += Time.deltaTime;
        }
        if (StepCount >= MaxStep)
        {
            AddReward(-0.5f); // penalty for failing to catch player in time
            EndEpisode();
        }
        if (Vector3.Distance(transform.position, player.position) < 1f)
        {
            AddReward(1.0f); // reward for catching player
            EndEpisode();
        }


    }

    // Trigger when catching the prey
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AddReward(+1.0f);  // success catch
            EndEpisode();
        }
    }
}
