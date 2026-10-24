using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// EDITOR TOOL — Builds the greybox puzzle setup for the vertical slice.
/// Safe to re-run: it removes its own previous output first.
/// Agent 03 replaces the greybox children with real Blender meshes later;
/// the TimeTarget state wiring survives that swap.
/// </summary>
public static class BorrowedTimeSceneSetup
{
    private const string TreeName = "Tree_Target";
    private const string BridgeName = "Bridge_Target";

    [MenuItem("Borrowed Time/Build Greybox Puzzle")]
    public static void BuildGreyboxPuzzle()
    {
        RemoveExisting(TreeName);
        RemoveExisting(BridgeName);

        // ---------- TREE: the root cause ----------
        var tree = new GameObject(TreeName);
        tree.transform.position = new Vector3(-4f, 0f, 6f);
        var treeTarget = tree.AddComponent<TimeTarget>();
        treeTarget.displayName = "Withered Tree";

        var tYoung = MakeStateBox(tree.transform, "State_0_Young",
            new Vector3(0f, 0.6f, 0f), new Vector3(0.4f, 1.2f, 0.4f), new Color(0.45f, 0.70f, 0.35f));
        var tMature = MakeStateBox(tree.transform, "State_1_Mature",
            new Vector3(0f, 1.6f, 0f), new Vector3(1.1f, 3.2f, 1.1f), new Color(0.25f, 0.60f, 0.25f));
        var tOld = MakeStateBox(tree.transform, "State_2_Old",
            new Vector3(0f, 1.2f, 0f), new Vector3(0.9f, 2.4f, 0.9f), new Color(0.42f, 0.35f, 0.26f));

        SetStates(treeTarget,
            new[] { "Young", "Mature", "Old" },
            new[] { tYoung, tMature, tOld },
            new[] { false, true, false },
            new[] { false, false, false });

        var treeSO = new SerializedObject(treeTarget);
        treeSO.FindProperty("startingStateIndex").intValue = 2;      // starts Old
        treeSO.FindProperty("energyCostPerChange").floatValue = 15f;
        treeSO.ApplyModifiedPropertiesWithoutUndo();

        // ---------- BRIDGE: gated by the tree ----------
        var bridge = new GameObject(BridgeName);
        bridge.transform.position = new Vector3(3f, 0f, 10f);
        var bridgeTarget = bridge.AddComponent<TimeTarget>();
        bridgeTarget.displayName = "Collapsed Bridge";

        var bIntact = MakeStateBox(bridge.transform, "State_0_Intact",
            new Vector3(0f, 0.15f, 0f), new Vector3(2.0f, 0.3f, 6.0f), new Color(0.62f, 0.48f, 0.32f));
        var bWorn = MakeStateBox(bridge.transform, "State_1_Worn",
            new Vector3(0f, 0.12f, 0f), new Vector3(1.8f, 0.24f, 5.2f), new Color(0.50f, 0.42f, 0.32f));
        var bBroken = MakeStateBox(bridge.transform, "State_2_Broken",
            new Vector3(0f, 0.10f, -1.6f), new Vector3(1.7f, 0.20f, 2.2f), new Color(0.34f, 0.30f, 0.26f));

        SetStates(bridgeTarget,
            new[] { "Intact", "Worn", "Broken" },
            new[] { bIntact, bWorn, bBroken },
            new[] { true, false, false },
            new[] { true, true, false });

        var bridgeSO = new SerializedObject(bridgeTarget);
        bridgeSO.FindProperty("startingStateIndex").intValue = 2;     // starts Broken
        bridgeSO.FindProperty("energyCostPerChange").floatValue = 20f;
        bridgeSO.FindProperty("prerequisite").objectReferenceValue = treeTarget;
        bridgeSO.FindProperty("prerequisiteRequiredState").intValue = 1;   // tree must be Mature
        bridgeSO.FindProperty("gatedHint").stringValue =
            "The bridge's timber came from that tree. Restore the tree first.";
        bridgeSO.ApplyModifiedPropertiesWithoutUndo();

        // ---------- WIRE THE PUZZLE ----------
        var puzzleGO = GameObject.Find("EnvironmentPuzzle");
        if (puzzleGO != null)
        {
            var puzzle = puzzleGO.GetComponent<PuzzleCondition>();
            if (puzzle != null)
            {
                var pSO = new SerializedObject(puzzle);
                pSO.FindProperty("puzzleId").stringValue = "puzzle_01_bridge";
                var reqs = pSO.FindProperty("requirements");
                reqs.arraySize = 2;
                var r0 = reqs.GetArrayElementAtIndex(0);
                r0.FindPropertyRelative("target").objectReferenceValue = treeTarget;
                r0.FindPropertyRelative("requiredStateIndex").intValue = 1;   // Mature
                var r1 = reqs.GetArrayElementAtIndex(1);
                r1.FindPropertyRelative("target").objectReferenceValue = bridgeTarget;
                r1.FindPropertyRelative("requiredStateIndex").intValue = 0;   // Intact
                pSO.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[BT Setup] EnvironmentPuzzle has no PuzzleCondition.");
        }
        else Debug.LogWarning("[BT Setup] EnvironmentPuzzle GameObject not found.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[BT Setup] Greybox puzzle built: Tree(Old) gates Bridge(Broken). " +
                  "Solve = Tree→Mature, then Bridge→Intact.");
    }

    private static void RemoveExisting(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);
    }

    private static GameObject MakeStateBox(
        Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    private static void SetStates(
        TimeTarget target, string[] names, GameObject[] roots, bool[] healthy, bool[] traversable)
    {
        var so = new SerializedObject(target);
        var arr = so.FindProperty("states");
        arr.arraySize = names.Length;

        for (int i = 0; i < names.Length; i++)
        {
            var el = arr.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("stateName").stringValue = names[i];
            el.FindPropertyRelative("visualRoot").objectReferenceValue = roots[i];
            el.FindPropertyRelative("isHealthy").boolValue = healthy[i];
            el.FindPropertyRelative("isTraversable").boolValue = traversable[i];
            el.FindPropertyRelative("stateCollider").objectReferenceValue = roots[i].GetComponent<Collider>();
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
