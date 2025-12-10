using UnityEngine;

public class TrailMarker : MonoBehaviour
{
    public static Vector3 LastTrailPos;

    private void Update()
    {
        if (Random.value < 0.05f) // trail every 20 frames
            LastTrailPos = transform.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(LastTrailPos, 0.2f);
    }
}
