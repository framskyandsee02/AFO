#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace OopsPlanet.Editor {
// Blender-authored visual skin. Original game objects, colliders and puzzle references stay in place.
public static class OopsSoftWorldArt {
    const string Root="Assets/OopsPlanet/Models/SoftWorld/";
    [Serializable] public class Row { public string key,name,role; public float[] pos,size,rot; }
    [Serializable] public class Manifest { public Row[] items; }
    static readonly Dictionary<string,string> Palette=new Dictionary<string,string> {
        {"Grass","79C762"},{"GrassLight","91D66C"},{"GrassDeep","5C9B88"},{"Cliff","79AEA2"},
        {"Cream","F9E8B9"},{"Path","E8CE98"},{"Water","61CFDC"},{"Teal","4C929B"},{"Mint","B4DBCD"},
        {"Coral","EA9E84"},{"Peach","F6C2A0"},{"Roof","65B8B0"},{"Yellow","FFD579"},{"Violet","B5A8DE"},
        {"Trunk","B58C6A"},{"Leaf","6ABD72"},{"LeafLight","ADDA80"},{"Flower","F2ACCF"},{"Cloud","FFF9E8"},
        {"Metal","98B8C5"},{"Dark","54848A"},{"White","FFFCED"}
    };
    static Color Hex(string value) { Color c; ColorUtility.TryParseHtmlString("#"+value,out c); return c; }
    static bool Boundary(string n) {
        return n.StartsWith("Hub wall ") || n.StartsWith("Factory wall ") || n.StartsWith("Rice wall ") ||
            n.StartsWith("Factory path wall ") || n.StartsWith("Rice path wall ") || n.StartsWith("Dike wall ") ||
            n.StartsWith("Conveyor wall ") || n.StartsWith("Lights shortcut wall ");
    }
    [MenuItem("Tools/OOPS Planet/V2/Apply Blender Soft World Art")]
    public static void ApplyAndSave() {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before applying map art");
        Apply(); var scene=EditorSceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }
    public static void Apply() {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before applying map art");
        var data=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"SoftWorld_manifest.json");
        var garden=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"SoftWorld_Garden.fbx");
        if(!data || !garden) return; // Greybox remains usable before art assets are present.
        var w=Object.FindFirstObjectByType<BlobWorld>(); if(!w || !w.ship) throw new InvalidOperationException("Open the V2 scene first");
        var manifest=JsonUtility.FromJson<Manifest>(data.text);
        var meshes=new Dictionary<string,Mesh>();
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(Root+"SoftWorld_Modules.fbx")) if(asset is Mesh mesh) { meshes[mesh.name]=mesh; meshes[mesh.name.Split('.')[0]]=mesh; }
        foreach(var row in manifest.items) if(string.IsNullOrEmpty(row.role)) throw new InvalidOperationException("Missing material role: "+row.name);
        if(!meshes.ContainsKey("SOFT_UNIT")) throw new InvalidOperationException("Blender rounded module meshes were not imported");
        string materialDir="Assets/OopsPlanet/Materials/SoftWorld";
        System.IO.Directory.CreateDirectory(materialDir);
        var materials=new Dictionary<string,Material>();
        foreach(var p in Palette) {
            string path=materialDir+"/SW_"+p.Key+".mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
            mat.SetColor("_BaseColor",Hex(p.Value)); mat.SetFloat("_Smoothness",p.Key=="Water"?.38f:.16f); mat.enableInstancing=true;
            materials[p.Key]=mat;
        }
        int replaced=0,hiddenGrid=0;
        foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
            var r=f.GetComponent<MeshRenderer>(); if(!r || f.GetComponentInParent<BlobPlayer>()) continue;
            string n=f.name;
            if(n.StartsWith("Grid ") || (r.sharedMaterial && r.sharedMaterial.name=="Grid lines")) { r.enabled=false; hiddenGrid++; continue; }
            if(n.StartsWith("Sign backing")) continue;
            Row match=null;
            foreach(var row in manifest.items) {
                if(row.name!=n) continue;
                if(Vector3.Distance(f.transform.position,new Vector3(row.pos[0],row.pos[1],row.pos[2]))<.025f) { match=row; break; }
            }
            if(match!=null && meshes.TryGetValue(match.key,out Mesh rounded)) {
                f.sharedMesh=rounded; if(materials.ContainsKey(match.role)) r.sharedMaterial=materials[match.role]; replaced++;
            } else if(f.sharedMesh && f.sharedMesh.name=="Cube" && f.GetComponentInParent<Rigidbody>()) {
                f.sharedMesh=meshes["SOFT_UNIT"]; replaced++;
            }
            if(Boundary(n) || n=="Vine step") r.enabled=false;
        }
        // Give the existing gated ramp a visible smooth skin; it stays hidden until the seed grows.
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
            if(t.name!="Vine stair walkable collision") continue;
            var box=t.GetComponent<BoxCollider>(); if(!box || !meshes.ContainsKey("SOFT_RAMP")) continue;
            var existing=t.Find("Soft vine ramp visual");
            if(!existing) {
                var visual=new GameObject("Soft vine ramp visual"); visual.transform.SetParent(t,false); visual.transform.localScale=box.size;
                visual.AddComponent<MeshFilter>().sharedMesh=meshes["SOFT_RAMP"];
                visual.AddComponent<MeshRenderer>().sharedMaterial=materials["LeafLight"];
            }
        }
        foreach(var tm in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
            if(!tm.name.StartsWith("Sign ")) continue;
            bool keep=tm.name.Contains("รีเซ็ต") || tm.name.Contains("RESET") || tm.name.StartsWith("Sign F") || tm.name.StartsWith("Sign R") || tm.name.StartsWith("Sign โรงงาน") || tm.name.StartsWith("Sign นาข้าว") || tm.name.Contains("จุดยานตก CRASH");
            tm.GetComponent<Renderer>().enabled=keep; tm.color=Hex("355A62"); tm.transform.localScale=Vector3.one*.65f;
            foreach(var f in tm.GetComponentsInChildren<MeshFilter>(true)) {
                f.sharedMesh=meshes["SOFT_UNIT"]; var r=f.GetComponent<MeshRenderer>(); if(r) { r.enabled=keep; r.sharedMaterial=materials["Cream"]; }
            }
        }
        var old=GameObject.Find("Soft World Garden"); if(old) Object.DestroyImmediate(old);
        var art=(GameObject)PrefabUtility.InstantiatePrefab(garden); art.name="Soft World Garden"; art.transform.position=Vector3.zero;
        foreach(var r in art.GetComponentsInChildren<MeshRenderer>(true)) {
            string role=r.name.StartsWith("DEC_")?r.name.Substring(4).Split('.')[0]:"Grass";
            r.sharedMaterial=materials.ContainsKey(role)?materials[role]:materials["Grass"];
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
        foreach(var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) { cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=Hex("B7E5F0"); }
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if(light.type==LightType.Directional) {
            if(light.name=="Directional Light") { light.color=Hex("FFF1D5"); light.intensity=1.25f; light.shadows=LightShadows.Soft; }
            else { light.color=Hex("BCDDEE"); light.intensity=.4f; }
        }
        RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=Hex("B7DBEB");
        RenderSettings.ambientEquatorColor=Hex("BBD5CD"); RenderSettings.ambientGroundColor=Hex("93B591");
        RenderSettings.fog=true; RenderSettings.fogColor=Hex("B7E5F0"); RenderSettings.fogStartDistance=85; RenderSettings.fogEndDistance=220;
        w.levelTitle="OOPS! PLANET";
        AssetDatabase.SaveAssets();
        Debug.Log("OOPS_SOFT_WORLD_APPLIED rounded="+replaced+" hiddenGrid="+hiddenGrid+" meshes="+meshes.Count);
    }
}
}
#endif
