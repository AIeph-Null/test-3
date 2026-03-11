using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SeabedMeshGenerator : MonoBehaviour
{
    [Header("Grid Size (world units)")]
    public float width = 2000f;
    public float length = 2000f;

    [Header("Grid Resolution")]
    public int xSegments = 200;
    public int zSegments = 200;

    [Header("Depth Shaping")]
    public float maxDepth = 300f;              // meters-ish
    public AnimationCurve depthCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("World-space shoreline reference point (temporary). Replace later with a shoreline distance field.")]
    public Transform referencePoint;

    [Tooltip("How far from referencePoint until max depth is reached.")]
    public float maxDepthDistance = 600f;

    [Header("Noise (optional)")]
    public float noiseScale = 0.005f;
    public float noiseStrength = 10f;

    Mesh mesh;

    void OnValidate()
    {
        if (xSegments < 2) xSegments = 2;
        if (zSegments < 2) zSegments = 2;
        Generate();
    }

    void Start() => Generate();

    public void Generate()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Procedural Seabed";
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
        else
        {
            mesh.Clear();
        }

        var verts = new Vector3[(xSegments + 1) * (zSegments + 1)];
        var uvs = new Vector2[verts.Length];
        var tris = new int[xSegments * zSegments * 6];

        float xStep = width / xSegments;
        float zStep = length / zSegments;

        Vector3 origin = transform.position - new Vector3(width * 0.5f, 0f, length * 0.5f);

        int vi = 0;
        for (int z = 0; z <= zSegments; z++)
        {
            for (int x = 0; x <= xSegments; x++)
            {
                float wx = origin.x + x * xStep;
                float wz = origin.z + z * zStep;

                Vector3 worldPos = new Vector3(wx, 0f, wz);

                // Temporary distance driver: distance from reference point.
                // Later you can replace this with "distance from shoreline".
                float dist = referencePoint ? Vector3.Distance(new Vector3(referencePoint.position.x, 0, referencePoint.position.z),
                                                              new Vector3(worldPos.x, 0, worldPos.z))
                                            : Vector3.Distance(Vector3.zero, new Vector3(worldPos.x, 0, worldPos.z));

                float t = Mathf.Clamp01(dist / maxDepthDistance);
                float depth01 = depthCurve.Evaluate(t);
                float depth = maxDepth * depth01;

                // Optional noise to break perfect smoothness
                float n = Mathf.PerlinNoise(wx * noiseScale, wz * noiseScale) - 0.5f;
                depth += n * noiseStrength;

                // Push down in Y (negative)
                verts[vi] = transform.InverseTransformPoint(new Vector3(wx, -Mathf.Max(0, depth), wz));
                uvs[vi] = new Vector2((float)x / xSegments, (float)z / zSegments);
                vi++;
            }
        }

        int ti = 0;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int i0 = z * (xSegments + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (xSegments + 1);
                int i3 = i2 + 1;

                tris[ti++] = i0;
                tris[ti++] = i2;
                tris[ti++] = i1;

                tris[ti++] = i1;
                tris[ti++] = i2;
                tris[ti++] = i3;
            }
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Optional collider
        var col = GetComponent<MeshCollider>();
        if (col)
        {
            col.sharedMesh = null;
            col.sharedMesh = mesh;
        }
    }
}
