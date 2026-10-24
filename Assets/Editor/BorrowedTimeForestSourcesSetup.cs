using System;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit, idempotent conversion of Claude's existing six deadwood meshes.</summary>
public static class BorrowedTimeForestSourcesSetup
{
    [MenuItem("Borrowed Time/Handoff/Wire existing forest deadwood sources")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before wiring scene assets.");
        var group = GameObject.Find("BT_ForestDressing/TimeSource_Deadwood");
        if (group == null) throw new InvalidOperationException("Claude's deadwood group is missing.");
        var children = new Transform[group.transform.childCount];
        for (int i = 0; i < children.Length; i++) children[i] = group.transform.GetChild(i);
        int created = 0;
        foreach (var existing in children)
        {
            if (existing.GetComponent<TimeTarget>() != null) continue;
            if (existing.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
            var root = new GameObject("BT_Deadwood_Source_" + (created + 1).ToString("00"));
            Undo.RegisterCreatedObjectUndo(root, "Wire forest time source");
            root.transform.SetParent(group.transform, false);
            root.transform.position = existing.position;
            var fresh = new GameObject("State_0_Timber"); fresh.transform.SetParent(root.transform, false);
            Undo.SetTransformParent(existing, fresh.transform, "Preserve deadwood mesh");
            var spent = new GameObject("State_1_Crumbling"); spent.transform.SetParent(root.transform, false);
            UnityEngine.Object.Instantiate(existing.gameObject, spent.transform, false);
            spent.transform.localScale = new Vector3(0.85f, 0.15f, 0.85f);
            var a = AddMeshColliders(fresh); var b = AddMeshColliders(spent);
            var target = Undo.AddComponent<TimeTarget>(root);
            var serialized = new SerializedObject(target);
            var states = serialized.FindProperty("states"); states.arraySize = 2;
            SetState(states.GetArrayElementAtIndex(0), "Timber", fresh, a);
            SetState(states.GetArrayElementAtIndex(1), "Crumbling", spent, b);
            serialized.FindProperty("startingStateIndex").intValue = 0;
            serialized.FindProperty("displayName").stringValue = "Fallen deadwood — hold E to collect time";
            serialized.FindProperty("energyCostPerChange").floatValue = 25f;
            serialized.FindProperty("interpolateRootColour").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            spent.SetActive(false); created++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(group.scene);
        Debug.Log("Forest deadwood sources wired: " + created + ". Existing source meshes/materials preserved.");
    }
    private static Collider AddMeshColliders(GameObject root)
    {
        Collider first = null;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            var collider = filter.GetComponent<MeshCollider>();
            if (collider == null) collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            if (first == null) first = collider;
        }
        return first;
    }
    private static void SetState(SerializedProperty state, string name, GameObject visual, Collider collider)
    {
        state.FindPropertyRelative("stateName").stringValue = name;
        state.FindPropertyRelative("visualRoot").objectReferenceValue = visual;
        state.FindPropertyRelative("stateCollider").objectReferenceValue = collider;
        state.FindPropertyRelative("isHealthy").boolValue = false;
        state.FindPropertyRelative("isTraversable").boolValue = false;
    }
}
