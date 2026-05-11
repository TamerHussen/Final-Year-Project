using UnityEngine;

public class ObstacleGizmos : MonoBehaviour
{
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);
    public Color predatorPenaltyColor = new Color(1f, 0.5f, 0f, 0.4f);
    public Color preyAvoidanceColor = new Color(1f, 1f, 0f, 0.4f);

    public bool showAlways = true;
    public bool showAITriggerZones = true;

    private float preyAvoidanceDistance = 2.0f;
    private PredatorAgent agent;

    private void Start()
    {
        agent = FindFirstObjectByType<PredatorAgent>();
    }

    private void OnDrawGizmos()
    {
        if (showAlways) DrawColliderGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showAlways) DrawColliderGizmo();
    }

    private void DrawColliderGizmo()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        float predDistance = 2.5f;
        if (Application.isPlaying && agent != null)
        {
            predDistance = agent.penaltyDistance;
        }

        if (col is BoxCollider box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);

            if (showAITriggerZones)
            {
                Gizmos.color = predatorPenaltyColor;
                Gizmos.DrawWireCube(box.center, box.size + Vector3.one * (predDistance * 2));

                Gizmos.color = preyAvoidanceColor;
                Gizmos.DrawWireCube(box.center, box.size + Vector3.one * (preyAvoidanceDistance * 2));
            }
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);

            if (showAITriggerZones)
            {
                Gizmos.color = predatorPenaltyColor;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius + predDistance);

                Gizmos.color = preyAvoidanceColor;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius + preyAvoidanceDistance);
            }
        }
        else if (col is MeshCollider meshCollider && meshCollider.sharedMesh != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = gizmoColor;
            Gizmos.DrawMesh(meshCollider.sharedMesh);
            Gizmos.DrawWireMesh(meshCollider.sharedMesh);

            if (showAITriggerZones)
            {
                Gizmos.matrix = Matrix4x4.identity;

                Gizmos.color = predatorPenaltyColor;
                Gizmos.DrawWireCube(meshCollider.bounds.center, meshCollider.bounds.size + Vector3.one * (predDistance * 2));

                Gizmos.color = preyAvoidanceColor;
                Gizmos.DrawWireCube(meshCollider.bounds.center, meshCollider.bounds.size + Vector3.one * (preyAvoidanceDistance * 2));
            }

        }

        // reset matrix to stop affecting other gizmos
        Gizmos.matrix = Matrix4x4.identity;
    }
}
