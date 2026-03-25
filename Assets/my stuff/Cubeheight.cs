using UnityEngine;

public class Cubeheight : MonoBehaviour
{
    public Terrain terrain;
    public float targetWorldHeight = -3f; // depth in meters (water level = 0)
    public Vector3 areaSize = new Vector3(50, 50, 50); // cube size

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ApplyDepth();
        }
    }

    void ApplyDepth()
    {
        TerrainData data = terrain.terrainData;

        Vector3 terrainPos = terrain.transform.position;
        int resolution = data.heightmapResolution;

        float[,] heights = data.GetHeights(0, 0, resolution, resolution);

        Vector3 worldPos = transform.position;

        float terrainWidth = data.size.x;
        float terrainLength = data.size.z;
        float terrainHeight = data.size.y;

        int centerX = Mathf.RoundToInt(((worldPos.x - terrainPos.x) / terrainWidth) * resolution);
        int centerZ = Mathf.RoundToInt(((worldPos.z - terrainPos.z) / terrainLength) * resolution);

        int radiusX = Mathf.RoundToInt((areaSize.x / terrainWidth) * resolution * 0.5f);
        int radiusZ = Mathf.RoundToInt((areaSize.z / terrainLength) * resolution * 0.5f);

        float normalizedHeight = (targetWorldHeight - terrainPos.y) / terrainHeight;

        for (int z = -radiusZ; z <= radiusZ; z++)
        {
            for (int x = -radiusX; x <= radiusX; x++)
            {
                int px = centerX + x;
                int pz = centerZ + z;

                if (px >= 0 && px < resolution && pz >= 0 && pz < resolution)
                {
                    heights[pz, px] = normalizedHeight;
                }
            }
        }

        data.SetHeights(0, 0, heights);
    }
}