using UnityEngine;
using System.Collections.Generic;

public class TrailMarker : MonoBehaviour
{
    public List<Vector3> TrailPoints = new List<Vector3>();
    public int MaxTrailPoints = 20;
    public float TrailInterval = 0.3f;

    private float TrailTimer;

    private void Update()
    {
        TrailTimer += Time.deltaTime;
        if (TrailTimer >= TrailInterval) // trail updates every interval
        {
            TrailPoints.Add(transform.position);
            if (TrailPoints.Count > MaxTrailPoints)
                TrailPoints.RemoveAt(0); // removes oldest point
            TrailTimer = 0f;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        // makes the points
        for (int i = 0; i < TrailPoints.Count; i++)
            Gizmos.DrawSphere(TrailPoints[i], 0.15f);

        // connects points
        for (int i = 0; i < TrailPoints.Count - 1; i++)
            Gizmos.DrawLine(TrailPoints[i], TrailPoints[i + 1]);
    }
}
