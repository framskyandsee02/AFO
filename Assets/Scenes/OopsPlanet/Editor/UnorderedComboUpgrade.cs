#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace OopsPlanet.Editor {
public static class UnorderedComboUpgrade {
    [MenuItem("Tools/OOPS Planet/V2/Apply 15 Unordered Combos")]
    public static void Apply() {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Stop Play Mode first");
        var table=AssetDatabase.LoadAssetAtPath<ComboTable>("Assets/Scenes/OopsPlanet/Data/ComboTable_V2.asset");
        if(!table) throw new System.InvalidOperationException("Current combo table is missing");
        table.cells=ComboTableDefaults.Create();EditorUtility.SetDirty(table);AssetDatabase.SaveAssetIfDirty(table);
        ApplyToScene();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("UNORDERED_COMBOS_APPLIED pairs="+table.cells.Count);
    }
    public static void ApplyToScene() {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var shed=GameObject.Find("Storage shed (wooden walls)");
        if(shed) {
            var m=shed.GetComponent<WorldMaterial>();if(m){m.kind=MaterialKind.Metal;EditorUtility.SetDirty(m);}
            var beam=GameObject.Find("F3 steel beam (8 m)");
            if(beam) shed.GetComponent<Renderer>().sharedMaterial=beam.GetComponent<Renderer>().sharedMaterial;
            var anchor=shed.transform.Find("Shed roof magnetic anchor");
            if(!anchor){anchor=new GameObject("Shed roof magnetic anchor").transform;anchor.SetParent(shed.transform);anchor.gameObject.AddComponent<MagnetAnchor>();}
            anchor.position=new Vector3(-13.9f,6.3f,33.5f);
        }
        foreach(var p in UnityEngine.Object.FindObjectsByType<PuzzleObjective>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
            if(p.gameObject.scene!=scene)continue;
            if(p.id=="f2_boiler")p.hintTh="ขึ้นทางเดินสูง: ใช้ไอน้ำ ลิฟต์ หรือสลิงแม่เหล็ก";
            if(p.id=="r2_canal")p.hintTh="ข้ามคลอง: รดเมล็ดให้เป็นสะพาน ใช้สลิง หรือสายน้ำแม่เหล็กลากแพ";
            EditorUtility.SetDirty(p);
        }
        foreach(var l in UnityEngine.Object.FindObjectsByType<ComboLock>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
            if(l.gameObject.scene!=scene)continue;
            for(int i=0;i<l.acceptedCombos.Length;i++)l.acceptedCombos[i]=ComboTable.NormalizeId(l.acceptedCombos[i]);
            EditorUtility.SetDirty(l);
        }
    }
}
}
#endif
