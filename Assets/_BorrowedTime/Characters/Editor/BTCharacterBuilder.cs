#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace BorrowedTime.Characters.Editor {
[Serializable]public class CharacterMaterialData {public string name;public float[] color,emission;public float roughness,metallic,emissionStrength;}
[Serializable]public class CharacterMaterialList {public CharacterMaterialData[] items;}
public static class BTCharacterBuilder {
    public const string Root="Assets/_BorrowedTime/Characters";
    public const string ScenePath=Root+"/CharacterShowcase.unity";
    static BTAnimationCatalog Catalog(){return JsonUtility.FromJson<BTAnimationCatalog>(File.ReadAllText(Root+"/Models/AnimationCatalog.json"));}
    static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;int at=p.LastIndexOf('/');Folder(p.Substring(0,at));AssetDatabase.CreateFolder(p.Substring(0,at),p.Substring(at+1));}
    static Material Mat(string name,Color color,float smooth=0,float metal=0,float glow=0){
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);
        if(glow>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*glow);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}EditorUtility.SetDirty(m);return m;
    }
    static Dictionary<string,Material> Materials(){
        var data=JsonUtility.FromJson<CharacterMaterialList>("{\"items\":"+File.ReadAllText(Root+"/Models/Materials.json")+"}");var result=new Dictionary<string,Material>();
        foreach(var x in data.items){Color c=new Color(x.color[0],x.color[1],x.color[2],1).gamma;result[x.name]=Mat(x.name,c,1-x.roughness,x.metallic,x.emissionStrength);}return result;
    }
    static void Configure(string name,Dictionary<string,Material> mats){
        string path=Root+"/Models/"+name+".fbx";var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.importBlendShapes=true;importer.optimizeGameObjects=false;importer.isReadable=true;
        foreach(var pair in mats)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
        var clips=importer.defaultClipAnimations;var chosen=new List<ModelImporterClipAnimation>();string prefix=name=="Protagonist"?"Hero_":"Robo_";
        var specs=name=="Protagonist"?Catalog().hero:Catalog().robo;
        foreach(var c in clips){int at=c.name.LastIndexOf(prefix,StringComparison.Ordinal);if(at<0)continue;c.name=c.name.Substring(at+prefix.Length);var spec=specs.FirstOrDefault(s=>s.name==c.name);if(spec==null)continue;c.loopTime=spec.loop;c.loopPose=c.loopTime;chosen.Add(c);}
        if(chosen.Count>0)importer.clipAnimations=chosen.ToArray();importer.SaveAndReimport();
    }
    static Transform Bone(Transform root,string name){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
    static BTCharacterModel Prefab(string name,Dictionary<string,Material> mats){
        string path=Root+"/Models/"+name+".fbx";var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var go=new GameObject(name);var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(go.transform,false);
        var view=go.AddComponent<BTCharacterModel>();view.robot=name=="Robo";view.animator=model.GetComponent<Animator>();if(!view.animator)view.animator=model.AddComponent<Animator>();view.animator.applyRootMotion=false;view.face=model.GetComponentInChildren<SkinnedMeshRenderer>();view.rightHand=Bone(model.transform,"Hand.R");
        view.clips=view.robot?Catalog().robo:Catalog().hero;
        string controllerPath=Root+"/Animations/"+name+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();var machine=controller.layers[0].stateMachine;
        foreach(var clip in clips){var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==clip.name);if(!state)state=machine.AddState(clip.name);state.motion=clip;if(clip.name==(view.robot?"Hover":"Idle"))machine.defaultState=state;}
        if(!view.robot){
            if(!controller.parameters.Any(p=>p.name=="Speed"))controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            var idle=machine.states.Select(s=>s.state).First(s=>s.name=="Idle");var walk=machine.states.Select(s=>s.state).First(s=>s.name=="Walk");
            var toWalk=idle.transitions.FirstOrDefault(t=>t.destinationState==walk);if(!toWalk)toWalk=idle.AddTransition(walk);
            toWalk.hasExitTime=false;toWalk.duration=.18f;toWalk.conditions=new[]{new AnimatorCondition{mode=AnimatorConditionMode.Greater,parameter="Speed",threshold=.08f}};
            var toIdle=walk.transitions.FirstOrDefault(t=>t.destinationState==idle);if(!toIdle)toIdle=walk.AddTransition(idle);
            toIdle.hasExitTime=false;toIdle.duration=.18f;toIdle.conditions=new[]{new AnimatorCondition{mode=AnimatorConditionMode.Less,parameter="Speed",threshold=.08f}};
            var run=machine.states.Select(s=>s.state).First(s=>s.name=="Run");
            toWalk.conditions=new[]{new AnimatorCondition{mode=AnimatorConditionMode.Greater,parameter="Speed",threshold=.08f},new AnimatorCondition{mode=AnimatorConditionMode.Less,parameter="Speed",threshold=1.5f}};
            foreach(var state in new[]{idle,walk}){var transition=state.transitions.FirstOrDefault(t=>t.destinationState==run);if(!transition)transition=state.AddTransition(run);transition.hasExitTime=false;transition.duration=.18f;transition.conditions=new[]{new AnimatorCondition{mode=AnimatorConditionMode.Greater,parameter="Speed",threshold=1.5f}};}
            var runWalk=run.transitions.FirstOrDefault(t=>t.destinationState==walk);if(!runWalk)runWalk=run.AddTransition(walk);runWalk.hasExitTime=false;runWalk.duration=.18f;runWalk.conditions=toWalk.conditions;
            var runIdle=run.transitions.FirstOrDefault(t=>t.destinationState==idle);if(!runIdle)runIdle=run.AddTransition(idle);runIdle.hasExitTime=false;runIdle.duration=.18f;runIdle.conditions=toIdle.conditions;
        }
        view.animator.runtimeAnimatorController=controller;
        var initial=clips.FirstOrDefault(c=>c.name==(view.robot?"Hover":"Idle"));if(initial)initial.SampleAnimation(model,0);
        if(!view.robot){
            var articulation=go.AddComponent<BTCharacterArticulation>();articulation.model=view;
            var joints=new List<BTCharacterArticulation.Joint>();
            foreach(string side in new[]{"R","L"})foreach(string part in new[]{"UpperArm","LowerArm","UpperLeg","LowerLeg"}){
                string child=part=="UpperArm"?"LowerArm":part=="LowerArm"?"Hand":part=="UpperLeg"?"LowerLeg":"Foot";
                joints.Add(new BTCharacterArticulation.Joint{name=part+"."+side,bone=Bone(model.transform,part+"."+side),tip=Bone(model.transform,child+"."+side),min=part=="UpperArm"?-60:part=="UpperLeg"?-50:0,max=part=="UpperArm"?110:part=="UpperLeg"?100:135});
            }
            articulation.joints=joints.ToArray();articulation.CaptureRestPose();
        }
        if(!view.robot){var dial=new GameObject("Time Gauntlet hologram");dial.transform.SetParent(go.transform,false);var line=dial.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=97;line.widthMultiplier=.0035f;line.sharedMaterial=mats["Cyan"];for(int i=0;i<97;i++){float a=i*Mathf.PI*2/96;line.SetPosition(i,new Vector3(Mathf.Cos(a)*.14f,Mathf.Sin(a)*.14f,0));}line.enabled=false;view.chronoDial=line;}
        else view.SetFace("Happy");
        var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+name+".prefab");Object.DestroyImmediate(go);EditorUtility.SetDirty(controller);return prefab.GetComponent<BTCharacterModel>();
    }
    [MenuItem("Tools/Borrowed Time/Characters/Build character showcase")]
    public static void Build(){
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");foreach(var dir in new[]{"Materials","Prefabs","Animations","Evidence"})Folder(Root+"/"+dir);
        var mats=Materials();Configure("Protagonist",mats);Configure("Robo",mats);Prefab("Protagonist",mats);Prefab("Robo",mats);
        var old=SceneManager.GetActiveScene();
        if(string.IsNullOrEmpty(old.path) && old.isDirty)throw new Exception("Save the current untitled scene before building the character showcase.");
        bool replace=old.path==ScenePath || (string.IsNullOrEmpty(old.path) && !old.isDirty);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,replace?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var systems=new GameObject("Character preview controls").AddComponent<BTCharacterShowcase>();var turntable=new GameObject("Character turntable").transform;systems.turntable=turntable;
        systems.hero=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Protagonist.prefab"))).GetComponent<BTCharacterModel>();systems.hero.transform.SetParent(turntable,false);systems.hero.transform.localPosition=new Vector3(.3f,0,0);
        systems.articulation=systems.hero.GetComponent<BTCharacterArticulation>();
        systems.robo=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Robo.prefab"))).GetComponent<BTCharacterModel>();systems.robo.transform.SetParent(turntable,false);systems.robo.transform.localPosition=new Vector3(-.65f,1.15f,0);
        systems.font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/UI/NotoSansThai.ttf");
        var stage=GameObject.CreatePrimitive(PrimitiveType.Cylinder);stage.name="Character display plinth";stage.transform.position=new Vector3(0,-.0365f,0);stage.transform.localScale=new Vector3(4.4f,.05f,4.4f);stage.GetComponent<Renderer>().sharedMaterial=Mat("StudioSlate",new Color(.13f,.23f,.24f),.25f);
        var camera=new GameObject("Character Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(-2.2f,1.9f,4.6f);camera.transform.LookAt(new Vector3(-.1f,1,0));camera.orthographic=true;camera.orthographicSize=1.24f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.16f,.18f);systems.previewCamera=camera;
        foreach(var data in new[]{new Vector4(45,-30,0,1.25f),new Vector4(40,130,0,.7f)}){var light=new GameObject("Character studio light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(data.x,data.y,0);light.intensity=data.w;light.shadows=LightShadows.Soft;}
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.7f,.8f,.83f);RenderSettings.ambientEquatorColor=new Color(.5f,.65f,.65f);RenderSettings.ambientGroundColor=new Color(.25f,.34f,.34f);RenderSettings.fog=false;
        EditorSceneManager.SaveScene(scene,ScenePath);if(old.handle!=scene.handle && old.IsValid() && old.isLoaded && !old.isDirty)EditorSceneManager.CloseScene(old,true);AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(-.1f,1,0),Quaternion.Euler(8,155,0),2.3f);
        Debug.Log("BORROWED_TIME_CHARACTER_SHOWCASE_READY");
    }
    static Vector3 WorldSize(SkinnedMeshRenderer r,Mesh m){var v=m.vertices;var bounds=new Bounds(r.transform.TransformPoint(v[0]),Vector3.zero);foreach(var p in v)bounds.Encapsulate(r.transform.TransformPoint(p));return bounds.size;}
    [MenuItem("Tools/Borrowed Time/Characters/Validate models and clips")]
    public static string Validate(){
        var report=new System.Text.StringBuilder();
        foreach(string name in new[]{"Protagonist","Robo"}){
            string path=Root+"/Models/"+name+".fbx";var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));var r=go.GetComponentInChildren<SkinnedMeshRenderer>();var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
            int expected=name=="Robo"?Catalog().robo.Length:Catalog().hero.Length;
            if(!r || r.sharedMesh.blendShapeCount<8 || clips.Length!=expected)throw new Exception(name+" missing expected rig, expressions or clips: "+clips.Length+" / "+expected);
            int unweighted=0;foreach(var w in r.sharedMesh.boneWeights)if(w.weight0+w.weight1+w.weight2+w.weight3<.99f)unweighted++;
            if(unweighted>0)throw new Exception(name+" has unweighted vertices");
            var baked=new Mesh();foreach(var c in clips)foreach(float t in new[]{0f,.25f,.5f}){c.SampleAnimation(go,c.length*t);r.BakeMesh(baked,true);var size=WorldSize(r,baked);if(float.IsNaN(size.x)||size.x>3||size.y>3||size.z>3)throw new Exception(name+" exploded geometry in "+c.name+" size="+size);}
            for(int i=0;i<r.sharedMesh.blendShapeCount;i++){r.SetBlendShapeWeight(i,100);r.BakeMesh(baked,true);if(WorldSize(r,baked).y>3)throw new Exception("Expression geometry exceeded bounds");r.SetBlendShapeWeight(i,0);}
            report.AppendLine("PASS "+name+" vertices="+r.sharedMesh.vertexCount+" triangles="+r.sharedMesh.triangles.Length/3+" bones="+r.bones.Length+" expressions="+r.sharedMesh.blendShapeCount+" unweighted="+unweighted+" clips="+string.Join(",",clips.Select(c=>c.name).ToArray()));Object.DestroyImmediate(baked);Object.DestroyImmediate(go);
        }File.WriteAllText(Root+"/Evidence/CharacterValidation.txt",report.ToString());Debug.Log(report.ToString());return report.ToString();
    }
}
}
#endif
