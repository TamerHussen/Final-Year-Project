using UnityEngine;
using System.Collections.Generic;

public class TrailMarker : MonoBehaviour
{
    public List<Vector3> MainTrail = new List<Vector3>(); // short duration
    public List<Vector3> FamiliarTrail = new List<Vector3>(); // long/ permanent duration

    public int MaxMainTrailPoints = 20;
    public int MaxFamiliarTrailPoints = 1000; // added cap so it doesnt lag 
    public float TrailInterval = 0.3f;

    private float TrailTimer;

    private void Update()
    {
        TrailTimer += Time.deltaTime;
        if (TrailTimer >= TrailInterval) // trail updates every interval
        {
            Vector3 pos = transform.position;

            MainTrail.Add(pos);
            FamiliarTrail.Add(pos);
            if (MainTrail.Count > MaxMainTrailPoints)
                MainTrail.RemoveAt(0); // removes oldest point

            if (FamiliarTrail.Count > 0 && FamiliarTrail.Count > MaxFamiliarTrailPoints)
                FamiliarTrail.RemoveAt(0); // removes oldest point slowly

            TrailTimer = 0f;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        // makes the points
        for (int i = 0; i < MainTrail.Count; i++)
            Gizmos.DrawSphere(MainTrail[i], 0.15f);

        // connects points
        for (int i = 0; i < MainTrail.Count - 1; i++)
            Gizmos.DrawLine(MainTrail[i], MainTrail[i + 1]);


        Gizmos.color = Color.blue;

        for (int i = 0; i < FamiliarTrail.Count - 1; i++)
            Gizmos.DrawLine(FamiliarTrail[i], FamiliarTrail[i + 1]);
    }
}
