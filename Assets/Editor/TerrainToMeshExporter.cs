using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class TerrainToMeshExporter
{
    [MenuItem("Tools/Terrain/Export Selected Terrain To Mesh")]
    static void ExportSelectedTerrainToMesh()
    {
        var selected = Selection.activeGameObject;
        Terrain terrain = selected ? selected.GetComponent<Terrain>() : null;

        if (terrain == null)
        {
            Debug.LogError("Select a Terrain object first.");
            return;
        }

        TerrainData td = terrain.terrainData;
        int res = td.heightmapResolution;
        float[,] heights = td.GetHeights(0, 0, res, res);
        Vector3 size = td.size;

        int vertCount = res * res;
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[(res - 1) * (res - 1) * 6];

        int v = 0;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float xf = x / (float)(res - 1);
                float zf = z / (float)(res - 1);
                float y = heights[z, x] * size.y;

                vertices[v] = new Vector3(xf * size.x, y, zf * size.z);
                uvs[v] = new Vector2(xf, zf);
                v++;
            }
        }

        int t = 0;
        for (int z = 0; z < res - 1; z++)
        {
            for (int x = 0; x < res - 1; x++)
            {
                int i = z * res + x;

                triangles[t++] = i;
                triangles[t++] = i + res;
                triangles[t++] = i + 1;

                triangles[t++] = i + 1;
                triangles[t++] = i + res;
                triangles[t++] = i + res + 1;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = terrain.name + "_TopSurfaceMesh";
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string meshPath = AssetDatabase.GenerateUniqueAssetPath("Assets/" + mesh.name + ".asset");
        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.SaveAssets();

        GameObject meshGO = new GameObject(mesh.name);
        meshGO.transform.position = terrain.transform.position;
        meshGO.transform.rotation = terrain.transform.rotation;
        meshGO.transform.localScale = Vector3.one;

        MeshFilter mf = meshGO.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = meshGO.AddComponent<MeshRenderer>();
        if (terrain.materialTemplate != null)
            mr.sharedMaterial = terrain.materialTemplate;

        Selection.activeGameObject = meshGO;
        EditorGUIUtility.PingObject(meshGO);

        Debug.Log("Mesh created at: " + meshPath);
    }
}