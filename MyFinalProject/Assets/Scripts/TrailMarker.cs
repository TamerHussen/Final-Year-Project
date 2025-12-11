using UnityEngine;

public class TrailMarker : MonoBehaviour
{
    public static Vector3 LastTrailPos;

    private float TrailTimer = 0f;
    public float TrailInterval = 0.5f;

    private void Update()
    {
        TrailTimer += Time.deltaTime;
        if (TrailTimer >= TrailInterval) // trail updates every interval
        {
            LastTrailPos = transform.position;
            TrailTimer = 0f;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(LastTrailPos, 0.2f);
    }
}
