using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Continue Claude's scene using existing tree prefabs and terrain; no map regeneration.</summary>
public static class BorrowedTimeForestKeystoneSetup
{
    [MenuItem("Borrowed Time/Handoff/Wire Great Tree forest keystone")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        if (GameObject.Find("ForestKeystone") != null) throw new InvalidOperationException("Forest keystone already exists; nothing overwritten.");
        var target = GameObject.Find("Tree_Target").GetComponent<TimeTarget>();
        var dressing = GameObject.Find("BT_ForestDressing");
        var oldModel = GameObject.Find("BT_ForestDressing/GreatTree_VISUAL_old");
        if (oldModel == null) throw new InvalidOperationException("Claude's corrected old tree visual is missing.");
        var shader = Shader.Find("BORROWED TIME/TreePhaseDither");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Tree phase shader is not ready.");
        // Preflight every required source/output before touching the shared scene.
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/03_EnvironmentArt/Models/BT_ForestTerrain_Keystone.asset") != null)
            throw new InvalidOperationException("Keystone terrain output already exists; scene left unchanged.");
        foreach (var phase in new[] { "young", "mature" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_EnvironmentArt/Prefabs/BT_great_" + phase + ".prefab");
            if (asset == null || !asset.GetComponentsInChildren<Renderer>(true).Any(r => r.name.EndsWith("_Trunk")))
                throw new InvalidOperationException("Missing phase prefab/trunk: " + phase);
        }
        var preflightWorld = GameObject.Find("BT_World");
        foreach (var path in new[] { "Zone_02_City", "Zone_02_City/Ground_City", "Zone_01_Forest/GATE_RootBridge", "Zone_01_Forest/LINK_ForestToCity" })
            if (preflightWorld == null || preflightWorld.transform.Find(path) == null) throw new InvalidOperationException("Missing scene path: " + path);
        if (GameObject.Find("BT_ForestTerrain") == null || dressing.transform.Find("Trees_Dying") == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_EnvironmentArt/Prefabs/BT_broadleaf_mature.prefab") == null)
            throw new InvalidOperationException("Missing terrain or forest source dressing.");
        var terrainSource = GameObject.Find("BT_ForestTerrain");
        var terrainFilter = terrainSource.GetComponent<MeshFilter>();
        var lightSource = GameObject.Find("Directional Light");
        if (terrainFilter == null || terrainFilter.sharedMesh == null || terrainSource.GetComponent<MeshCollider>() == null ||
            lightSource == null || lightSource.GetComponent<Light>() == null ||
            !oldModel.GetComponentsInChildren<Renderer>(true).Any(r => r.name.EndsWith("_Trunk")))
            throw new InvalidOperationException("Missing terrain mesh/collider, directional light or old tree trunk; scene left unchanged.");
        foreach (Transform child in target.transform) child.gameObject.SetActive(false);
        target.transform.position = new Vector3(0f, 0f, 150f);
        target.transform.localScale = Vector3.one;
        string[] names = { "Young", "Mature", "Withered" };
        var phases = new GameObject[3]; var colliders = new Collider[3];
        for (int i = 0; i < 3; i++)
        {
            phases[i] = new GameObject("Phase_" + i + "_" + names[i]);
            phases[i].transform.SetParent(target.transform, false);
            GameObject model;
            if (i == 2) { model = oldModel; model.transform.SetParent(phases[i].transform, true); }
            else
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_EnvironmentArt/Prefabs/BT_great_" + (i == 0 ? "young" : "mature") + ".prefab");
                if (asset == null) throw new InvalidOperationException("Missing great tree phase prefab.");
                model = (GameObject)PrefabUtility.InstantiatePrefab(asset, phases[i].transform);
                model.transform.localScale = Vector3.one * 1.25f;
            }
            var trunk = model.GetComponentsInChildren<Renderer>(true).First(r => r.name.EndsWith("_Trunk"));
            Vector3 anchor = new Vector3(trunk.bounds.center.x, trunk.bounds.min.y, trunk.bounds.center.z);
            model.transform.position += target.transform.position - anchor;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.name.Contains("Trunk") && !filter.name.Contains("Buttress")) continue;
                var collider = filter.GetComponent<MeshCollider>();
                if (collider == null) collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                if (filter.name.EndsWith("_Trunk")) colliders[i] = collider;
            }
            phases[i].SetActive(i == 2);
        }
        var state = new SerializedObject(target); var array = state.FindProperty("states"); array.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            var item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("stateName").stringValue = names[i];
            item.FindPropertyRelative("visualRoot").objectReferenceValue = phases[i];
            item.FindPropertyRelative("stateCollider").objectReferenceValue = colliders[i];
            item.FindPropertyRelative("isHealthy").boolValue = i == 1;
            item.FindPropertyRelative("isTraversable").boolValue = false;
        }
        state.FindProperty("startingStateIndex").intValue = 2;
        state.FindProperty("energyCostPerChange").floatValue = 60f;
        state.FindProperty("displayName").stringValue = "Great Tree — restore to Mature";
        state.FindProperty("interpolateRootColour").boolValue = false;
        state.ApplyModifiedPropertiesWithoutUndo();
        var visual = target.gameObject.AddComponent<TreeAgeVisuals>();
        visual.target = target; visual.phases = phases; visual.transitionShader = shader;

        var keystone = new GameObject("ForestKeystone");
        var puzzle = keystone.AddComponent<PuzzleCondition>();
        var p = new SerializedObject(puzzle); p.FindProperty("puzzleId").stringValue = "forest_great_tree";
        var req = p.FindProperty("requirements"); req.arraySize = 1;
        req.GetArrayElementAtIndex(0).FindPropertyRelative("target").objectReferenceValue = target;
        req.GetArrayElementAtIndex(0).FindPropertyRelative("requiredStateIndex").intValue = 1;
        p.FindProperty("latchWhenSolved").boolValue = true; p.ApplyModifiedPropertiesWithoutUndo();
        var payoff = keystone.AddComponent<ForestKeystoneRestoration>(); payoff.puzzle = puzzle;
        var world = GameObject.Find("BT_World").transform;
        payoff.city = world.Find("Zone_02_City").gameObject;
        payoff.rootBridge = world.Find("Zone_01_Forest/GATE_RootBridge").gameObject;
        payoff.rootBridge.transform.position = new Vector3(0f, 0f, 180f);
        payoff.rootBridge.transform.localScale = new Vector3(9f, .40f, 24f);
        payoff.rootBridge.SetActive(false);
        var rootMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_EnvironmentArt/Materials/BT_GreatBarkDead_Tuned.mat");
        if (rootMaterial != null) payoff.rootBridge.GetComponent<Renderer>().sharedMaterial = rootMaterial;
        payoff.volume = UnityEngine.Object.FindFirstObjectByType<Volume>();
        payoff.sun = GameObject.Find("Directional Light").GetComponent<Light>();
        var legacyRestoration = UnityEngine.Object.FindFirstObjectByType<RestorationController>();
        if (legacyRestoration != null) legacyRestoration.enabled = false;
        var reserve = GameObject.Find("TimeReserve_Target"); if (reserve != null) reserve.SetActive(false);
        BuildHealthyDressing(dressing.transform, payoff);
        CutRavineAndAlignRamp(world);
        var hud = UnityEngine.Object.FindFirstObjectByType<HUDDisplay>();
        if (hud != null)
        {
            var h = new SerializedObject(hud); h.FindProperty("trackedPuzzle").objectReferenceValue = puzzle; h.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (label.text.Contains("RESTORE THE CROSSING")) label.text = "REVIVE THE GREAT TREE";
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        Debug.Log("Forest keystone wired: existing tree phases, 60-unit restoration, gradual visual transition, latched crossing and healthy dressing.");
    }

    private static void BuildHealthyDressing(Transform dressing, ForestKeystoneRestoration payoff)
    {
        var sick = dressing.Find("Trees_Dying"); var healthy = new GameObject("Trees_Restored"); healthy.transform.SetParent(dressing, false);
        var oldTrees = new List<GameObject>(); var newTrees = new List<GameObject>();
        foreach (Transform tree in sick)
        {
            var original = PrefabUtility.GetCorrespondingObjectFromSource(tree.gameObject);
            string path = original == null ? "" : AssetDatabase.GetAssetPath(original);
            string maturePath = path.Replace("_old.prefab", "_mature.prefab");
            var asset = maturePath == path ? null : AssetDatabase.LoadAssetAtPath<GameObject>(maturePath);
            if (asset == null) asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_EnvironmentArt/Prefabs/BT_broadleaf_mature.prefab");
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(asset, healthy.transform);
            replacement.transform.SetPositionAndRotation(tree.position, tree.rotation);
            replacement.transform.localScale = tree.localScale;
            replacement.SetActive(false); oldTrees.Add(tree.gameObject); newTrees.Add(replacement);
        }
        payoff.sickTrees = oldTrees.ToArray(); payoff.healthyTrees = newTrees.ToArray();
    }

    private static void CutRavineAndAlignRamp(Transform world)
    {
        var terrain = GameObject.Find("BT_ForestTerrain"); var filter = terrain.GetComponent<MeshFilter>();
        var copy = UnityEngine.Object.Instantiate(filter.sharedMesh); copy.name = "BT_ForestTerrain_Keystone";
        var vertices = copy.vertices; var indices = copy.triangles; var kept = new List<int>();
        for (int i = 0; i < indices.Length; i += 3)
        {
            float z = (terrain.transform.TransformPoint(vertices[indices[i]]).z + terrain.transform.TransformPoint(vertices[indices[i+1]]).z + terrain.transform.TransformPoint(vertices[indices[i+2]]).z) / 3f;
            if (z > 170f && z < 190f) continue;
            kept.Add(indices[i]); kept.Add(indices[i+1]); kept.Add(indices[i+2]);
        }
        copy.triangles = kept.ToArray(); copy.RecalculateBounds();
        string path = "Assets/03_EnvironmentArt/Models/BT_ForestTerrain_Keystone.asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) throw new InvalidOperationException("Keystone terrain copy already exists.");
        AssetDatabase.CreateAsset(copy, path); filter.sharedMesh = copy;
        var collider = terrain.GetComponent<MeshCollider>(); collider.sharedMesh = null; collider.sharedMesh = copy;
        var ramp = world.Find("Zone_01_Forest/LINK_ForestToCity");
        ramp.position = new Vector3(0f, 7.1f, 202f);
        ramp.rotation = Quaternion.Euler(-Mathf.Atan2(15f,20f) * Mathf.Rad2Deg,0f,0f);
        ramp.localScale = new Vector3(16f, 1f, 25f);
        // Keep the city floor from becoming a low ceiling over the ascending ramp.
        var cityFloor = world.Find("Zone_02_City/Ground_City");
        cityFloor.position = new Vector3(0f, 14.5f, 306f); cityFloor.localScale = new Vector3(220f,1f,188f);
    }
}
