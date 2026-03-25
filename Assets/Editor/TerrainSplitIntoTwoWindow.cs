using System.IO;
using UnityEditor;
using UnityEngine;

public class TerrainSplitIntoTwoBlocksWindow : EditorWindow
{
    private float cutWorldY = 100f;
    private bool disableOriginal = true;
    private bool clearFoliage = true;

    [MenuItem("Tools/Terrain/Split Selected Terrain Into Two Blocks")]
    private static void Open()
    {
        var window = GetWindow<TerrainSplitIntoTwoBlocksWindow>("Split Terrain");
        window.minSize = new Vector2(380f, 160f);
        window.ResetCutToMiddle();
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnGUI()
    {
        Terrain terrain = GetSelectedTerrain();

        if (terrain == null || terrain.terrainData == null)
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
        EditorGUILayout.LabelField("Valid World Y Range", $"{minY:0.###} to {maxY:0.###}");

        cutWorldY = EditorGUILayout.Slider("Cut World Y", cutWorldY, minY + 0.01f, maxY - 0.01f);
        disableOriginal = EditorGUILayout.Toggle("Disable Original", disableOriginal);
        clearFoliage = EditorGUILayout.Toggle("Clear Trees / Details", clearFoliage);

        GUILayout.Space(8);

        if (GUILayout.Button("Split Terrain"))
        {
            SplitTerrainIntoBlocks(terrain, cutWorldY, disableOriginal, clearFoliage);
        }

        if (GUILayout.Button("Reset Cut To Middle"))
        {
            ResetCutToMiddle();
        }
    }

    private static Terrain GetSelectedTerrain()
    {
        return Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Terrain>() : null;
    }

    private void ResetCutToMiddle()
    {
        Terrain terrain = GetSelectedTerrain();
        if (terrain == null || terrain.terrainData == null) return;

        cutWorldY = terrain.transform.position.y + terrain.terrainData.size.y * 0.5f;
    }

    private static void SplitTerrainIntoBlocks(Terrain source, float cutWorldY, bool disableOriginal, bool clearFoliage)
    {
        TerrainData srcData = source.terrainData;
        Vector3 srcPos = source.transform.position;
        Vector3 srcSize = srcData.size;

        float cutLocalY = Mathf.Clamp(cutWorldY - srcPos.y, 0.01f, srcSize.y - 0.01f);

        int hmRes = srcData.heightmapResolution;
        float[,] srcHeights = srcData.GetHeights(0, 0, hmRes, hmRes);

        float lowerHeightRange = Mathf.Max(0.01f, cutLocalY);
        float upperHeightRange = Mathf.Max(0.01f, srcSize.y - cutLocalY);

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
        string folder = "Assets";
        if (!string.IsNullOrEmpty(srcAssetPath))
        {
            string dir = Path.GetDirectoryName(srcAssetPath);
            if (!string.IsNullOrEmpty(dir))
                folder = dir.Replace("\\", "/");
        }

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
        SetAllSurface(lowerData);
        SetAllSurface(upperData);
#endif

        if (clearFoliage)
        {
            ClearVegetation(lowerData);
            ClearVegetation(upperData);
        }

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

        if (clearFoliage)
        {
            lowerTerrain.drawTreesAndFoliage = false;
            upperTerrain.drawTreesAndFoliage = false;
        }

        lowerTerrain.Flush();
        upperTerrain.Flush();

        if (disableOriginal)
            source.gameObject.SetActive(false);

        Selection.objects = new Object[] { lowerTerrain.gameObject, upperTerrain.gameObject };

        Debug.Log(
            $"Split complete.\n" +
            $"Lower: {lowerTerrain.name}\n" +
            $"Upper: {upperTerrain.name}\n" +
            $"This version keeps both halves as solid terrain tiles (no holes)."
        );
    }

#if UNITY_2019_3_OR_NEWER
    private static void SetAllSurface(TerrainData data)
    {
        int res = data.holesResolution;
        bool[,] holes = new bool[res, res];

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                holes[y, x] = true; // true = surface
            }
        }

        data.SetHoles(0, 0, holes);
    }
#endif

    private static void ClearVegetation(TerrainData data)
    {
        data.SetTreeInstances(new TreeInstance[0], false);

        int layerCount = data.detailPrototypes != null ? data.detailPrototypes.Length : 0;
        if (layerCount <= 0) return;

        int[,] empty = new int[data.detailWidth, data.detailHeight];

        for (int layer = 0; layer < layerCount; layer++)
        {
            data.SetDetailLayer(0, 0, layer, empty);
        }
    }

    private static Terrain CreateTerrainObject(Terrain source, TerrainData data, string newName, Vector3 worldPosition)
    {
        GameObject go = Terrain.CreateTerrainGameObject(data);
        go.name = newName;

        if (source.transform.parent != null)
            go.transform.SetParent(source.transform.parent, true);

        go.transform.position = worldPosition;
        go.layer = source.gameObject.layer;
        go.tag = source.gameObject.tag;

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

        dst.heightmapPixelError = src.heightmapPixelError;
        dst.basemapDistance = src.basemapDistance;

        dst.detailObjectDistance = src.detailObjectDistance;
        dst.detailObjectDensity = src.detailObjectDensity;

        dst.treeDistance = src.treeDistance;
        dst.treeBillboardDistance = src.treeBillboardDistance;
        dst.treeCrossFadeLength = src.treeCrossFadeLength;
        dst.treeMaximumFullLODCount = src.treeMaximumFullLODCount;

        dst.materialTemplate = src.materialTemplate;
    }
}