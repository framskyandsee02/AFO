#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BorrowedTime.Editor {
[InitializeOnLoad]
public static class BTFactoryCampaignTraining {
    const string Command="Library/BTFactoryCampaignTraining.command", Result="Library/BTFactoryCampaignTraining.result.json";
    static readonly string Output=Path.GetFullPath("Captures/TrainingComfort");
    static bool waiting,testing,oldBackground,flowChecked,occlusionChecked;
    static BTTrainingGame game;
    static int step,mode;
    static float since,nextAction;
    static double started;
    static readonly List<string> log=new List<string>();
    static Queue<Vector3> route;
    static Vector3 routeGoal;
    static BTFactoryCampaignTraining(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=StateChanged;}
    static void Write(string status,string detail){File.WriteAllText(Result,JsonUtility.ToJson(new Report{status=status,detail=detail,playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,paused=EditorApplication.isPaused,time=Time.time,timeScale=Time.timeScale,position=game&&game.player?game.player.transform.position:Vector3.zero},true));}
    [Serializable]class Report{public string status,detail;public bool playing,compiling,paused;public float time,timeScale;public Vector3 position;}
    [MenuItem("Tools/Borrowed Time/Factory/Test/Apply gentle exploration tutorial")]
    public static void Apply(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode before editing the scene");
        var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Current scene has unsaved changes; retained without overwriting");
        if(scene.path!=BTJourney.TrainingPath)scene=EditorSceneManager.OpenScene(BTJourney.TrainingPath);
        var g=Object.FindFirstObjectByType<BTTrainingGame>();if(!g||!g.IsGentleTraining)throw new Exception("Not the TrainingArea tutorial");
        g.ConfigureGentleTraining();g.bridge.age=g.bridge.initialAge=1;g.bridge.Apply();
        g.mechanism.age=g.mechanism.initialAge=0;g.mechanism.Apply();g.seal.age=g.seal.initialAge=2;g.seal.Apply();
        g.mechanism.displayName="นาฬิกาประดับลานพัก";g.seal.displayName="เถาวัลย์เก่าข้างทาง";
        // Preserve the decorations, but remove extra targets that suggest a
        // mandatory combined lock at the tutorial's final viewpoint.
        g.mechanism.enabled=false;g.seal.enabled=false;
        foreach(var t in new[]{g.mechanism,g.seal}){if(t.focusRing)t.focusRing.SetActive(false);if(t.aimPoint)t.aimPoint.gameObject.SetActive(false);}
        var world=g.GetComponent<BTExpansionWorld>();
        if(!world){EditorUtility.SetDirty(g);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Write("APPLIED","Gentle tutorial with decorative forest retained");return;}
        world.ConfigureGentleTraining();world.Refresh();
        for(int i=0;i<2;i++)if(world.puzzles[i].gate)world.puzzles[i].gate.localPosition=world.puzzles[i].gateClosed+Vector3.up*4;
        var flow=g.player.GetComponent<BTLifeFlow>();if(!flow)flow=g.player.gameObject.AddComponent<BTLifeFlow>();
        const string materialPath="Assets/_BorrowedTime/Materials/LifeFlow.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material){
            material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.name="LifeFlow";
            material.SetColor("_BaseColor",new Color(.45f,1,.62f,.9f));material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            AssetDatabase.CreateAsset(material,materialPath);
        }
        flow.flowMaterial=material;EditorUtility.SetDirty(flow);
        // The old south fence crossed the new garden connection. Make a real
        // 4.4 m opening instead of requiring players to find a route around it.
        foreach(var t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray()){
            if(Mathf.Abs(t.position.z+16)>.15f)continue;
            if((t.name=="Fence post"||t.name=="Post cap")&&t.position.x>-18.2f&&t.position.x< -13.8f)t.gameObject.SetActive(false);
            if(t.name!="Fence upper rail"&&t.name!="Fence lower rail")continue;
            var renderer=t.GetComponent<Renderer>();if(!renderer||renderer.bounds.size.x<12)continue;
            float height=t.position.y;var scale=t.localScale;
            t.position=new Vector3(-20.1f,height,-16);t.localScale=new Vector3(scale.x,scale.y,3.8f);
            var right=Object.Instantiate(t.gameObject,t.parent).transform;right.name="Garden entrance rail";right.position=new Vector3(-10.9f,height,-16);right.localScale=new Vector3(scale.x,scale.y,5.8f);
        }
        foreach(var text in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TextMesh>(true))){
            if(!text.transform.IsChildOf(world.transform) && text.name!="Etched location sign")continue;
            Vector3 at=text.transform.position;
            for(int i=0;i<world.puzzles.Length;i++){
                var p=world.puzzles[i];Vector3 center=p.approach+Vector3.forward*6;
                if(Vector3.Distance(at,center+new Vector3(0,1.22f,3))<2.5f){
                    text.text=p.title+"\n"+new[]{"ล้อเมาส์ไปหน้า • ปลุกต้นกล้า","F ที่แท่นกลาง • เปิดน้ำ","F ที่ระฆังใดก็ได้ • ฟังเสียงสวน"}[i]+"\nเลือกเล่นได้ ไม่บังคับ";
                    // Move the complete supported sign beside the arch so it
                    // does not fill the following camera at the reward gate.
                    Vector3 offset=new Vector3(7,0,-1.5f),anchor=center+Vector3.forward*3;
                    foreach(var support in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
                        if((support.name=="Sign stone footing"||support.name=="Sign support post")&&Mathf.Abs(support.position.x-anchor.x)<.1f&&Mathf.Abs(support.position.z-anchor.z)<.1f)support.position+=offset;
                    text.transform.parent.position+=offset;
                }
            }
            if(text.text.Contains("ระบบน้ำ • ระบบแสง • สวนลับ"))text.text="สวนทดลองข้างทาง\nแวะชมได้ตามใจ • ไม่บังคับ";
        }
        foreach(var t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
            if(t.name.Contains("circuit conduit")||t.name.Contains("linked copper conduit"))t.gameObject.SetActive(false);
        EditorUtility.SetDirty(g);EditorUtility.SetDirty(world);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        if(world.RequiredCount!=0||!BTExpansionWorld.CanLeave(g,out _))throw new Exception("Training expansion still blocks completion");
        Write("APPLIED","Gentle tutorial; optional discovery gardens; one-step bridge; no combined final lock");
    }
    [MenuItem("Tools/Borrowed Time/Factory/Test/Test full journey and finite energy")]
    public static void TestMain(){StartTest(0);}
    // The former garden test is retained for old scene snapshots only.
    public static void TestGardens(){if(!Object.FindFirstObjectByType<BTExpansionWorld>())throw new Exception("Back forest is scenery only; use the main route test");StartTest(1);}
    static void StartTest(int testMode){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        if(SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene retained");
        EditorSceneManager.OpenScene(BTJourney.TrainingPath);SessionState.SetInt("BTFactoryCampaignTrainingMode",testMode+1);waiting=true;EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
    }
    static void StateChanged(PlayModeStateChange state){
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetInt("BTFactoryCampaignTrainingMode",0)>0)waiting=true;
        if(state==PlayModeStateChange.ExitingPlayMode && testing)Finish(false,"Play Mode interrupted");
    }
    static void Tick(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try{
            if(File.Exists(Command)){
                string command=File.ReadAllText(Command).Trim();File.Delete(Command);
                if(command=="apply")Apply();else if(command=="test-main")TestMain();else if(command=="test-gardens")TestGardens();
                else if(command=="status")Write("READY",SceneManager.GetActiveScene().path);
                else if(command=="stop"){if(testing)Finish(false,"Stopped by request");EditorApplication.isPlaying=false;}
                else if(command=="capture")Capture("Training_View.png");
            }
            if(waiting && EditorApplication.isPlaying){
                game=Object.FindFirstObjectByType<BTTrainingGame>();if(!game||!game.player)return;
                waiting=false;testing=true;mode=SessionState.GetInt("BTFactoryCampaignTrainingMode",1)-1;SessionState.SetInt("BTFactoryCampaignTrainingMode",0);
                oldBackground=Application.runInBackground;Application.runInBackground=true;
                BTSaveGame.TestPathOverride=Path.GetFullPath("Library/BTFactoryCampaignTraining.test-save.json");
                game.GetComponent<BTPresentation>().briefingComplete=true;game.Begin();
                step=0;route=null;flowChecked=occlusionChecked=false;started=EditorApplication.timeSinceStartup;since=Time.time;nextAction=Time.time;log.Clear();
                game.player.inputOverride=Input;Note("Started "+(mode==0?"main route without optional gardens":"optional garden route")+" with real CharacterController and no right mouse aim input");Write("TESTING",mode.ToString());
            }
            if(!testing)return;
            if(!EditorApplication.isPlaying)return;
            if(mode==0 && SceneManager.GetActiveScene().path==BTJourney.CityPath){
                var city=Object.FindFirstObjectByType<BTCityGame>();if(!city)return;
                if(!BTJourney.FromTraining||!city.hasMiracleSeed)throw new Exception("Seed was not carried to City");
                Finish(true,"Main route completed and transitioned to ForgottenCity carrying the miracle seed; optional gardens untouched");return;
            }
            if(!game)throw new Exception("Training game missing");
            if(Time.time-since>45)throw new Exception("Timeout step="+step+" position="+game.player.transform.position+" target="+(game.player.target?game.player.target.name:"none")+" feedback="+game.player.feedback);
        }catch(Exception e){if(testing)Finish(false,e.ToString());else{Write("ERROR",e.ToString());Debug.LogException(e);}}
    }
    static void Note(string text){log.Add((EditorApplication.timeSinceStartup-started).ToString("F2")+"s | "+text);if(testing)Write("TESTING",text);}
    static void Next(string text){Note("PASS "+step+" "+text);step++;route=null;since=Time.time;nextAction=Time.time+.7f;}
    static Queue<Vector3> GardenPath(Vector3 start,Vector3 goal){
        Vector2Int a=new Vector2Int(Mathf.RoundToInt(start.x),Mathf.RoundToInt(start.z)),b=new Vector2Int(Mathf.RoundToInt(goal.x),Mathf.RoundToInt(goal.z));
        var frontier=new List<Vector2Int>{a};var costs=new Dictionary<Vector2Int,float>{{a,0}};var came=new Dictionary<Vector2Int,Vector2Int>();var free=new Dictionary<Vector2Int,bool>();Vector2Int found=a;int visits=0;
        while(frontier.Count>0 && visits++<9000){
            int best=0;float score=float.MaxValue;for(int i=0;i<frontier.Count;i++){float s=costs[frontier[i]]+Vector2Int.Distance(frontier[i],b);if(s<score){best=i;score=s;}}
            var n=frontier[best];frontier.RemoveAt(best);if(Vector2Int.Distance(n,b)<.75f){found=n;break;}
            foreach(var direction in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down}){
                var next=n+direction;if(next.x< -60||next.x> -5||next.y< -60||next.y> -8)continue;
                if(!free.TryGetValue(next,out bool valid)){
                    Vector3 p=new Vector3(next.x,0,next.y);bool ground=Physics.Raycast(p+Vector3.up*1.7f,Vector3.down,out var hit,2.15f,~(1<<2),QueryTriggerInteraction.Ignore)&&hit.point.y<.36f&&hit.point.y>-.2f;
                    valid=ground&&!Physics.CheckCapsule(p+Vector3.up*(hit.point.y+.43f),p+Vector3.up*(hit.point.y+1.58f),.38f,~(1<<2),QueryTriggerInteraction.Ignore);free[next]=valid;
                }
                if(!valid)continue;float cost=costs[n]+1;if(costs.ContainsKey(next)&&costs[next]<=cost)continue;costs[next]=cost;came[next]=n;if(!frontier.Contains(next))frontier.Add(next);
            }
        }
        if(found==a&&Vector2Int.Distance(a,b)>1)throw new Exception("No grounded walking route to garden target "+goal);
        var points=new List<Vector3>{goal};var node=found;while(node!=a){points.Add(new Vector3(node.x,0,node.y));node=came[node];}points.Reverse();return new Queue<Vector3>(points);
    }
    static bool Move(Vector3 destination,ref BTPlayer.FrameInput input,float tolerance=.35f){
        Vector3 delta=destination-game.player.transform.position;delta.y=0;if(delta.magnitude<tolerance)return true;
        if(mode==1){
            if(route==null||Vector3.Distance(routeGoal,destination)>.05f){routeGoal=destination;route=GardenPath(game.player.transform.position,destination);}
            while(route.Count>1){Vector3 d=route.Peek()-game.player.transform.position;d.y=0;if(d.magnitude>.35f)break;route.Dequeue();}
            if(route.Count>0){delta=route.Peek()-game.player.transform.position;delta.y=0;}
        }
        Vector3 local=Quaternion.Inverse(Quaternion.Euler(0,game.player.yaw,0))*delta.normalized;input.move=new Vector2(local.x,local.z);return false;
    }
    static bool Aim(BTTimeTarget target,ref BTPlayer.FrameInput input){
        Vector3 delta=target.aimPoint.position-game.player.view.transform.position;
        float yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,pitch=-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg;
        input.look=new Vector2(Mathf.DeltaAngle(game.player.yaw,yaw)*.45f/.14f,-(pitch-game.player.pitch)*.45f/.12f);
        input.aim=false;return game.player.target==target;
    }
    static void Cast(BTTimeTarget target,int goal,ref BTPlayer.FrameInput input){if(Aim(target,ref input)&&Time.time>nextAction){input.scroll=target.age>goal?-120:target.age<goal?120:0;nextAction=Time.time+.7f;}}
    static BTPlayer.FrameInput Input(){
        var input=new BTPlayer.FrameInput();if(!testing)return input;
        try{if(mode==1){Gardens(ref input);return input;}
            switch(step){
                case 0:if(Move(new Vector3(-18,0,-5),ref input)&&game.stage==2)Next("Movement lesson completed");break;
                case 1:if(Move(new Vector3(-15,0,-1.1f),ref input))Next("Reached tree within absorption range");break;
                case 2:
                    if(Aim(game.tree,ref input))input.absorb=true;
                    if(game.player.IsAbsorbing&&!flowChecked&&Time.time-since>.6f){flowChecked=true;Capture("LifeFlow.png");Note("PASS tree-to-gauntlet life flow active without RMB");}
                    if(game.stage==3){if(!flowChecked||game.tree.life<game.tree.minimumLife||game.player.energy<59)throw new Exception("Life flow or nonlethal absorption failed");Next("10-unit life lesson completed");}break;
                case 3:
                    if(Time.time-since<.5f)break;
                    if(game.player.IsAbsorbing)throw new Exception("Absorption continues after releasing LMB");
                    if(Move(new Vector3(-8.5f,0,4),ref input))Next("Absorb release stops flow; bridge approach reached");break;
                case 4:
                    if(!occlusionChecked && Aim(game.bridge,ref input)){
                        game.player.FindTarget(true);if(game.player.target!=game.bridge)break;
                        Vector3 a=game.player.view.transform.position,b=game.bridge.aimPoint.position;
                        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary targeting obstruction";wall.transform.position=(a+b)*.5f;wall.transform.rotation=Quaternion.LookRotation(b-a);wall.transform.localScale=new Vector3(4,4,.5f);Physics.SyncTransforms();
                        game.player.FindTarget(true);bool blocked=!game.player.target;Object.DestroyImmediate(wall);Physics.SyncTransforms();game.player.FindTarget(true);
                        if(!blocked||game.player.target!=game.bridge)throw new Exception("Crosshair selects through a wall");occlusionChecked=true;Note("PASS crosshair respects opaque obstruction");
                    }
                    if(occlusionChecked)Cast(game.bridge,0,ref input);if(game.bridge.age==0&&game.stage==4)Next("One backward scroll restored bridge; sapling was not required");break;
                case 5:if(Move(new Vector3(-5.7f,0,4),ref input))Next("Bridge threshold");break;
                case 6:if(Move(new Vector3(-1,0,4),ref input))Next("Standing on bridge collider");break;
                case 7:
                    Cast(game.bridge,1,ref input);if(Time.time-since>.9f){if(game.bridge.age!=0)throw new Exception("Support removed from under player");Next("Bridge safety preserved while standing on it");}break;
                case 8:if(Move(new Vector3(6.3f,0,4),ref input))Next("Crossed real bridge");break;
                case 9:if(Move(new Vector3(12,0,7.2f),ref input))Next("Vine approach");break;
                case 10:Cast(game.vine,1,ref input);if(game.vine.age==1&&game.stage>=5)Next("One forward scroll grew vine ramp");break;
                case 11:if(Move(new Vector3(12,0,9.3f),ref input))Next("Ramp threshold");break;
                case 12:if(Move(new Vector3(12,3,15.5f),ref input)&&game.player.transform.position.y>2.8f)Next("Climbed vine; gate opened without combined puzzle");break;
                case 13:if(Time.time-since>3&&Move(new Vector3(12,3,20.7f),ref input))Next("Passed final gate");break;
                case 14:
                    var world=game.GetComponent<BTExpansionWorld>();if(world && (world.puzzles.Any(p=>p.solved)||world.RequiredCount!=0))throw new Exception("Optional gardens became mandatory or were auto-completed");
                    input.interact=true;if(game.completed){Capture("TrainingComplete.png");Next("Received seed through F with all optional gardens untouched");}break;
                case 15:break;
            }
        }catch(Exception e){Finish(false,e.ToString());}return input;
    }
    static void Gardens(ref BTPlayer.FrameInput input){
        var world=game.GetComponent<BTExpansionWorld>();var p=world.puzzles;
        switch(step){
            case 0:if(Move(new Vector3(-16,0,-22),ref input))Next("Reached optional garden entrance");break;
            case 1:if(Move(new Vector3(-16,0,-39),ref input))Next("Reached seedling courtyard approach");break;
            case 2:if(Move(new Vector3(-17,0,-37),ref input))Next("Reached optional seedling");break;
            case 3:Cast(p[0].temporal[1],1,ref input);if(p[0].solved)Next("Seedling garden blooms from one forward scroll; no valves or submit needed");break;
            case 4:if(Move(new Vector3(-17,0,-43),ref input))Next("Returned to garden trail");break;
            case 5:if(Move(new Vector3(-38,0,-52),ref input))Next("Reached water garden trail");break;
            case 6:if(Move(new Vector3(-38,0,-47),ref input))Next("Reached water switch");break;
            case 7:input.interact=Time.time>nextAction;if(input.interact)nextAction=Time.time+.8f;if(p[1].solved)Next("Water garden restored with one F press; no linked circuit");break;
            case 8:if(Move(new Vector3(-43,0,-39),ref input))Next("Reached music garden trail");break;
            case 9:if(Move(new Vector3(-43,0,-29.1f),ref input))Next("Reached leaf chime");break;
            case 10:input.interact=Time.time>nextAction;if(input.interact)nextAction=Time.time+.8f;if(p[2].solved)Next("Any one chime opens the optional garden");break;
            case 11:if(Time.time-since>3&&Move(new Vector3(-43,0,-19.8f),ref input))Next("Walked through optional garden gate");break;
            case 12:input.interact=Time.time>nextAction;if(input.interact)nextAction=Time.time+.8f;if(p[2].collected){Capture("OptionalGarden.png");Finish(true,"Three optional discoveries completed through player controls; no sequence guessing; memory leaf collected");}break;
        }
    }
    static void Capture(string filename){
        Directory.CreateDirectory(Output);var g=game?game:Object.FindFirstObjectByType<BTTrainingGame>();if(!g)return;
        var camera=g.player.view;var rt=new RenderTexture(1440,900,24);var previous=camera.targetTexture;var active=RenderTexture.active;
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
        File.WriteAllBytes(Output+"/"+filename,image.EncodeToPNG());
        if(EditorApplication.isPlaying)ScreenCapture.CaptureScreenshot(Output+"/HUD_"+filename);camera.targetTexture=previous;RenderTexture.active=active;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
    }
    static void Finish(bool ok,string detail){
        testing=false;waiting=false;SessionState.SetInt("BTFactoryCampaignTrainingMode",0);if(game&&game.player)game.player.inputOverride=null;
        Application.runInBackground=oldBackground;BTSaveGame.CancelPendingContinue();
        Note((ok?"PASS ":"FAIL ")+detail);Directory.CreateDirectory(Output);File.WriteAllLines(Output+(mode==0?"/MainRoute.txt":"/OptionalGardens.txt"),log);
        Write(ok?"PASS":"FAIL",detail);Debug.Log("BT_TRAINING_COMFORT "+(ok?"PASS ":"FAIL ")+detail);if(ok)BTFactoryCampaignCity.StartAtCity();else{BTSaveGame.TestPathOverride=null;EditorApplication.isPlaying=false;}
    }
}
}
#endif
