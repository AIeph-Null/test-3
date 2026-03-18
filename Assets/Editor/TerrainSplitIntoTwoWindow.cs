// Assets/Editor/TerrainSplitIntoTwoWindow.cs

using System.IO;
using UnityEditor;
using UnityEngine;

public class TerrainSplitIntoTwoWindow : EditorWindow
{
    private float cutWorldY = 100f;
    private bool disableOriginal = true;

    [MenuItem("Tools/Terrain/Split Selected Terrain Into Two")]
    private static void Open()
    {
        var window = GetWindow<TerrainSplitIntoTwoWindow>("Split Terrain");
        window.minSize = new Vector2(360f, 140f);
        window.ResetCutToMiddle();
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnGUI()
    {
        Terrain terrain = GetSelectedTerrain();

        if (terrain == null)
        {
            EditorGUILayout.HelpBox("Select a Terrain GameObject first.", MessageType.Info);
            return;
        }

        TerrainData data = terrain.terrainData;
        float minY = terrain.transform.position.y;
        float maxY = terrain.transform.position.y + data.size.y;

        if (cutWorldY <= minY || cutWorldY >= maxY)
            cutWorldY = Mathf.Lerp(minY, maxY, 0.5f);

        EditorGUILayout.LabelField("Selected Terrain", terrain.name);
        EditorGUILayout.LabelField("Valid World Y Range", $"{minY:0.###}  to  {maxY:0.###}");

        cutWorldY = EditorGUILayout.Slider("Cut World Y", cutWorldY, minY + 0.01f, maxY - 0.01f);
        disableOriginal = EditorGUILayout.Toggle("Disable Original", disableOriginal);

        GUILayout.Space(8);

        if (GUILayout.Button("Split Terrain"))
        {
            SplitTerrain(terrain, cutWorldY, disableOriginal);
        }

        GUILayout.Space(4);
        if (GUILayout.Button("Reset Cut To Middle"))
        {
            ResetCutToMiddle();
        }
    }

    private static Terrain GetSelectedTerrain()
    {
        if (Selection.activeGameObject == null) return null;
        return Selection.activeGameObject.GetComponent<Terrain>();
    }

    private void ResetCutToMiddle()
    {
        Terrain terrain = GetSelectedTerrain();
        if (terrain == null || terrain.terrainData == null) return;

        cutWorldY = terrain.transform.position.y + terrain.terrainData.size.y * 0.5f;
    }

    private static void SplitTerrain(Terrain source, float cutWorldY, bool disableOriginal)
    {
        if (source == null || source.terrainData == null)
        {
            Debug.LogError("No valid Terrain selected.");
            return;
        }

        TerrainData srcData = source.terrainData;
        Vector3 srcPos = source.transform.position;
        Vector3 srcSize = srcData.size;

        float cutLocalY = cutWorldY - srcPos.y;
        cutLocalY = Mathf.Clamp(cutLocalY, 0.01f, srcSize.y - 0.01f);

        int hmRes = srcData.heightmapResolution;
        float[,] srcHeights = srcData.GetHeights(0, 0, hmRes, hmRes);

        float lowerHeightRange = cutLocalY;
        float upperHeightRange = srcSize.y - cutLocalY;

        float[,] lowerHeights = new float[hmRes, hmRes];
        float[,] upperHeights = new float[hmRes, hmRes];

        for (int y = 0; y < hmRes; y++)
        {
            for (int x = 0; x < hmRes; x++)
            {
                float originalLocalY = srcHeights[y, x] * srcSize.y;

                float lowerLocalY = Mathf.Min(originalLocalY, cutLocalY);
                float upperLocalY = Mathf.Max(0f, originalLocalY - cutLocalY);

                lowerHeights[y, x] = lowerLocalY / lowerHeightRange;
                upperHeights[y, x] = upperLocalY / upperHeightRange;
            }
        }

        string srcAssetPath = AssetDatabase.GetAssetPath(srcData);
        string folder = string.IsNullOrEmpty(srcAssetPath)
            ? "Assets"
            : Path.GetDirectoryName(srcAssetPath).Replace("\\", "/");

        TerrainData lowerData = Object.Instantiate(srcData);
        TerrainData upperData = Object.Instantiate(srcData);

        lowerData.name = srcData.name + "_Lower";
        upperData.name = srcData.name + "_Upper";

        string lowerAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{lowerData.name}.asset");
        string upperAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{upperData.name}.asset");

        AssetDatabase.CreateAsset(lowerData, lowerAssetPath);
        AssetDatabase.CreateAsset(upperData, upperAssetPath);
        AssetDatabase.SaveAssets();

        lowerData.size = new Vector3(srcSize.x, lowerHeightRange, srcSize.z);
        upperData.size = new Vector3(srcSize.x, upperHeightRange, srcSize.z);

        lowerData.SetHeights(0, 0, lowerHeights);
        upperData.SetHeights(0, 0, upperHeights);

#if UNITY_2019_3_OR_NEWER
        int holesRes = srcData.holesResolution;
        bool[,] srcHoles = srcData.GetHoles(0, 0, holesRes, holesRes);
        bool[,] lowerHoles = new bool[holesRes, holesRes];
        bool[,] upperHoles = new bool[holesRes, holesRes];

        for (int y = 0; y < holesRes; y++)
        {
            for (int x = 0; x < holesRes; x++)
            {
                float u = holesRes > 1 ? x / (float)(holesRes - 1) : 0f;
                float v = holesRes > 1 ? y / (float)(holesRes - 1) : 0f;

                float localHeight = srcData.GetInterpolatedHeight(u, v);
                bool surfaceExists = srcHoles[y, x];

                lowerHoles[y, x] = surfaceExists;
                upperHoles[y, x] = surfaceExists && localHeight > cutLocalY;
            }
        }

        lowerData.SetHoles(0, 0, lowerHoles);
        upperData.SetHoles(0, 0, upperHoles);
#endif

        Terrain lowerTerrain = CreateTerrainObject(
            source,
            lowerData,
            source.name + "_Lower",
            srcPos
        );

        Terrain upperTerrain = CreateTerrainObject(
            source,
            upperData,
            source.name + "_Upper",
            new Vector3(srcPos.x, cutWorldY, srcPos.z)
        );

        if (disableOriginal)
            source.gameObject.SetActive(false);

        lowerTerrain.Flush();
        upperTerrain.Flush();

        Selection.objects = new Object[] { upperTerrain.gameObject, lowerTerrain.gameObject };

        Debug.Log(
            $"Split complete.\n" +
            $"Lower: {lowerTerrain.name}\n" +
            $"Upper: {upperTerrain.name}\n" +
            $"Delete or disable '{lowerTerrain.name}' if you only want the top part."
        );
    }

    private static Terrain CreateTerrainObject(Terrain source, TerrainData data, string newName, Vector3 worldPosition)
    {
        GameObject go = Terrain.CreateTerrainGameObject(data);
        go.name = newName;
        go.layer = source.gameObject.layer;
        go.tag = source.gameObject.tag;

        if (source.transform.parent != null)
            go.transform.SetParent(source.transform.parent, true);

        go.transform.position = worldPosition;

        Terrain terrain = go.GetComponent<Terrain>();
        TerrainCollider terrainCollider = go.GetComponent<TerrainCollider>();

        if (terrainCollider != null)
            terrainCollider.terrainData = data;

        CopyTerrainSettings(source, terrain);

        return terrain;
    }

    private static void CopyTerrainSettings(Terrain src, Terrain dst)
    {
        dst.allowAutoConnect = src.allowAutoConnect;
        dst.groupingID = src.groupingID;

        dst.drawHeightmap = src.drawHeightmap;
        dst.drawTreesAndFoliage = src.drawTreesAndFoliage;
        dst.drawInstanced = src.drawInstanced;

        dst.heightmapPixelError = src.heightmapPixelError;
        dst.basemapDistance = src.basemapDistance;

        dst.detailObjectDistance = src.detailObjectDistance;
        dst.detailObjectDensity = src.detailObjectDensity;

        dst.treeDistance = src.treeDistance;
        dst.treeBillboardDistance = src.treeBillboardDistance;
        dst.treeCrossFadeLength = src.treeCrossFadeLength;
        dst.treeMaximumFullLODCount = src.treeMaximumFullLODCount;

        dst.materialTemplate = src.materialTemplate;
        dst.reflectionProbeUsage = src.reflectionProbeUsage;
        dst.shadowCastingMode = src.shadowCastingMode;
    }
}