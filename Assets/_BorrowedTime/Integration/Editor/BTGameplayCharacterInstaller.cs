#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace BorrowedTime.Characters.Editor {
[InitializeOnLoad]
public static class BTGameplayCharacterInstaller {
    public static readonly string[] Scenes={BTJourney.TrainingPath,BTJourney.CityPath,BTJourney.FactoryPath};
    const string OpenRequest="Library/BTMapCharacters_OpenTraining.request";
    static BTGameplayCharacterInstaller(){
        EditorSceneManager.sceneSaving+=OnSaving;
        if(File.Exists(OpenRequest))EditorApplication.update+=OpenRequestedTraining;
    }
    static void OpenRequestedTraining(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        // The request is written only after the user's explicit authorization
        // to leave Play Mode and apply the verified character integration.
        if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        EditorApplication.update-=OpenRequestedTraining;File.Delete(OpenRequest);
        if(SceneManager.GetActiveScene().isDirty){File.WriteAllText("Library/BTMapCharacters_EditorResult.txt","Characters installed; unsaved current scene retained. Open TrainingArea when ready.");return;}
        var scene=EditorSceneManager.OpenScene(BTJourney.TrainingPath);var player=Object.FindFirstObjectByType<BTPlayer>();
        if(player){Selection.activeGameObject=player.gameObject;if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(player.transform.position+Vector3.up,Quaternion.Euler(18,160,0),4);}
        File.WriteAllText("Library/BTMapCharacters_EditorResult.txt","PASS stopped authorized Play Mode and opened "+scene.path);
    }
    static void OnSaving(Scene scene,string path){
        if(!EditorApplication.isPlaying && Scenes.Contains(path))Install(scene);
    }
    static void Layer(Transform root,int layer){foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;}
    static BTCharacterModel Create(string name,Transform parent){
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(BTCharacterBuilder.Root+"/Prefabs/"+name+".prefab");
        if(!source)throw new Exception("Missing character prefab: "+name);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;
        var model=go.GetComponent<BTCharacterModel>();model.animator.applyRootMotion=false;
        var clip=AssetDatabase.LoadAllAssetsAtPath(BTCharacterBuilder.Root+"/Models/"+name+".fbx").OfType<AnimationClip>().First(c=>c.name==(name=="Robo"?"Hover":"Idle"));
        clip.SampleAnimation(model.animator.gameObject,0);Layer(go.transform,2);return model;
    }
    static Bounds Bounds(BTCharacterModel model){
        var mesh=new Mesh();model.face.BakeMesh(mesh,true);var points=mesh.vertices;
        var b=new Bounds(model.transform.InverseTransformPoint(model.face.transform.TransformPoint(points[0])),Vector3.zero);
        foreach(var point in points)b.Encapsulate(model.transform.InverseTransformPoint(model.face.transform.TransformPoint(point)));
        Object.DestroyImmediate(mesh);return b;
    }
    public static BTGameplayCharacter Install(Scene scene){
        // Unity's default Cylinder uses a CapsuleCollider. A wide, flattened
        // arrival disc expands that capsule into a tall invisible dome, pushing
        // the explorer metres above the visible floor. Match this one platform
        // to its existing render mesh; preserve its art and transform.
        if(scene.path==BTJourney.TrainingPath){
            var arrival=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="Arrival circle");
            if(arrival&&arrival.GetComponent<CapsuleCollider>()){
                var mesh=arrival.GetComponent<MeshFilter>();if(mesh&&mesh.sharedMesh){Object.DestroyImmediate(arrival.GetComponent<CapsuleCollider>());arrival.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;}
            }
        }
        var players=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BTPlayer>(true)).ToArray();
        if(players.Length!=1)throw new Exception(scene.name+" expected one BTPlayer, got "+players.Length);
        var player=players[0];var existing=player.GetComponent<BTGameplayCharacter>();if(existing&&existing.hero&&existing.robo)return existing;
        if(!player.game||!player.game.robo||!player.visual)throw new Exception("Player/companion references incomplete");
        var adapter=existing?existing:player.gameObject.AddComponent<BTGameplayCharacter>();adapter.player=player;adapter.legacyVisual=player.visual;
        var visual=new GameObject("Explorer animated visual").transform;visual.SetParent(player.transform,false);visual.localRotation=player.visual.localRotation;
        adapter.hero=Create("Protagonist",visual);var hero=adapter.hero;var bounds=Bounds(hero);
        var feet=hero.animator.GetComponentsInChildren<Transform>().Where(t=>t.name=="Foot.L"||t.name=="Foot.R").ToArray();
        Vector3 center=Vector3.zero;foreach(var foot in feet)center+=hero.transform.InverseTransformPoint(foot.position)/feet.Length;
        var controller=player.GetComponent<CharacterController>();float sole=controller.center.y-controller.height*.5f;
        // Remove the Blender studio's horizontal offset without moving the
        // rotation pivot away from the existing CharacterController.
        hero.transform.localPosition=new Vector3(-center.x,sole-bounds.min.y,-center.z);
        player.visual.gameObject.SetActive(false);player.visual=visual;player.rightHand=hero.rightHand;player.leftFoot=player.rightFoot=null;
        var robotRoot=player.game.robo;
        foreach(Transform child in robotRoot)child.gameObject.SetActive(false);
        adapter.robo=Create("Robo",robotRoot);adapter.robo.transform.localPosition=-Bounds(adapter.robo).center;adapter.robo.currentPose="Hover";adapter.robo.SetFace("Happy");
        foreach(var model in new[]{adapter.hero,adapter.robo}){
            model.autoReturn=false;model.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);PrefabUtility.RecordPrefabInstancePropertyModifications(model.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(model.animator);
            foreach(var t in model.GetComponentsInChildren<Transform>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
        }
        EditorUtility.SetDirty(player);EditorUtility.SetDirty(adapter);return adapter;
    }
    [MenuItem("Tools/Borrowed Time/Characters/Use characters in the three maps")]
    public static void ApplyAll(){
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
        var previous=SceneManager.GetActiveScene();if(previous.isDirty)throw new Exception("Save current scene before applying characters");
        foreach(string path in Scenes){var scene=EditorSceneManager.OpenScene(path);Install(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        if(!string.IsNullOrEmpty(previous.path))EditorSceneManager.OpenScene(previous.path);
        AssetDatabase.SaveAssets();Debug.Log("BORROWED_TIME_CHARACTERS_IN_THREE_MAPS");
    }
}
}
#endif
