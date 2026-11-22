using UnityEngine;

/*
 * RiverWaterSpawner (class name: WaterBodySpawner)
 * ------------------------------------------------
 * Puts some small flat water planes (lakes) in the low places of the planet.
 * Put it on the planet object, with the CubeSphereGenerator and PlanetNoise.
 *
 * How: we try "waterCount" random directions. For each one we ask PlanetNoise
 * the height of the ground there. If it is low (< 0.2) we create a water
 * prefab a bit under the ground level, turned to follow the planet surface.
 *
 * MISTAKES / things we didn't have time to fix:
 *  - The file is called RiverWaterSpawner.cs but the class is
 *    WaterBodySpawner. Unity needs the SAME name for a MonoBehaviour, so this
 *    script can't be added to an object in the Inspector. We should rename
 *    the file or the class.
 *  - We try waterCount directions, but we only spawn water if the place is
 *    low enough. So we usually get LESS lakes than waterCount (maybe 0).
 *  - Random.onUnitSphere is not seeded, so the lakes are at a different place
 *    each time we play, while the planet itself is always the same.
 *  - The real rivers are made in RiverWater.cs, this script only makes
 *    small lakes. The name is a bit confusing.
 *  - 0.2 and the scale 3 are written by hand.
 */
public class WaterBodySpawner : MonoBehaviour
{
    public GameObject waterPrefab;
    public CubeSphereGenerator generator;
    public PlanetNoise noise;
    public int waterCount = 6;

    void Start()
    {
        if (waterPrefab == null || noise == null || generator == null) return;

        // Spawn flat water bodies at calculated low elevations (lakes/rivers)
        for (int i = 0; i < waterCount; i++)
        {
            Vector3 randomDir = Random.onUnitSphere;
            float height = noise.GetHeight(randomDir);

            // Only spawn water in depressions (height below zero or near rivers)
            if (height < 0.2f)
            {
                // position = centre + direction * (radius + ground height - a bit)
                Vector3 spawnPos = transform.position + randomDir * (generator.radius + height - 0.2f);
                GameObject water = Instantiate(waterPrefab, spawnPos, Quaternion.identity);
                water.transform.SetParent(transform);
                // the "up" of the water plane points away from the planet centre
                water.transform.rotation = Quaternion.FromToRotation(Vector3.up, randomDir);
                water.transform.localScale = Vector3.one * 3f; // Scale lake size
            }
        }
    }
}