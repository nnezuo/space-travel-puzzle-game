
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DiamondMesh : MonoBehaviour
{
    void Awake()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Diamond";

        int sides = 8;
        float radius = 0.6f;

        Vector3 top = new Vector3(0, 0.65f, 0);
        Vector3 bottom = new Vector3(0, -0.85f, 0);

        Vector3[] ring = new Vector3[sides];

        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            ring[i] = new Vector3(
                Mathf.Cos(angle) * radius,
                0.25f,
                Mathf.Sin(angle) * radius
            );
        }

        // Separate vertices for each triangular face
        // to give the diamond sharp, flat facets.
        Vector3[] vertices = new Vector3[sides * 6];
        int[] triangles = new int[sides * 6];

        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            int index = i * 6;

            vertices[index] = top;
            vertices[index + 1] = ring[i];
            vertices[index + 2] = ring[next];

            vertices[index + 3] = bottom;
            vertices[index + 4] = ring[next];
            vertices[index + 5] = ring[i];

            for (int j = 0; j < 6; j++)
                triangles[index + j] = index + j;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}

