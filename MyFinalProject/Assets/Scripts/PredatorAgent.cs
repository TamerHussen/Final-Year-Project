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
    public float moveForce = 50f;
    public float turnSpeed = 180f;

    [Header("Vision Settings")]
    public float rayDistance = 15f;
    public LayerMask visionMask; // walls, player, obstacles

    private float lastDistanceToPlayer;
    private bool lastSeen = false;
    private float timeSinceSeen = 0f;

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        lastSeen = false;
        timeSinceSeen = 0;


        // randomises respawn for better ml agen learning
        Vector3 PredatorPos = new Vector3(Random.Range(-8f, 8f), 1.5f, Random.Range(-8f, 8f));
        Vector3 PlayerPos = new Vector3(Random.Range(-8f, 8f), 1.5f, Random.Range(-8f, 8f));

        transform.position = PredatorPos;
        player.position = PlayerPos;

        lastDistanceToPlayer = Vector3.Distance(transform.position, player.position);
    }

    // ------------------------------------------------------------
    //  Collect Observations
    // ------------------------------------------------------------
    public override void CollectObservations(VectorSensor sensor)
    {
        if (player == null || rb == null)
        {
            for (int i = 0; i < 19; i++)
                sensor.AddObservation(0f);
            return;
        }

        // Distance + direction to player
        float dist = Vector3.Distance(transform.position, player.position);
        Vector3 direction = (player.position - transform.position).normalized;

        sensor.AddObservation(dist);
        sensor.AddObservation(direction);

        // turning behaviour
        Vector3 localDir = transform.InverseTransformDirection(direction);
        sensor.AddObservation(localDir);

        // Speed Observer
        sensor.AddObservation(player.GetComponent<CharacterController>().velocity.magnitude);

        // Agent movement
        sensor.AddObservation(rb.linearVelocity);

        // Field of view angle
        float angle = Vector3.Angle(transform.forward, (player.position - transform.position));
        sensor.AddObservation(angle / 100f);

        // Line of sight
        bool visible = CheckLineOfSight();
        sensor.AddObservation(visible ? 1f : 0f);
        sensor.AddObservation(timeSinceSeen);

        // Hearing
        Vector3 soundDir = SoundEmitter.LastSoundPos - transform.position;
        sensor.AddObservation(transform.InverseTransformDirection(soundDir.normalized));
        sensor.AddObservation(SoundEmitter.LastSoundVolume);

        // Scent Trail
        Vector3 scentDir = TrailMarker.LastTrailPos - transform.position;
        sensor.AddObservation(transform.InverseTransformDirection(scentDir.normalized));
        sensor.AddObservation(Vector3.Distance(transform.position, TrailMarker.LastTrailPos) / 20f);

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
                    sensor.AddObservation(2f); // player
                else
                    sensor.AddObservation(1f); // wall/obstacle
            }
            else
            {
                sensor.AddObservation(1f); // no hit
                sensor.AddObservation(0f);  // nothing
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

        // Movement
        Vector3 force = transform.forward * forward + transform.right * strafe;
        rb.AddForce(force * moveForce);

        transform.Rotate(0, turn * turnSpeed * Time.fixedDeltaTime, 0);

        // --------- ⭐ REWARD SYSTEM ---------

        // Small time penalty
        AddReward(-0.0002f);

        float currentDist = Vector3.Distance(transform.position, player.position);

        // Reward getting closer
        if (currentDist < lastDistanceToPlayer)
            AddReward((lastDistanceToPlayer - currentDist) * 0.1f);

        lastDistanceToPlayer = currentDist;

        // Vision reward
        bool visible = CheckLineOfSight();

        if (visible)
        {
            AddReward(0.005f); // maintain LOS
            lastSeen = true;
            timeSinceSeen = 0f;
        }
        else
        {
            if (lastSeen)
                AddReward(-0.02f * Mathf.Min(timeSinceSeen, 3f)); // lost target

            lastSeen = false;
            timeSinceSeen += Time.deltaTime;
        }
        if (StepCount >= MaxStep)
        {
            AddReward(-0.1f); // penalty for failing to catch player in time
            EndEpisode();
        }
        if (Vector3.Distance(transform.position, player.position) < 1f)
        {
            AddReward(1.0f); // reward for catching player
            EndEpisode();
        }


    }

    public override void Heuristic(in ActionBuffers actionsOut) // add model asset if starting without training to avoid error/
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = 0f;
        continuousActionsOut[1] = 0f;
        continuousActionsOut[2] = 0f;
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

    private void OnDrawGizmos()
    {
        if (player == null) return;

        // vision cone
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f); // transparent colour
        Gizmos.DrawFrustum(transform.position, 60f, rayDistance, 0.1f, 1f);

        // raycast direction
        Gizmos.color = Color.yellow;
        Vector3[] rays =
        {
            transform.forward,
            Quaternion.Euler(0,-25,0) * transform.forward,
            Quaternion.Euler(0,25,0) * transform.forward,
            Quaternion.Euler(0,-50,0) * transform.forward,
            Quaternion.Euler(0,50,0) * transform.forward

        };

        foreach (var dir in rays)
            Gizmos.DrawRay(transform.position, dir * rayDistance);

        // hearing radius
        Gizmos.color = new Color(0f, 0f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, 12f);

        // trail line
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, TrailMarker.LastTrailPos);
    }
}
