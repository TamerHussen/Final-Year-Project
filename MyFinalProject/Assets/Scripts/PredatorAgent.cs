using System.Runtime.CompilerServices;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.Rendering;

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

    [Header("Familiar Settings")]
    public float timeBeforeSummon = 15f; // how long it cant see player before spawning familiars
    private float familiarCooldown = 0f;

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

        Physics.SyncTransforms();

        if (preyTrail != null)
        {
            preyTrail.ResetTrail();
        }

        lastDistanceToPlayer = Vector3.Distance(transform.position, player.position);
    }

    // ------------------------------------------------------------
    //  Collect Observations
    // ------------------------------------------------------------
    public override void CollectObservations(VectorSensor sensor)
    {
        // Distance to player = 1 obs
        float dist = Vector3.Distance(transform.position, player.position);
        sensor.AddObservation(dist / 20f);


        // Direction to player = 3 obs
        Vector3 direction = (player.position - transform.position).normalized;
        sensor.AddObservation(transform.InverseTransformDirection(direction));

        // Velocity = 3 obs
        sensor.AddObservation(rb.linearVelocity / 10f);

        // Line of sight = 1 obs
        bool visible = CheckLineOfSight();
        sensor.AddObservation(visible ? 1f : 0f);

        // time since seen = 1 obs
        sensor.AddObservation(Mathf.Clamp01(timeSinceSeen / 5f));

        // hiding pentaly = 2 obs
        sensor.AddObservation(playerMovement !=null && playerMovement.isExposed ? 1f : 0f);
        sensor.AddObservation(preyAi != null && preyAi.isExposed ? 1f : 0f);

        // Hearing = 4 obs
        Vector3 soundDir = Vector3.zero;
        soundDir = SoundEmitter.LastSoundPos - transform.position;
        if (soundDir.sqrMagnitude < 0.01f)
            sensor.AddObservation(Vector3.zero);
        else
            sensor.AddObservation(transform.InverseTransformDirection(soundDir.normalized));
        sensor.AddObservation(Mathf.Clamp01(SoundEmitter.LastSoundVolume));

        // Scent Trail points for predator = 12 obs
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
        // Scent Trail points for familiar = 4 obs
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

        // Add Raycasts = 25 obs
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

                bool isOther = !hit.collider.CompareTag("Player") && !hit.collider.CompareTag("SolidObj") && !hit.collider.CompareTag("SoftObj");
                sensor.AddObservation(isOther? 1f : 0f); // other
            }
            else
            {
                sensor.AddObservation(1f); // max distance
                sensor.AddObservation(0f);  // 0 for tags = nothing hit
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
        float forward = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f); // move forward and backwards but stops it from moonwalking
        float strafe = Mathf.Clamp(actions.ContinuousActions[1], -0.2f, 0.2f); // move left/right and limiting the strafing
        float turn = actions.ContinuousActions[2]; // rotate

        // Movement
        rb.AddForce((transform.forward * forward + transform.right * strafe) * moveForce);
        transform.Rotate(0, turn * turnSpeed * Time.fixedDeltaTime, 0f);

        // no more zooming around
        float maxSpeed = 8f;
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        // --------- REWARD SYSTEM ---------

        // Small time penalty
        AddReward(-0.0002f);

        // Reward getting closer
        float currentDist = Vector3.Distance(transform.position, player.position);
        if (currentDist < lastDistanceToPlayer)
            AddReward(0.001f); // moving closer
        if (currentDist > lastDistanceToPlayer)
            AddReward(-0.001f); // moving further

        lastDistanceToPlayer = currentDist;

        // Vision reward
        if (CheckLineOfSight())
        {
            AddReward(0.005f); // maintain LOS
            timeSinceSeen = 0f;
        }
        else
        {
            timeSinceSeen += Time.deltaTime;
            
            if (timeSinceSeen > timeBeforeSummon && Time.time > familiarCooldown)
            {
                if (preyTrail == null || preyTrail.MainTrail.Count == 0)
                {
                    SummonFamiliar();
                    familiarCooldown = Time.time + 30f; // no spamming
                }
            }
        }

        if (StepCount >= MaxStep)
        {
            AddReward(-0.1f); // penalty for failing to catch player in time
            EndEpisode();
        }

        // cloud trail reward
        if (!CheckLineOfSight() && preyTrail != null && preyTrail.MainTrail.Count > 0)
        {
            float closestDist = float.MaxValue;
            foreach (Vector3 point in preyTrail.MainTrail)
            {
                float d = Vector3.Distance(transform.position, point);
                if (d < closestDist)
                {
                    closestDist = d;
                }
            }

            float scentRadius = 6f;

            if(closestDist <  scentRadius)
            {
                float scentStrength = 1f - (closestDist / scentRadius);

                AddReward(0.002f * scentStrength); // gives reward for stay in and getting closer to the scent
            }

            Vector3 last = preyTrail.MainTrail[preyTrail.MainTrail.Count - 1];
            float dist = Vector3.Distance(transform.position, last);

            if (dist < lastDistanceToScent)
                AddReward(0.002f);

            lastDistanceToScent = dist;
        }

        ApplyProximityPenalty();

    }

    private void SummonFamiliar()
    {
        Debug.Log("Predator lost scent. Summon Familiar/s to track the permanent trail.. ");
            // going to add a prefab for the familiar here after making the familiar and its script
    }

    public override void Heuristic(in ActionBuffers actionsOut) // add model asset if starting without training to avoid error/
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[2] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Mouse X");
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

    // pentality for hugging the obstacles like the walls or solid objects.
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("SolidObj") || collision.gameObject.CompareTag("Walls"))
        {
            AddReward(-0.002f);
        }
    }

    // the closer the ml agent is to the solid objects and walls the higher the penalty
    private void ApplyProximityPenalty()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 2.5f, visionMask))
        {
            if (hit.collider.CompareTag("SolidObj") || hit.collider.CompareTag("Walls"))
            {
                float closeness = 1f - (hit.distance / 2.5f);
                AddReward(-0.005f * closeness);
            }
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
                Gizmos.color = Color.red; // shows that an obstacle or the player is in the line of sight
                Gizmos.DrawLine(transform.position, hit.point);
                Gizmos.DrawSphere(hit.point, 0.2f);
            }
            else
            {
                Gizmos.color = Color.yellow; // shows the line of sight
                Gizmos.DrawRay(transform.position, dir * rayDistance);
            }
        }

        // hearing radius
        Gizmos.color = new Color(0f, 0f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, 12f);

        // cloud trail line
        if (preyTrail != null && preyTrail.MainTrail.Count > 0)
        {
            Gizmos.color = Color.green;

            float closestDist = float.MaxValue;
            Vector3 closestPoint = transform.position;
            foreach (Vector3 point in preyTrail.MainTrail)
            {
                float d = Vector3.Distance(transform.position, point);
                if (d < closestDist)
                {
                    closestDist = d;
                    closestPoint = point;
                }
            }

            Gizmos.DrawLine(transform.position, closestPoint);

            Gizmos.color = new Color(0f, 1f, 0f, 0.2f); // transparent radius around main trail points
            Gizmos.DrawWireSphere(closestPoint, 6f);
        }
    }
}
