#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using WobbleWorks.Characters;
using Object=UnityEngine.Object;
namespace OopsPlanet.Editor {
// Presentation pass only: keep gameplay roots, colliders, trigger volumes and puzzle references.
public static class WorldPolishArt {
    public const int Revision=3;
    const string Base="Assets/Scenes/OopsPlanet/";
    const string Models=Base+"Models/WorldPolish/";
    static readonly Dictionary<string,string> Colors=new Dictionary<string,string>{
        {"Cream","FFF0CE"},{"White","FDF8ED"},{"Teal","579B9C"},{"Dark","294A60"},{"Metal","9BB9BE"},{"Coral","EB997B"},{"Blue","78B4D4"},{"Pink","E6A2BB"},{"Water","50C8CE"},{"Yellow","F5D163"},{"Orange","EE9251"},{"Leaf","83AA78"},{"Grass","A5C97E"},{"Path","DCC7A5"},{"Coal","48515F"},{"DryLeaf","BDC57B"}};
    static Dictionary<string,Material> materials;
    static Color Hex(string h){Color c;ColorUtility.TryParseHtmlString("#"+h,out c);return c;}
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');Folder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
    static void MakeMaterials(){
        materials=new Dictionary<string,Material>();Folder(Base+"Materials/WorldPolish");
        foreach(var p in Colors){string path=Base+"Materials/WorldPolish/POL_"+p.Key+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",Hex(p.Value));m.SetFloat("_Smoothness",p.Key=="Metal"?.56f:.32f);m.SetFloat("_Metallic",p.Key=="Metal"?.28f:0);m.enableInstancing=true;
            if(p.Key=="Orange"){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Hex(p.Value)*.3f);}
            EditorUtility.SetDirty(m);materials[p.Key]=m;
        }
    }
    static string Role(Material m){if(!m)return "";string n=m.name;int at=n.IndexOf("POL_");return at>=0?n.Substring(at+4).Split('.')[0]:"";}
    static void Dress(GameObject g,string fallback="Metal"){
        foreach(var r in g.GetComponentsInChildren<Renderer>(true)){
            var a=r.sharedMaterials;for(int i=0;i<a.Length;i++){string role=Role(a[i]);a[i]=materials.ContainsKey(role)?materials[role]:materials[fallback];}r.sharedMaterials=a;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
    }
    static Bounds Bounds(GameObject g){var rr=g.GetComponentsInChildren<Renderer>(true);Bounds b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
    static Vector3 Divide(Vector3 a,Vector3 b)=>new Vector3(a.x/Mathf.Max(.0001f,b.x),a.y/Mathf.Max(.0001f,b.y),a.z/Mathf.Max(.0001f,b.z));
    static GameObject Art(Transform parent,string name,string asset,Vector3 point,float height,bool centered=false,string fallback="Metal"){
        var old=parent.Find(name);if(old)Object.DestroyImmediate(old.gameObject);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset.Contains('/')?asset:Models+asset+".fbx");if(!prefab)throw new InvalidOperationException("Missing art model: "+asset);
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent.gameObject.scene);obj.name=name;
        Vector3 importedScale=obj.transform.localScale;obj.transform.SetParent(parent,false);obj.transform.localScale=Divide(importedScale,parent.lossyScale);obj.transform.localRotation=Quaternion.identity;Dress(obj,fallback);
        var b=Bounds(obj);obj.transform.localScale*=height/Mathf.Max(.001f,b.size.y);b=Bounds(obj);
        obj.transform.position+=point-(centered?b.center:new Vector3(b.center.x,b.min.y,b.center.z));
        foreach(var t in obj.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=2;PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);return obj;
    }
    static void Remember(Renderer r){if(r)PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
    static void Machine(GameObject g,string asset,float height=-1,string fallback="Metal"){
        if(!g)return;var c=g.GetComponent<Collider>();var r=g.GetComponent<Renderer>();if(!c&&!r)return;
        var b=c?c.bounds:r.bounds;var obj=Art(g.transform,"World polish machine",asset,new Vector3(b.center.x,b.min.y,b.center.z),height>0?height:b.size.y,false,fallback);
        if(r){r.enabled=false;Remember(r);}var m=g.GetComponent<WorldMaterial>();if(m){m.visuals=obj.GetComponentsInChildren<Renderer>(true);EditorUtility.SetDirty(m);}
    }
    static void Source(BlobInteractable s){
        // Keep the movable scrap pickup beside R1, outside the southward straw discharge lane.
        if(s.name=="Rice west Magnet source (scrap)")s.transform.position=new Vector3(37,0,12);
        float h=s.property==BlobProperty.Pressure?1.3f:s.property==BlobProperty.Water?1.1f:s.property==BlobProperty.Electric?.9f:1f;
        if(s.property==BlobProperty.Bounce)h=.75f;else if(s.property==BlobProperty.Magnet)h=.78f;else if(s.property==BlobProperty.Heat)h=.95f;
        var art=Art(s.transform,"World polish source","Source_"+s.property,s.transform.position,h);
        s.highlightVisual=art.transform;EditorUtility.SetDirty(s);
        // The hidden original renderer still defines the exact original aim point and collider transforms.
        if(s.fullVisual)foreach(var r in s.fullVisual.GetComponentsInChildren<Renderer>(true)){r.enabled=false;Remember(r);}
        var visuals=art.GetComponentsInChildren<Renderer>(true);var fx=s.GetComponent<AbsorbableVisual>();if(fx){fx.renderers=visuals;EditorUtility.SetDirty(fx);}
        var m=s.GetComponent<WorldMaterial>();if(m){m.visuals=visuals;EditorUtility.SetDirty(m);}
    }
    public static void InstallPino(BlobPlayer p){
        var old=p.transform.Find("PinoVisual");GameObject obj;
        if(old)obj=old.gameObject;
        else{var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PinoCharacter/Prefabs/PinoCharacter.prefab");if(!prefab)throw new InvalidOperationException("Pino prefab missing");obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,p.gameObject.scene);obj.name="PinoVisual";obj.transform.SetParent(p.transform,false);}
        var body=obj.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r=>r.name.Contains("Pino.Body"));
        var mesh=new Mesh();body.BakeMesh(mesh);float min=float.MaxValue,max=float.MinValue;foreach(var v in mesh.vertices){float y=body.transform.TransformPoint(v).y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);}Object.DestroyImmediate(mesh);
        obj.transform.localScale*=1.2f/Mathf.Max(.01f,max-min);
        var adapter=obj.GetComponent<PinoGameplayAdapter>();if(!adapter)adapter=obj.AddComponent<PinoGameplayAdapter>();adapter.Configure(p,obj.GetComponent<PinoMorphController>(),p.visual);adapter.SyncNow(true,0);
        foreach(var t in obj.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=2;PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
        foreach(var r in p.visual.GetComponentsInChildren<Renderer>(true)){r.enabled=false;Remember(r);}
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(adapter);EditorUtility.SetDirty(adapter);
    }
    static void Label(Transform root,string name,string text,Vector3 pos,float yaw,Font font){
        var g=new GameObject(name);g.transform.SetParent(root,false);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);
        var t=g.AddComponent<TextMesh>();t.text=text;t.font=font;t.fontSize=96;t.characterSize=.09f;t.anchor=TextAnchor.MiddleCenter;t.color=Hex("294A60");t.GetComponent<Renderer>().sharedMaterial=font.material;
    }
    [MenuItem("Tools/OOPS Planet/V2/Apply Full World Polish")]
    public static void ApplyAndSave(){Apply();var s=EditorSceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();}
    public static void Apply(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before changing map art");
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(Models+"Source_Water.fbx"))return;
        var w=Object.FindFirstObjectByType<BlobWorld>();if(!w||!w.ship)return;MakeMaterials();
        foreach(var s in w.sources)if(s)Source(s);
        foreach(var m in Object.FindObjectsByType<WorldMaterial>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            if(m.gameObject.scene!=w.gameObject.scene)continue;string n=m.name;
            if(n.Contains("irrigation pump (machine)")||n.Contains("gate motor (machine)")||n.Contains("turbine motor (machine")||n.Contains("cargo lift motor (machine)"))Machine(m.gameObject,"Machine_Pump");
            else if(n=="R3 steam pump feed tank")Machine(m.gameObject,"Machine_Boiler",1.15f);
            else if(n=="R3 rice plot")Machine(m.gameObject,"Rice_Plot",.66f);
            else if(n=="Straw bundle (2 kg)")Machine(m.gameObject,Base+"Models/PZ_StrawBundle.fbx",-1,"Yellow");
        }
        Machine(GameObject.Find("Old machine (30 kg, magnetic)"),"Machine_Pump");
        Machine(GameObject.Find("Counterweight (6 kg, magnetic)"),Base+"Models/PZ_Counterweight.fbx");
        foreach(var t in Object.FindObjectsByType<PressureTurbine>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            if(t.gameObject.scene!=w.gameObject.scene)continue;Art(t.transform,"World polish turbine frame","Machine_Turbine",t.transform.position,3.1f,true);
        }
        foreach(var part in w.ship.parts)if(part){var old=part.transform.Find("Part visual");if(!old)continue;var partArt=Art(part.transform,"World polish ship part",Base+"Models/PROP_Gear.fbx",old.position,.55f,true,"Yellow");part.visual=partArt.transform;EditorUtility.SetDirty(part);foreach(var r in old.GetComponentsInChildren<Renderer>(true)){r.enabled=false;Remember(r);}}
        var hull=w.ship.liftOff;
        if(hull){foreach(var r in hull.GetComponentsInChildren<Renderer>(true))if(!w.ship.stageObjects.Any(g=>g && r.transform.IsChildOf(g.transform))) {r.enabled=false;Remember(r);}Art(hull,"World polish ship","Ship_Capsule",hull.position+Vector3.up*.05f,3.45f);}
        var oldWorld=GameObject.Find("World polish accents");if(oldWorld)Object.DestroyImmediate(oldWorld);
        var accents=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models+"World_Accents.fbx"),w.gameObject.scene);accents.name="World polish accents";Dress(accents);
        Label(accents.transform,"Workshop entrance title","โรงงาน",new Vector3(0,5.5f,28.87f),0,w.uiFont);
        Label(accents.transform,"Rice entrance title","นาข้าว",new Vector3(30.45f,4.8f,0),90,w.uiFont);
        // Quieter terrain leaves contrast for the player, absorbables and active devices.
        foreach(var p in new Dictionary<string,string>{{"Grass","A5C97E"},{"GrassLight","C5D9A2"},{"GrassDeep","7AA99A"},{"Leaf","83B28C"},{"LeafLight","B5CEA0"},{"Path","DCC7A5"},{"Cliff","A9C7BC"},{"Mint","BCE0E1"}}){var m=AssetDatabase.LoadAssetAtPath<Material>(Base+"Materials/SoftWorld/SW_"+p.Key+".mat");if(m){m.SetColor("_BaseColor",Hex(p.Value));EditorUtility.SetDirty(m);}}
        InstallPino(w.player);UnorderedComboUpgrade.ApplyToScene();
        Debug.Log("WORLD_POLISH_APPLIED sources="+w.sources.Length+" player=Pino");
    }
}
}
#endif
