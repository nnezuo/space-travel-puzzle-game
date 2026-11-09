using UnityEngine;

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
                Vector3 spawnPos = transform.position + randomDir * (generator.radius + height - 0.2f);
                GameObject water = Instantiate(waterPrefab, spawnPos, Quaternion.identity);
                water.transform.SetParent(transform);
                water.transform.rotation = Quaternion.FromToRotation(Vector3.up, randomDir);
                water.transform.localScale = Vector3.one * 3f; // Scale lake size
            }
        }
    }
}