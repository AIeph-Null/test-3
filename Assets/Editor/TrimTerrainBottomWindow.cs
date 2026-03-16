using UnityEditor;
using UnityEngine;

public class TrimTerrainBottomWindow : EditorWindow
{
    private float bottomMargin = 5f;

    [MenuItem("Tools/Terrain/Trim Bottom")]
    private static void Open()
    {
        GetWindow<TrimTerrainBottomWindow>("Trim Terrain Bottom");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Select one or more Terrain objects. This keeps the terrain surface in the same world position, while moving the hidden bottom upward.",
            MessageType.Info);

        bottomMargin = Mathf.Max(0f, EditorGUILayout.FloatField("Bottom Margin", bottomMargin));

        GUI.enabled = Selection.gameObjects.Length > 0;
        if (GUILayout.Button("Trim Selected Terrains"))
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                Terrain terrain = go.GetComponent<Terrain>();
                if (terrain != null)
                {
                    TrimTerrain(terrain, bottomMargin);
                }
            }
        }
        GUI.enabled = true;
    }

    private static void TrimTerrain(Terrain terrain, float margin)
    {
        TerrainData data = terrain.terrainData;
        if (data == null) return;

        int res = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, res, res);

        float oldBaseY = terrain.transform.position.y;
        float oldHeight = data.size.y;

        float minWorldY = float.PositiveInfinity;
        float maxWorldY = float.NegativeInfinity;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float worldY = oldBaseY + heights[y, x] * oldHeight;
                if (worldY < minWorldY) minWorldY = worldY;
                if (worldY > maxWorldY) maxWorldY = worldY;
            }
        }

        float newBaseY = minWorldY - margin;
        float newHeight = Mathf.Max(1f, maxWorldY - newBaseY);

        float[,] newHeights = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float worldY = oldBaseY + heights[y, x] * oldHeight;
                newHeights[y, x] = Mathf.Clamp01((worldY - newBaseY) / newHeight);
            }
        }

        Undo.RegisterCompleteObjectUndo(data, "Trim Terrain Bottom");
        Undo.RegisterCompleteObjectUndo(terrain.transform, "Trim Terrain Bottom");

        Vector3 size = data.size;
        data.size = new Vector3(size.x, newHeight, size.z);

        Vector3 pos = terrain.transform.position;
        terrain.transform.position = new Vector3(pos.x, newBaseY, pos.z);

        data.SetHeights(0, 0, newHeights);
        terrain.Flush();

        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(terrain);
    }
}