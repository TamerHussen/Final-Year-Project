using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public class PredatorAgent : Agent
{
    [Header("References")]
    public Transform player;
    public Rigidbody rb;
    public TrailMarker preyTrail;
    public PlayerMovement playerMovement;
    public PreyAi preyAi;

    [Header("Movement Settings")]
    public float moveForce = 50f;
    public float turnSpeed = 180f;

    [Header("Vision Settings")]
    public float rayDistance = 15f;
    public LayerMask visionMask; // walls, player, obstacles

    private float lastDistanceToPlayer;
    private float lastDistanceToScent = Mathf.Infinity;
    private float timeSinceSeen = 0f;

    // used for debug UI
    public float TimeSinceSeen => timeSinceSeen;
    public float LastDistanceToScent => lastDistanceToScent;
    public float LastDistanceToPlayer => lastDistanceToPlayer;

    public override void OnEpisodeBegin()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        timeSinceSeen = 0;
        lastDistanceToScent = Mathf.Infinity;

        // randomises respawn for better ml agent learning
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
        // Distance to player
        float dist = Vector3.Distance(transform.position, player.position);
        sensor.AddObservation(dist / 20f);


        // Direction to player
        Vector3 direction = (player.position - transform.position).normalized;
        sensor.AddObservation(transform.InverseTransformDirection(direction));

        // Velocity
        sensor.AddObservation(rb.linearVelocity / 10f);

        // Line of sight
        bool visible = CheckLineOfSight();
        sensor.AddObservation(visible ? 1f : 0f);

        // time since seen
        sensor.AddObservation(Mathf.Clamp01(timeSinceSeen / 5f));

        sensor.AddObservation(playerMovement.isExposed ? 1f : 0f);
        sensor.AddObservation(preyAi.isExposed ? 1f : 0f);

        // Hearing
        Vector3 soundDir = Vector3.zero;
        soundDir = SoundEmitter.LastSoundPos - transform.position;
        if (soundDir.sqrMagnitude < 0.01f)
            sensor.AddObservation(Vector3.zero);
        else
            sensor.AddObservation(transform.InverseTransformDirection(soundDir.normalized));
        sensor.AddObservation(Mathf.Clamp01(SoundEmitter.LastSoundVolume));

        // Scent Trail points for predator
        if (preyTrail != null && preyTrail.MainTrail != null)
        {
            int count = preyTrail.MainTrail.Count;

            for (int i = 0; i < 3; i++)
            {
                int index = count - 1 - i;
                if (index < 0)
                {
                    sensor.AddObservation(Vector3.zero);
                    sensor.AddObservation(0f);
                }
                else
                {
                    Vector3 scentDir = preyTrail.MainTrail[index] - transform.position;
                    sensor.AddObservation(transform.InverseTransformDirection(scentDir.normalized));
                    sensor.AddObservation(Mathf.Clamp01(scentDir.magnitude / rayDistance));
                }

            }
        }
        else
        {
            // fallback
            for (int i = 0; i < 3; i++)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(0f);
            }
        }
        // Scent Trail points for familiar
        if (preyTrail != null && preyTrail.FamiliarTrail != null && preyTrail.FamiliarTrail.Count > 0)
        {
            Vector3 familiarPoint = preyTrail.FamiliarTrail[preyTrail.FamiliarTrail.Count - 1];

            Vector3 dir = familiarPoint - transform.position;

            sensor.AddObservation(transform.InverseTransformDirection(dir.normalized));
            sensor.AddObservation(Mathf.Clamp01(dir.magnitude / rayDistance));
        }
        else
        {
           sensor.AddObservation(Vector3.zero);
           sensor.AddObservation(0f);
        }

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
                sensor.AddObservation(hit.distance / rayDistance); // 1 obs

                // Hit types
                sensor.AddObservation(hit.collider.CompareTag("Player") ? 1f : 0f);
                sensor.AddObservation(hit.collider.CompareTag("SolidObj") ? 1f : 0f);
                sensor.AddObservation(hit.collider.CompareTag("SoftObj") ? 1f : 0f);
                sensor.AddObservation(!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("SolidObj") && !hit.collider.CompareTag("SoftObj") ? 1f : 0f); // other
            }
            else
            {
                sensor.AddObservation(1f); // max distance
                sensor.AddObservation(0f);  // nothing hit
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }
    }

    // used for Debug ui
    public bool HasLineOfSight()
    {
        return CheckLineOfSight();
    }

    bool CheckLineOfSight()
    {
        Vector3 dirToPlayer = player.position - transform.position;
        if (dirToPlayer.sqrMagnitude < 0.00001f) return false;
        Vector3 dirNorm = dirToPlayer.normalized;
        if (Physics.Raycast(transform.position, dirNorm, out RaycastHit hit, rayDistance, visionMask))
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
        rb.AddForce((transform.forward * forward + transform.right * strafe) * moveForce);
        transform.Rotate(0, turn * turnSpeed * Time.fixedDeltaTime, 0f);

        // --------- REWARD SYSTEM ---------

        // Small time penalty
        AddReward(-0.0002f);

        // Reward getting closer
        float currentDist = Vector3.Distance(transform.position, player.position);
        if (currentDist < lastDistanceToPlayer)
            AddReward(0.001f);
        if (currentDist > lastDistanceToPlayer)
            AddReward(-0.001f);

        lastDistanceToPlayer = currentDist;

        // reward for facing prey
        Vector3 localDir = transform.InverseTransformDirection((player.position - transform.position).normalized);
        AddReward((1f - Mathf.Abs(localDir.x)) * 0.002f);

        // Vision reward
        if (CheckLineOfSight())
        {
            AddReward(0.005f); // maintain LOS
            timeSinceSeen = 0f;
        }
        else
        {
            AddReward(-0.001f); // lost target
            timeSinceSeen += Time.deltaTime;
        }

        if (StepCount >= MaxStep)
        {
            AddReward(-0.1f); // penalty for failing to catch player in time
            EndEpisode();
        }

        // trail reward
        if (!CheckLineOfSight() && preyTrail != null && preyTrail.MainTrail.Count > 0)
        {
            Vector3 last = preyTrail.MainTrail[preyTrail.MainTrail.Count - 1];
            float dist = Vector3.Distance(transform.position, last);

            if (dist < lastDistanceToScent)
                AddReward(0.002f);

            lastDistanceToScent = dist;
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
            AddReward(1.0f);  // success catch
            EndEpisode();
        }
    }

    private void DrawVisionCone()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f); // transparent colour

        float fov = 60f;
        float halfFov = fov * 0.5f;
        int segments = 16;

        Vector3 origin = transform.position;

        // draws the circle at max distance
        for (int i = -segments / 2; i < segments / 2; i++)
        {
            float angleA = halfFov * (i / (float)segments);
            float angleB = halfFov * ((i + 1) / (float)segments);

            Vector3 dirA = Quaternion.Euler(0, angleA, 0) * transform.forward;
            Vector3 dirB = Quaternion.Euler(0, angleB, 0) * transform.forward;

            Vector3 pointA = origin + dirA * rayDistance;
            Vector3 pointB = origin + dirB * rayDistance;

            Gizmos.DrawLine(origin, pointA);
            Gizmos.DrawLine(pointA, pointB);

        }
    }

    private void OnDrawGizmos()
    {
        if (player == null) return;

        // vision cone
        DrawVisionCone();

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
        if (preyTrail != null && preyTrail.MainTrail.Count > 0)
            Gizmos.DrawLine(transform.position, preyTrail.MainTrail[preyTrail.MainTrail.Count - 1]);
    }
}
