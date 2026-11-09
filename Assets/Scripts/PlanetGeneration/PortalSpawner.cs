using System.Collections;
using UnityEngine;

public class PortalSpawner : MonoBehaviour
{
    public GameObject portalPrefab;
    public CubeSphereGenerator planetGenerator;
    public PlanetNoise planetNoise;

    [Tooltip("Raise (+) or lower (-) the portal if its pivot is not at its base")]
    public float heightOffset = 0f;

    IEnumerator Start()
    {
        if (portalPrefab == null || planetNoise == null || planetGenerator == null) yield break;

        yield return null;   // wait for the planet and its Mesh Collider to exist

        MeshCollider col = planetGenerator.GetComponent<MeshCollider>();
        if (col == null)
        {
            Debug.LogWarning("PortalSpawner: the planet has no Mesh Collider.", this);
            yield break;
        }

        Transform planet = planetGenerator.transform;

        // Direction of the portal, in world space (works even if the planet is moved or rotated)
        Vector3 worldDir = planet.TransformDirection(planetNoise.portalDirection.normalized);

        // Shoot a ray from far above the surface straight toward the planet centre
        float far = planetGenerator.radius + planetNoise.height + planetNoise.mountainHeight + 20f;
        Ray ray = new Ray(planet.position + worldDir * far, -worldDir);

        if (!col.Raycast(ray, out RaycastHit hit, far * 2f))
        {
            Debug.LogWarning("PortalSpawner: the ray missed the planet.", this);
            yield break;
        }

        // Stand on the real ground and tilt with its normal
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
        Vector3 pos = hit.point + hit.normal * heightOffset;

        GameObject portal = Instantiate(portalPrefab, pos, rot, planet);
        Debug.Log("Portal spawned on the surface.", portal);
    }
}