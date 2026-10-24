using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// EDITOR TOOL — Builds the 600 m valley and dresses it with real assets from the
/// LMHPOLY and ithappy packs. Not a greybox: this is the playable-looking pass.
///
/// Layout runs +Z and climbs 30 m:
///   Forest  z0-200   y0    coastal, sick, the great tree
///   City    z200-400 y15   stone terraces, houses, the water wheel
///   Factory z400-600 y30   rust and ruin, the outfall at the end
///
/// Safe to re-run: destroys only BT_World and rebuilds from a fixed random seed,
/// so the same layout comes back every time. Never touches Tree_Target,
/// Bridge_Target, TimeReserve_Target, Player or system objects — keep those at
/// scene root, because anything under BT_World is destroyed on rebuild.
/// </summary>
public static class BorrowedTimeValleyDressed
{
    private const string WorldRoot = "BT_World";
    private const int Seed = 20261024;

    private const float Width = 110f;        // half-width of the walkable floor
    private const float ForestZ0 = 0f,   ForestZ1 = 200f,  ForestY  = 0f;
    private const float CityZ0   = 200f, CityZ1   = 400f,  CityY    = 15f;
    private const float FactZ0   = 400f, FactZ1   = 600f,  FactY    = 30f;

    private static readonly Color ForestGround = new Color(0.26f, 0.36f, 0.22f);
    private static readonly Color CityGround   = new Color(0.68f, 0.64f, 0.55f);
    private static readonly Color FactGround   = new Color(0.40f, 0.29f, 0.25f);
    private static readonly Color RustCol      = new Color(0.46f, 0.28f, 0.22f);
    private static readonly Color WaterCol     = new Color(0.32f, 0.50f, 0.56f);
    private static readonly Color MarkerCol    = new Color(0.95f, 0.78f, 0.25f);
    private static readonly Color PuzzleCol    = new Color(0.58f, 0.40f, 0.72f);

    // Prefab pools, filled once per build.
    private static GameObject[] poolTrees, poolBigTrees, poolDeadwood, poolPlants,
                                poolBushes, poolFlowers, poolGrass, poolRocks,
                                poolHouses, poolRuins, poolLiana, poolMushroom;

    [MenuItem("Borrowed Time/Build Valley (dressed, 600m)")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[BT Valley] Exit Play mode first — the scene cannot be saved while playing.");
            return;
        }

        Random.InitState(Seed);
        LoadPools();

        var old = GameObject.Find(WorldRoot);
        if (old != null) Object.DestroyImmediate(old);
        var world = new GameObject(WorldRoot);

        BuildForest(world.transform);
        BuildCity(world.transform);
        BuildFactory(world.transform);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[BT Valley] Built 600 m dressed valley. Forest z0-200 y0 -> City z200-400 y15 " +
                  "-> Factory z400-600 y30. Yellow = keystone, purple = puzzle, blue = time source.");
    }

    // ──────────────────────────── ASSET POOLS ───────────────────────────────

    private static void LoadPools()
    {
        poolTrees    = Find("t:Prefab fabulous_tree", "big_fabulous");   // exclude the hero trees
        poolBigTrees = Find("t:Prefab big_fabulous_tree", null);
        poolDeadwood = Find("t:Prefab fabulous_deadwood", null);
        poolPlants   = Find("t:Prefab fabulous_plant", null);
        poolBushes   = Find("t:Prefab bush_0", null);
        poolMushroom = Find("t:Prefab fabulous_mushroom", null);
        poolLiana    = Find("t:Prefab liana_0", null);
        poolHouses   = Find("t:Prefab house_0", "castle");               // plain houses only
        poolRuins    = Find("t:Prefab ruins_0", null);
        // Box colliders keep the physics cost sane for scatter dressing.
        poolRocks    = Find("t:Prefab Rock_", null, "With_Box_Colliders");
        poolFlowers  = Find("t:Prefab Flower_", null, "TwoSided");
        poolGrass    = Find("t:Prefab Grass", null, "Grass3D");

        Debug.Log($"[BT Valley] pools — trees:{poolTrees.Length} big:{poolBigTrees.Length} " +
                  $"deadwood:{poolDeadwood.Length} plants:{poolPlants.Length} rocks:{poolRocks.Length} " +
                  $"flowers:{poolFlowers.Length} grass:{poolGrass.Length} houses:{poolHouses.Length} " +
                  $"ruins:{poolRuins.Length} liana:{poolLiana.Length}");
    }

    /// <summary>Find prefabs by name filter, then narrow by folder.
    /// FindAssets ANDs space-separated terms against the asset NAME only, so folder
    /// names have to be filtered here rather than in the query string.</summary>
    private static GameObject[] Find(string filter, string exclude, string include = null)
    {
        var guids = AssetDatabase.FindAssets(filter);
        var list = new List<GameObject>();
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            if (exclude != null && path.Contains(exclude)) continue;
            if (include != null && !path.Contains(include)) continue;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) list.Add(go);
        }
        return list.ToArray();
    }

    private static GameObject Pick(GameObject[] pool)
        => (pool == null || pool.Length == 0) ? null : pool[Random.Range(0, pool.Length)];

    /// <summary>Place one prefab instance. Returns null if the pool was empty.</summary>
    private static GameObject Place(Transform parent, GameObject[] pool, Vector3 pos,
                                    float scaleMin = 1f, float scaleMax = 1f)
    {
        var prefab = Pick(pool);
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        go.transform.localScale = Vector3.one * Random.Range(scaleMin, scaleMax);
        return go;
    }

    /// <summary>Scatter a pool across a band of the valley, avoiding the central path.</summary>
    private static void Scatter(Transform parent, string groupName, GameObject[] pool,
                                float z0, float z1, int count,
                                float scaleMin, float scaleMax,
                                float pathClearance = 14f, float maxX = Width)
    {
        if (pool == null || pool.Length == 0) return;
        var group = new GameObject(groupName);
        group.transform.SetParent(parent, false);

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(pathClearance, maxX) * (Random.value < 0.5f ? -1f : 1f);
            float z = Random.Range(z0, z1);
            Place(group.transform, pool, new Vector3(x, 0f, z), scaleMin, scaleMax);
        }
    }

    // ───────────────────────────────── FOREST ───────────────────────────────

    private static void BuildForest(Transform parent)
    {
        var zone = Zone("Zone_01_Forest", parent, ForestY);

        Ground(zone, "Ground_Forest", ForestZ0, ForestZ1, ForestGround);

        // Spawn, with the great tree visible straight ahead.
        Marker(zone, "SPAWN_Player", new Vector3(0f, 1.5f, 12f), 2f);

        // The hero tree. Scaled up — pack trees are ordinary-sized.
        var big = Place(zone, poolBigTrees, new Vector3(0f, 0f, 150f), 3.2f, 3.2f);
        if (big != null) big.name = "KEYSTONE_GreatTree_VISUAL";
        Label(zone, "NOTE_GreatTree_cost60_rootsCrossRavine", new Vector3(0f, 30f, 150f));

        // Deadwood time sources — hand-placed along the approach so the tutorial reads.
        var dwGroup = new GameObject("TimeSources_Deadwood");
        dwGroup.transform.SetParent(zone, false);
        for (int i = 0; i < 5; i++)
        {
            var d = Place(dwGroup.transform, poolDeadwood,
                          new Vector3(-16f + i * 8f, 0f, 35f + i * 14f), 1.4f, 1.8f);
            if (d != null) d.name = $"TimeSource_Deadwood_{i + 1}";
        }

        // Dressing.
        Scatter(zone, "Trees", poolTrees, ForestZ0, ForestZ1, 70, 0.8f, 1.6f, 12f);
        Scatter(zone, "Bushes", poolBushes, ForestZ0, ForestZ1, 50, 0.8f, 1.4f, 8f);
        Scatter(zone, "Plants", poolPlants, ForestZ0, ForestZ1, 60, 0.9f, 1.5f, 6f);
        Scatter(zone, "Mushrooms", poolMushroom, ForestZ0, ForestZ1, 30, 0.8f, 1.6f, 6f);
        Scatter(zone, "Rocks", poolRocks, ForestZ0, ForestZ1, 35, 0.6f, 1.8f, 10f);
        Scatter(zone, "Flowers", poolFlowers, ForestZ0, ForestZ1, 90, 0.8f, 1.3f, 4f);
        Scatter(zone, "Grass", poolGrass, ForestZ0, ForestZ1, 120, 0.8f, 1.4f, 3f);

        // Stream down the east side.
        Block(zone, "Stream", new Vector3(36f, -0.25f, 100f), new Vector3(7f, 0.4f, 190f), WaterCol);

        // Puzzles — the walk needs things to solve, not just distance.
        Puzzle(zone, "PZL_F1_FallenTrunk_AGE_toCrumble", new Vector3(0f, 1.2f, 62f), new Vector3(16f, 2.4f, 2.4f), 80f);
        Puzzle(zone, "PZL_F2_ShortVines_REJUV_toClimb", new Vector3(-22f, 5f, 108f), new Vector3(3f, 10f, 1f), 0f);
        Puzzle(zone, "PZL_F3_Boulder_AGE_toErode", new Vector3(14f, 2f, 128f), new Vector3(7f, 4f, 7f), 0f);
        Puzzle(zone, "PZL_F4_LogBridge_REJUV_overStream", new Vector3(36f, 0.8f, 86f), new Vector3(3f, 0.6f, 14f), 90f);

        // The ravine, and the root bridge that is the forest's reward.
        Block(zone, "Ravine_Floor", new Vector3(0f, -8f, 180f), new Vector3(Width * 2f, 1f, 18f),
              new Color(0.16f, 0.18f, 0.16f));
        var roots = Block(zone, "GATE_RootBridge", new Vector3(0f, 0.3f, 180f),
                          new Vector3(9f, 0.6f, 20f), MarkerCol);
        roots.SetActive(false);

        Ramp(zone, "LINK_ForestToCity", new Vector3(0f, 0f, 194f), new Vector3(16f, 1f, 30f), CityY - ForestY, ForestGround);
    }

    // ────────────────────────────────── CITY ────────────────────────────────

    private static void BuildCity(Transform parent)
    {
        var zone = Zone("Zone_02_City", parent, CityY);

        Ground(zone, "Ground_City", CityZ0, CityZ1, CityGround);

        // Canal down the middle-east, catching what flows from the factory.
        Block(zone, "Canal", new Vector3(30f, -0.3f, 300f), new Vector3(14f, 0.6f, 190f), WaterCol);

        // Houses in two rows along the street, facing in.
        var hGroup = new GameObject("Houses");
        hGroup.transform.SetParent(zone, false);
        for (int i = 0; i < 12; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            float z = CityZ0 + 22f + (i / 2) * 29f;
            var h = Place(hGroup.transform, poolHouses, new Vector3(side * 24f, 0f, z), 1f, 1.3f);
            if (h != null)
            {
                h.name = $"House_{i + 1}";
                h.transform.localRotation = Quaternion.Euler(0f, side > 0f ? 270f : 90f, 0f);
            }
        }

        Scatter(zone, "Ruins", poolRuins, CityZ0 + 10f, CityZ1 - 10f, 7, 1f, 1.6f, 36f);
        Scatter(zone, "Liana", poolLiana, CityZ0, CityZ1, 14, 1f, 1.8f, 20f);
        Scatter(zone, "Rocks", poolRocks, CityZ0, CityZ1, 20, 0.5f, 1.2f, 40f);
        Scatter(zone, "Grass", poolGrass, CityZ0, CityZ1, 50, 0.6f, 1.1f, 30f);
        Scatter(zone, "Plants", poolPlants, CityZ0, CityZ1, 25, 0.7f, 1.2f, 30f);

        // Time sources: crumbling arches, stood in by tall rocks until Blender work lands.
        var aGroup = new GameObject("TimeSources_StoneArch");
        aGroup.transform.SetParent(zone, false);
        for (int i = 0; i < 4; i++)
        {
            var a = Block(aGroup.transform, $"TimeSource_StoneArch_{i + 1}",
                          new Vector3((i % 2 == 0 ? -1f : 1f) * 40f, 3.5f, CityZ0 + 40f + i * 42f),
                          new Vector3(7f, 7f, 2f), new Color(0.35f, 0.65f, 0.85f));
            a.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }

        // Blender work, blocked out for now.
        Block(zone, "BUILD_ClockTower", new Vector3(-30f, 15f, 330f), new Vector3(9f, 30f, 9f), MarkerCol);
        Label(zone, "NOTE_ClockTower_BLENDER", new Vector3(-30f, 32f, 330f));
        Marker(zone, "KEYSTONE_WaterWheel_ANCHOR", new Vector3(30f, 5f, 372f), 3f);
        Label(zone, "NOTE_WaterWheel_cost80_BLENDER", new Vector3(30f, 12f, 372f));
        Block(zone, "BUILD_ArchedBridge", new Vector3(30f, 1.5f, 290f), new Vector3(26f, 1.5f, 8f),
              new Color(0.80f, 0.76f, 0.66f));

        // Puzzles.
        Puzzle(zone, "PZL_C1_CollapsedStair_REJUV_toClimb", new Vector3(-18f, 2f, 240f), new Vector3(8f, 4f, 10f), 0f);
        Puzzle(zone, "PZL_C2_JammedGate_AGE_toRustOpen", new Vector3(0f, 2.5f, 280f), new Vector3(10f, 5f, 1.2f), 0f);
        Puzzle(zone, "PZL_C3_FallenArch_REJUV_toSpanCanal", new Vector3(30f, 2f, 330f), new Vector3(16f, 4f, 3f), 90f);
        Puzzle(zone, "PZL_C4_DeadIvy_REJUV_toClimbWall", new Vector3(-26f, 5f, 360f), new Vector3(3f, 10f, 1f), 0f);

        var lift = Block(zone, "GATE_CanalLift", new Vector3(0f, 0.5f, 392f), new Vector3(12f, 1f, 12f), MarkerCol);
        lift.SetActive(false);

        Ramp(zone, "LINK_CityToFactory", new Vector3(0f, 0f, 394f), new Vector3(16f, 1f, 28f), FactY - CityY, CityGround);
    }

    // ───────────────────────────────── FACTORY ──────────────────────────────

    private static void BuildFactory(Transform parent)
    {
        var zone = Zone("Zone_03_Factory", parent, FactY);

        Ground(zone, "Ground_Factory", FactZ0, FactZ1, FactGround);

        // No industrial assets exist in either pack, so this zone stays blocked out
        // until Blender work lands. Ruins and rock stand in for rubble.
        Scatter(zone, "Rubble_Rocks", poolRocks, FactZ0, FactZ1, 30, 0.5f, 1.6f, 14f);
        Scatter(zone, "Ruins", poolRuins, FactZ0 + 10f, FactZ1 - 20f, 6, 1f, 1.5f, 30f);
        // Weeds forcing through concrete — the only life up here, and it matters.
        Scatter(zone, "Weeds", poolPlants, FactZ0, FactZ1, 40, 0.5f, 1.0f, 6f);
        Scatter(zone, "Weed_Grass", poolGrass, FactZ0, FactZ1, 60, 0.5f, 0.9f, 4f);

        Block(zone, "BUILD_RailTrack", new Vector3(0f, 0.2f, FactZ0 + 45f), new Vector3(5f, 0.4f, 80f),
              new Color(0.32f, 0.28f, 0.26f));
        Block(zone, "BUILD_RailCar", new Vector3(8f, 2f, FactZ0 + 60f), new Vector3(4f, 4f, 10f), RustCol);

        var mGroup = new GameObject("TimeSources_Machinery");
        mGroup.transform.SetParent(zone, false);
        for (int i = 0; i < 3; i++)
            Block(mGroup.transform, $"TimeSource_Machinery_{i + 1}",
                  new Vector3(-16f + i * 16f, 2f, FactZ0 + 90f + i * 22f),
                  new Vector3(6f, 4f, 6f), new Color(0.35f, 0.65f, 0.85f));

        Puzzle(zone, "PZL_X1_CollapsedPipe_REJUV_toTunnel", new Vector3(0f, 2f, FactZ0 + 120f), new Vector3(22f, 4f, 3f), 0f);
        Puzzle(zone, "PZL_X2_SealedDoor_AGE_toRustThrough", new Vector3(0f, 2.5f, FactZ0 + 145f), new Vector3(11f, 5f, 1.5f), 0f);
        Puzzle(zone, "PZL_X3_ConveyorGap_REJUV_toBridge", new Vector3(0f, 1f, FactZ0 + 165f), new Vector3(15f, 0.8f, 11f), 0f);
        Puzzle(zone, "PZL_X4_PipeMaze_AGE_toCollapse", new Vector3(0f, 3f, FactZ0 + 182f), new Vector3(24f, 6f, 4f), 0f);

        for (int i = 0; i < 2; i++)
            Block(zone, $"BUILD_Smokestack_{i + 1}", new Vector3(-16f + i * 32f, 22f, FactZ1 - 22f),
                  new Vector3(7f, 44f, 7f), RustCol);

        Block(zone, "BUILD_Outfall", new Vector3(0f, 3f, FactZ1 - 12f), new Vector3(16f, 6f, 10f),
              new Color(0.26f, 0.23f, 0.21f));
        Marker(zone, "KEYSTONE_SeedSite", new Vector3(0f, 1.5f, FactZ1 - 24f), 3f);
        Label(zone, "NOTE_Seed_costALL_becomesYggdrasil", new Vector3(0f, 8f, FactZ1 - 24f));

        var ygg = Block(zone, "FINALE_Yggdrasil_scale", new Vector3(0f, 65f, FactZ1 - 24f),
                        new Vector3(16f, 130f, 16f), new Color(0.35f, 0.60f, 0.35f));
        ygg.SetActive(false);
    }

    // ──────────────────────────────── HELPERS ───────────────────────────────

    private static Transform Zone(string name, Transform parent, float y)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, y, 0f);
        return go.transform;
    }

    private static void Ground(Transform parent, string name, float z0, float z1, Color col)
    {
        float depth = z1 - z0;
        Block(parent, name, new Vector3(0f, -0.5f, z0 + depth * 0.5f),
              new Vector3(Width * 2f, 1f, depth), col);
    }

    private static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 size, Color col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        Paint(go, col);
        return go;
    }

    private static GameObject Puzzle(Transform parent, string name, Vector3 pos, Vector3 size, float yaw)
    {
        var go = Block(parent, name, pos, size, PuzzleCol);
        if (!Mathf.Approximately(yaw, 0f)) go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }

    private static GameObject Marker(Transform parent, string name, Vector3 pos, float size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * size;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        Paint(go, MarkerCol);
        return go;
    }

    private static GameObject Ramp(Transform parent, string name, Vector3 pos,
                                   Vector3 size, float rise, Color col)
    {
        var go = Block(parent, name, pos, size, col);
        float angle = Mathf.Atan2(rise, size.z) * Mathf.Rad2Deg;
        go.transform.localRotation = Quaternion.Euler(-angle, 0f, 0f);
        go.transform.localPosition = pos + new Vector3(0f, rise * 0.5f, 0f);
        return go;
    }

    private static GameObject Label(Transform parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go;
    }

    private static void Paint(GameObject go, Color col)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { color = col };
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}
