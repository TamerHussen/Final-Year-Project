using UnityEngine;

public class ObstacleGizmos : MonoBehaviour
{
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);

    public Color predatorPenaltyColor = new Color(1f, 0.5f, 0f, 0.4f);
    public Color preyAvoidanceColor = new Color(1f, 1f, 0f, 0.4f);

    public bool showAlways = true;
    public bool showAITriggerZones = true;

    private float predatorPenaltyDistance = 2.5f;
    private float preyAvoidanceDistance = 3.0f;

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

        if(col != null)
        {
            if(col is BoxCollider box)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);

                if (showAITriggerZones)
                {
                    Gizmos.color = predatorPenaltyColor;
                    Gizmos.DrawWireCube(box.center, box.size + (Vector3.one * (predatorPenaltyDistance * 2)));

                    Gizmos.color = preyAvoidanceColor;
                    Gizmos.DrawWireCube(box.center, box.size + (Vector3.one * (preyAvoidanceDistance * 2)));
                }
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);

                if (showAITriggerZones)
                {
                    Gizmos.color = predatorPenaltyColor;
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius + predatorPenaltyDistance);

                    Gizmos.color = preyAvoidanceColor;
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius + preyAvoidanceDistance);
                }
            }
        }
    }
}
