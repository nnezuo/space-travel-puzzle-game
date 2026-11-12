
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalSpawner : MonoBehaviour
{
    public GameObject crystalPrefab;
    public GameObject enemyPrefab;

    public int crystalCount = 3;
    public int samples = 3000;
    public float minSeparation = 8f;
    public float hoverHeight = 1f;

    public float enemyDistance = 4f;
    public float enemySurfaceOffset = 1f;

    struct Spot
    {
        public Vector3 point, normal;
        public float altitude;
    }

    IEnumerator Start()
    {
        yield return null;

        var gen = GetComponent<CubeSphereGenerator>();
        var col = GetComponent<MeshCollider>();
        var noise = GetComponent<PlanetNoise>();

        if (gen == null || col == null || crystalPrefab == null)
        {
            Debug.LogWarning("Crystal spawner is missing a required component.");
            yield break;
        }

        Random.InitState(noise != null ? noise.seed : 0);

        float top = gen.radius +
            (noise ? noise.height + noise.mountainHeight : 0f) + 5f;

        var spots = new List<Spot>();

        for (int i = 0; i < samples; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            Ray ray = new Ray(transform.position + dir * top, -dir);

            if (!col.Raycast(ray, out RaycastHit hit, top * 2f))
                continue;
            Vector3 outward =
               (hit.point - transform.position).normalized;


            if (Vector3.Angle(hit.normal, outward) > 20f)
                continue;

            spots.Add(new Spot
            {
                point = hit.point,
                normal = hit.normal,
                altitude = (hit.point - transform.position).magnitude
            });
        }

        float targetAltitude = gen.radius + 1f;

        spots.Sort((a, b) =>
            Mathf.Abs(a.altitude - targetAltitude)
            .CompareTo(Mathf.Abs(b.altitude - targetAltitude))
        );

        var placed = new List<Vector3>();

        foreach (var s in spots)
        {
            if (placed.Count >= crystalCount)
                break;

            bool tooClose = false;

            foreach (var p in placed)
            {
                if (Vector3.Distance(p, s.point) < minSeparation)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            Vector3 crystalPos = s.point + s.normal * hoverHeight;
            Quaternion crystalRot =
                Quaternion.FromToRotation(Vector3.up, s.normal);

            GameObject crystalObject =
                Instantiate(crystalPrefab, crystalPos, crystalRot, transform);

            // Find a nearby position on the planet for the enemy.
            if (enemyPrefab != null)
            {
                Vector3 tangent = Vector3.ProjectOnPlane(
                    Random.onUnitSphere, s.normal).normalized;

                if (tangent.sqrMagnitude < 0.001f)
                    tangent = Vector3.Cross(s.normal, Vector3.right).normalized;

                Vector3 target = s.point + tangent * enemyDistance;

                Vector3 outward =
                    (target - transform.position).normalized;

                Vector3 rayStart =
                    transform.position + outward * top;

                Ray enemyRay = new Ray(rayStart, -outward);

                if (col.Raycast(enemyRay, out RaycastHit enemyHit,
                                top * 2f))
                {
                    Vector3 enemyOutward =
                        (enemyHit.point - transform.position).normalized;

                    if (Vector3.Angle(enemyHit.normal, enemyOutward) <= 20f)
                    {
                        Vector3 enemyPos =
                            enemyHit.point +
                            enemyHit.normal * enemySurfaceOffset;

                        Quaternion enemyRot =
                            Quaternion.FromToRotation(
                                Vector3.up, enemyHit.normal);

                        GameObject spawnedEnemy =
                
                        Instantiate(enemyPrefab, enemyPos, enemyRot, transform);

                        Crystal crystalScript =
                            crystalObject.GetComponent<Crystal>();

                        if (crystalScript != null)
                        {
                         crystalScript.guardingEnemy =
                                spawnedEnemy.GetComponent<PlanetEnemy>();
                        }
                    }
                }
            }

            placed.Add(s.point);
        }

        Debug.Log("Crystals spawned: " + placed.Count);
    }
}
