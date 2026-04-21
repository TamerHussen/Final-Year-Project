using UnityEngine;
using System.Collections.Generic;

public class MapRandomiser : MonoBehaviour
{
    public static MapRandomiser instance;

    [Header("Walls Bounds (Range)")]
    public List<GameObject> OuterWalls;
    public LayerMask ObstacleMask;

    [Header("Terrain Layer")]
    public LayerMask terrainLayer;

    [Header("Randomising Objects")]
    public List<Transform> SolidObjects;
    public List<Transform> SoftObjects;
    public List<Transform> DistractionAnimals;

    [Header("For ML Training")]
    public Transform Predator;
    public Transform Prey;

    [Header("Randomisation Settings")]
    public float ValidPlacementSphereRadius = 1.5f;
    public float wallPadding = 17.0f;
    public float minStartingDistance = 10f; // prevent instant catches during training

    [Header("Scale Randomisation For SoftObj")]
    public float minSoftScale = 2f;
    public float maxSoftScale = 7f;

    [Header("Scale Randomisation For SolidObj")]
    public float minSolidScale = 2f;
    public float maxSolidScale = 7f;

    private float minX, maxX, minZ, maxZ;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        RecalculateBounds();
    }

    [ContextMenu("Recalculate Boundaries")]
    public void RecalculateBounds()
    {
        if (OuterWalls == null || OuterWalls.Count == 0) return;

        Bounds arenaBoounds = new Bounds(transform.position, Vector3.zero);
        foreach (var wall in OuterWalls)
        {
            Collider col = wall.GetComponent<Collider>();
            if (col != null) arenaBoounds.Encapsulate(col.bounds);
        }

        minX = arenaBoounds.min.x + wallPadding;
        maxX = arenaBoounds.max.x - wallPadding;
        minZ = arenaBoounds.min.z + wallPadding;
        maxZ = arenaBoounds.max.z - wallPadding;
    }

    public void Randomise(PredatorAgent predator)
    {
        if (OuterWalls == null || OuterWalls.Count == 0) return;

        RecalculateBounds();

        // randomise obj locations
        RandomiseObjectLocations(SolidObjects, true, minSolidScale, maxSolidScale);
        RandomiseObjectLocations(SoftObjects, true, minSoftScale, maxSoftScale);
        RandomiseAnimals();

        // headstart mechanic
        Vector3 centerArea = new Vector3((minX + maxX) / 2f, 0, (minZ + maxZ) / 2f);
        // place prey near center
        Prey.position = GetValidSpawnLocationNear(centerArea, 5f);

        // place predator with minimum distance limit
        Vector3 predatorSpawn = Vector3.zero;
        int attempts = 0;
        do
        {
            predatorSpawn = GetValidSpawnLocationNear(centerArea, 15f);
            attempts++;
        }
        while (Vector3.Distance(predatorSpawn, Prey.position) < minStartingDistance && attempts < 30);

        predator.transform.position = predatorSpawn;
        predator.transform.LookAt(new Vector3(Prey.position.x, predator.transform.position.y, Prey.position.z));

        Physics.SyncTransforms();
    }

    public void RandomiseForGameplay(Transform playerTransform, Transform predatorTransform)
    {
        if (OuterWalls == null || OuterWalls.Count == 0) return;
        RecalculateBounds();

        // randomise obj locations
        RandomiseObjectLocations(SolidObjects, true, minSolidScale, maxSolidScale);
        RandomiseObjectLocations(SoftObjects, true, minSoftScale, maxSoftScale);
        RandomiseAnimals();

        // headstart mechanic
        Vector3 center = new Vector3((minX + maxX) / 2f, 0, (minZ + maxZ) / 2f);
        // place prey near center
        Vector3 playerSpawn = GetValidSpawnLocationNear(center, 8f);
        playerTransform.position = playerSpawn;

        // place predator with minimum distance limit
        Vector3 predSpawn = Vector3.zero;

        int attempts = 0;
        do
        {
            predSpawn = GetValidSpawnLocation(true);
            attempts++;
        }
        while (Vector3.Distance(predSpawn, playerSpawn) < minStartingDistance && attempts < 30);

        predatorTransform.position = predSpawn;
        predatorTransform.LookAt(new Vector3(playerSpawn.x, predatorTransform.position.y, playerSpawn.z));

        Physics.SyncTransforms();
    }

    public Vector3 GetTerrainPoint()
    {
        int tries = 0;
        LayerMask mask = terrainLayer != 0 ? terrainLayer : ~0;

        while (tries < 50)
        {
            tries++;
            Vector3 candidate = new Vector3(Random.Range(minX, maxX), 200f, Random.Range(minZ, maxZ));

            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 300f, mask))
            {
                if (hit.normal.y > 0.85f)
                    return hit.point + Vector3.up * 0.5f;
            }
        }

        Vector3 fallback = new Vector3((minX + maxX) / 2f, 200f, (minZ + maxZ) / 2f);
        if (Physics.Raycast(fallback, Vector3.down, out RaycastHit fbHit, 300f))
            return fbHit.point + Vector3.up * 0.5f;

        return center_flat();
    }

    Vector3 center_flat() => new Vector3((minX + maxX) / 2f, 1f, (minZ + maxZ) / 2f);

    void RandomiseObjectLocations(List<Transform> objects, bool checkOverlap, float minSize, float maxSize)
    {
        foreach (Transform obj in objects)
        {
            obj.position = GetValidSpawnLocation(checkOverlap);
            obj.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // random scale
            float randScale = Random.Range(minSize, maxSize);
            obj.localScale = new Vector3(randScale, randScale, randScale);
        }
    }

    void RandomiseAnimals()
    {
        if (DistractionAnimals == null || DistractionAnimals.Count == 0) return;

        foreach (Transform animal in DistractionAnimals)
        {
            animal.position = GetValidSpawnLocation(true);
            animal.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            DistractionAnimal animalScript = animal.GetComponent<DistractionAnimal>();
            if (animalScript != null)
            {
                animalScript.ResetAnimal();
            }
        }
    }

    public Vector3 GetRandomValidPoint()
    {
        return GetValidSpawnLocation(false);
    }

    public Vector3 GetValidSpawnLocation(bool checkOverlap)
    {
        Vector3 randomPoint = Vector3.zero;
        bool valid = false;
        float tries = 0;

        while (!valid && tries < 50)
        {
            randomPoint = new Vector3(Random.Range(minX, maxX), 100f, Random.Range(minZ, maxZ));
            tries++;

            if (Physics.Raycast(randomPoint, Vector3.down, out RaycastHit terrainHit, 200f))
            {
                randomPoint = terrainHit.point + (Vector3.up * 1.5f);
                valid = true;
            }

            // prevent spawning ontop of eachother
            if (checkOverlap && Physics.CheckSphere(randomPoint, ValidPlacementSphereRadius, ObstacleMask))
            {
                valid = false;
            }
        }
        return randomPoint;
    }

    Vector3 GetValidSpawnLocationNear(Vector3 center, float radius)
    {
        int tries = 0;
        while (tries < 30)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(0f, radius);

            Vector3 candidate = new Vector3(center.x + Mathf.Cos(angle) * dist, 100f, center.z + Mathf.Sin(angle) * dist);

            tries++;

            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit terrainHit, 200f))
            {
                Vector3 spawnPos = terrainHit.point + Vector3.up * 1.5f;
                // checks for any obstacles already there
                if (!Physics.CheckSphere(spawnPos, ValidPlacementSphereRadius, ObstacleMask)) return spawnPos;
            }
        }
        // fallback
        return center + Vector3.up * 1.5f;
    }

    void OnDrawGizmosSelected()
    {
        RecalculateBounds();

        // visualise bounds
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Vector3 center = new Vector3((minX + maxX) / 2f, 5f, (minZ + maxZ) / 2f);
        Vector3 size = new Vector3(maxX - minX, 10f, maxZ - minZ);
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}
