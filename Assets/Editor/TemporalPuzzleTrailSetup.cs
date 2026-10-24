using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>Seven explicitly labelled mechanical prototypes. Never regenerates the forest.</summary>
public static class TemporalPuzzleTrailSetup
{
    private const string MatFolder = "Assets/03_EnvironmentArt/TemporalPrototype";
    private static Material wood, rot, stone, moss, leaf, glow, dark, water, gold;
    private struct Form
    {
        public string name; public Vector3 size; public Material material; public PrimitiveType shape;
        public Form(string name, Vector3 size, Material material, PrimitiveType shape = PrimitiveType.Cube)
        { this.name=name; this.size=size; this.material=material; this.shape=shape; }
    }
    [MenuItem("Borrowed Time/Handoff/Build seven puzzle prototype trail")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
        if(GameObject.Find("BT_ForestPuzzleTrail")!=null)throw new InvalidOperationException("Puzzle trail already exists; nothing overwritten.");
        var terrain=GameObject.Find("BT_ForestTerrain");
        if(terrain==null||terrain.GetComponent<MeshFilter>()==null||terrain.GetComponent<MeshCollider>()==null)throw new InvalidOperationException("Forest terrain is missing.");
        if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ithappy/Animals_FREE/Prefabs/Chicken_001.prefab")==null)throw new InvalidOperationException("Installed Chicken asset is missing.");
        if(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/03_EnvironmentArt/Models/BT_ForestTerrain_PuzzleTrail.asset")!=null)throw new InvalidOperationException("Trail terrain output exists; nothing overwritten.");
        InitMaterials();var root=new GameObject("BT_ForestPuzzleTrail");
        Category1(Group(root,"P1_Change"));Category2(Group(root,"P2_Growth"));Category3(Group(root,"P3_LifeSwitch"));
        Category4(Group(root,"P4_Water"),terrain);Category5(Group(root,"P5_Animal"));Category6(Group(root,"P6_Chain"));Category7(Group(root,"P7_ReverseAndChoice"));
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);AssetDatabase.SaveAssets();
        Debug.Log("Seven optional puzzle prototype stations built. Main forest keystone retained.");
    }
    private static Transform Group(GameObject root,string name){var o=new GameObject(name);o.transform.SetParent(root.transform,false);return o.transform;}
    private static void InitMaterials()
    {
        if(!AssetDatabase.IsValidFolder(MatFolder))AssetDatabase.CreateFolder("Assets/03_EnvironmentArt","TemporalPrototype");
        wood=Mat("Timber",new Color(.38f,.24f,.12f));rot=Mat("Rotted",new Color(.16f,.14f,.09f));
        stone=Mat("Stone",new Color(.35f,.37f,.34f));moss=Mat("Moss",new Color(.19f,.29f,.12f));
        leaf=Mat("Growth",new Color(.24f,.50f,.18f));glow=Mat("Glow",new Color(.2f,.75f,.95f),true);
        dark=Mat("Cave",new Color(.035f,.045f,.045f));water=Mat("Water",new Color(.08f,.33f,.43f));gold=Mat("Reward",new Color(.85f,.61f,.18f),true);
    }
    private static Material Mat(string name,Color color,bool emission=false)
    {
        string path=MatFolder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);
        if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*2f);}
        AssetDatabase.CreateAsset(m,path);return m;
    }
    private static Vector3 At(float x,float z,float y=0f)
    {
        RaycastHit h;var c=GameObject.Find("BT_ForestTerrain").GetComponent<MeshCollider>();
        float ground=c.Raycast(new Ray(new Vector3(x,100,z),Vector3.down),out h,200)?h.point.y:0f;
        return new Vector3(x,ground+y,z);
    }
    private static GameObject Block(Transform parent,string name,Vector3 pos,Vector3 size,Material material,bool solid=true,PrimitiveType shape=PrimitiveType.Cube)
    {
        var o=GameObject.CreatePrimitive(shape);o.name=name;o.transform.SetParent(parent,false);o.transform.position=pos;
        o.transform.localScale=new Vector3(size.x,shape==PrimitiveType.Cylinder?size.y*.5f:size.y,size.z);
        o.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;
    }
    private static TimeTarget Target(Transform parent,string name,Vector3 pos,int initial,float cost,params Form[] forms)
    {
        var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.position=pos;
        var target=root.AddComponent<TimeTarget>();var so=new SerializedObject(target);var states=so.FindProperty("states");states.arraySize=forms.Length;
        for(int i=0;i<forms.Length;i++)
        {
            var f=forms[i];var obj=Block(root.transform,"State_"+i+"_"+f.name,pos+Vector3.up*(f.size.y*.5f),f.size,f.material,true,f.shape);
            var state=states.GetArrayElementAtIndex(i);state.FindPropertyRelative("stateName").stringValue=f.name;
            state.FindPropertyRelative("visualRoot").objectReferenceValue=obj;state.FindPropertyRelative("stateCollider").objectReferenceValue=obj.GetComponent<Collider>();
            state.FindPropertyRelative("isHealthy").boolValue=i==1;state.FindPropertyRelative("isTraversable").boolValue=true;obj.SetActive(i==initial);
        }
        so.FindProperty("startingStateIndex").intValue=initial;so.FindProperty("energyCostPerChange").floatValue=cost;
        so.FindProperty("displayName").stringValue=name.Replace('_',' ');so.ApplyModifiedPropertiesWithoutUndo();return target;
    }
    private static void Sign(Transform parent,string text,Vector3 pos)
    {
        var o=new GameObject("SIGN_"+text.Split('\n')[0]);o.transform.SetParent(parent,false);o.transform.position=pos;
        var label=o.AddComponent<TextMeshPro>();label.text=text;label.fontSize=2.2f;label.alignment=TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta=new Vector2(17,5);label.color=new Color(.93f,.94f,.81f);o.AddComponent<TemporalWorldLabel>();
    }
    private static void Category1(Transform group)
    {
        var p=At(16,34);Target(group,"P1_Timber",p,0,10,new Form("Fresh",new Vector3(5,1.5f,1.2f),wood),new Form("Rotted",new Vector3(5,.15f,1.2f),rot));
        Target(group,"P1_ErodingStone",At(23,35),0,8,new Form("Sound",Vector3.one*2.5f,stone),new Form("Eroded",Vector3.one*.3f,moss));
        Sign(group,"1  STATE CHANGE\nE ages / Q restores",p+new Vector3(2,3.3f,-3));
    }
    private static void Category2(Transform group)
    {
        var p=At(-24,59);Target(group,"P2_SeedStructure",p,0,8,new Form("Seed",new Vector3(.2f,.2f,.2f),rot),new Form("Sapling",new Vector3(1,2.5f,1),leaf),new Form("Tree",new Vector3(2,5,2),leaf));
        Block(group,"GREYBOX_TreeLookout",p+new Vector3(0,5.1f,3),new Vector3(6,.3f,4),wood);
        var v=At(-32,63);var vine=Target(group,"P2_GrowingVine",v,0,8,new Form("Short",new Vector3(.3f,.15f,.3f),moss),new Form("Reach",new Vector3(.3f,3.075f,.3f),leaf),new Form("Long",new Vector3(.3f,6,.3f),leaf));
        var region=new GameObject("ClimbRegion");region.transform.SetParent(group,false);region.transform.position=v;
        var box=region.AddComponent<BoxCollider>();box.isTrigger=true;
        var climb=region.AddComponent<TemporalClimbVolume>();
        var exit=new GameObject("ClimbExit");exit.transform.SetParent(group,false);exit.transform.position=v+new Vector3(0,6.3f,1.8f);climb.topExit=exit.transform;
        var growth=group.gameObject.AddComponent<TemporalGrowthStructure>();growth.target=vine;growth.climbVolume=climb;growth.maximumHeight=6;
        Block(group,"GREYBOX_VineLookout",v+new Vector3(0,6,3),new Vector3(5,.3f,4),wood);
        var m=At(-31,53);Target(group,"P2_MushroomStep",m,0,6,new Form("Bud",new Vector3(.5f,.15f,.5f),moss,PrimitiveType.Cylinder),new Form("Growing",new Vector3(1.6f,.9f,1.6f),leaf,PrimitiveType.Cylinder),new Form("Platform",new Vector3(3,1.6f,3),leaf,PrimitiveType.Cylinder));
        Block(group,"GREYBOX_MushroomShelf",m+new Vector3(0,2.1f,3),new Vector3(4,.3f,3),stone);
        Sign(group,"2  GROW A WAY UP\nE grows; W/S climbs vines; Space releases",p+new Vector3(-3,3,-8));
    }
    private static void Category3(Transform group)
    {
        var p=At(-40,97);Block(group,"GREYBOX_CaveLeft",p+new Vector3(-3,2,2),new Vector3(.4f,4,9),dark);
        Block(group,"GREYBOX_CaveRight",p+new Vector3(3,2,2),new Vector3(.4f,4,9),dark);
        Block(group,"GREYBOX_CaveRoof",p+new Vector3(0,4,2),new Vector3(6.4f,.4f,9),dark);
        var flower=Target(group,"P3_GlowFlower",p+new Vector3(-1.7f,0,-2),0,5,new Form("Bud",new Vector3(.35f,.4f,.35f),moss),new Form("Bloom",new Vector3(.8f,.9f,.8f),glow));
        var lamp=new GameObject("BloomLight");lamp.transform.SetParent(group,false);lamp.transform.position=p+new Vector3(0,2,0);
        var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=12;light.intensity=9;light.color=new Color(.35f,.8f,1);
        var life=flower.gameObject.AddComponent<TemporalLifeSwitch>();life.target=flower;life.activeState=1;life.pathLight=light;
        var hedge=Target(group,"P3_LeafCurtain",p+new Vector3(0,0,5.8f),1,5,new Form("Bare",new Vector3(.3f,1,.3f),wood),new Form("Leafy",new Vector3(.8f,1.8f,.8f),leaf),new Form("Leafless",new Vector3(.3f,1,.3f),rot));
        var curtain=Block(group,"LeafSecretBarrier",p+new Vector3(0,1.6f,6.5f),new Vector3(5.6f,3.2f,.25f),leaf);
        var secret=hedge.gameObject.AddComponent<TemporalLifeSwitch>();secret.target=hedge;secret.activeState=2;secret.hiddenPassageBarrier=curtain;secret.revealPassageWhenActive=true;
        Block(group,"SecretMarker",p+new Vector3(0,.8f,8),Vector3.one*.5f,gold,false,PrimitiveType.Sphere);
        Sign(group,"3  LIFE IS A SWITCH\nGrow light; fallen leaves reveal the exit",p+new Vector3(0,3,-5));
    }
    private static void Category4(Transform group,GameObject terrain)
    {
        var oldBridge=GameObject.Find("Bridge_Target");if(oldBridge!=null)oldBridge.SetActive(false);
        var marker=GameObject.Find("BT_World/Zone_01_Forest/PZL_F4_LogBridge_REJUV_overStream");if(marker!=null)marker.SetActive(false);
        CutBasin(terrain);
        Block(group,"BasinBed",new Vector3(36,-1.8f,87),new Vector3(14,.35f,16),rot);
        var west=Block(group,"DryWestRamp",new Vector3(30,-.8f,82),new Vector3(Mathf.Sqrt(18.56f),.25f,5),stone);west.transform.rotation=Quaternion.Euler(0,0,-Mathf.Atan2(1.6f,4)*Mathf.Rad2Deg);
        var east=Block(group,"DryEastRamp",new Vector3(42,-.8f,82),new Vector3(Mathf.Sqrt(18.56f),.25f,5),stone);east.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(1.6f,4)*Mathf.Rad2Deg);
        var surface=Block(group,"WaterSurface",new Vector3(36,.1f,87),new Vector3(14,.06f,16),water,false);
        var log=Block(group,"FloatingTimber",new Vector3(36,.05f,89),new Vector3(14,.4f,1.8f),wood);
        var gate=Target(group,"P4_Floodgate",At(26,86),1,10,new Form("Full",new Vector3(1.1f,1.3f,1.1f),stone),new Form("Dry",new Vector3(1,.6f,1),rot));
        var sealedRamps=new GameObject("FloodedDryRoute");sealedRamps.transform.SetParent(group,false);
        Block(sealedRamps.transform,"FloodedWestEntry",new Vector3(28.4f,1,82),new Vector3(.4f,3,5),water);
        Block(sealedRamps.transform,"FloodedEastEntry",new Vector3(43.6f,1,82),new Vector3(.4f,3,5),water);
        // Optional emergency steps lead back to the west bank, never strand a player in the basin.
        Block(group,"BasinEscapeStep",new Vector3(29.5f,-1.2f,94),new Vector3(1.5f,.5f,1.8f),stone);
        Block(group,"BasinEscapeUpper",new Vector3(28.5f,-.5f,94),new Vector3(1.5f,.5f,1.8f),stone);
        var puzzle=group.gameObject.AddComponent<TemporalWaterPuzzle>();puzzle.floodgate=gate;puzzle.water=surface.transform;puzzle.floatingLog=log.transform;puzzle.floodedBarrier=sealedRamps;puzzle.drySurface=-1.4f;puzzle.fullSurface=.1f;
        Sign(group,"4  TWO WATER ROUTES\nQ fills and floats timber; E drains the bed",At(25,79,3));
    }
    private static void Category5(Transform group)
    {
        var p=At(25,115);var source=Target(group,"P5_NurseryTree",p,2,8,new Form("Young",new Vector3(.5f,1,.5f),leaf),new Form("Mature",new Vector3(2,3.5f,2),leaf),new Form("Withered",new Vector3(.6f,2.7f,.6f),rot));
        var shelf=At(34,124,3.8f);Block(group,"GREYBOX_UnreachableShelf",shelf-Vector3.up*.2f,new Vector3(7,.4f,5),stone);
        var planted=Target(group,"P5_DeliveredSeed",shelf,0,6,new Form("Seed",Vector3.one*.2f,rot),new Form("Grown",new Vector3(2,3.2f,2),leaf));
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ithappy/Animals_FREE/Prefabs/Chicken_001.prefab");
        var bird=(GameObject)PrefabUtility.InstantiatePrefab(asset,group);bird.name="SeedCarrier_Chicken_Prototype";bird.transform.position=p+new Vector3(2,0,0);
        var renders=bird.GetComponentsInChildren<Renderer>(true);if(renders.Length>0){var b=renders[0].bounds;foreach(var r in renders)b.Encapsulate(r.bounds);if(b.size.y>.001f)bird.transform.localScale*=.65f/b.size.y;}
        var animator=bird.GetComponentInChildren<Animator>();if(animator!=null)animator.applyRootMotion=false;
        foreach(var behaviour in bird.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
        foreach(var controller in bird.GetComponentsInChildren<CharacterController>(true))controller.enabled=false;
        var seed=Block(group,"CarriedSeed",bird.transform.position+new Vector3(0,.45f,.22f),Vector3.one*.14f,gold,false,PrimitiveType.Sphere);
        var route=new Transform[3];var positions=new[]{p+new Vector3(4,0,3),p+new Vector3(7,1.7f,6),shelf+new Vector3(0,.25f,0)};
        for(int i=0;i<route.Length;i++){var w=new GameObject("BirdWaypoint_"+i);w.transform.SetParent(group,false);w.transform.position=positions[i];route[i]=w.transform;}
        var delivery=group.gameObject.AddComponent<TemporalSeedCarrier>();delivery.sourceTree=source;delivery.sourceHealthyState=1;delivery.animal=bird.transform;delivery.animalAnimator=animator;delivery.seed=seed.transform;delivery.route=route;delivery.plantedTarget=planted;delivery.travelSpeed=2;delivery.growthSeconds=3;
        Sign(group,"5  LIFE HELPS LIFE\nQ restores the nursery; watch the bird carry a seed",p+new Vector3(1,4,-4));
    }
    private static void Category6(Transform group)
    {
        var p=At(-18,130);var shade=Target(group,"P6_ShadeTreeA",p,1,8,new Form("Young",new Vector3(.4f,1,.4f),leaf),new Form("Shade",new Vector3(4,4,4),leaf),new Form("Leafless",new Vector3(.45f,2,.45f),rot));
        var plant=Target(group,"P6_SunPlantB",p+new Vector3(1.4f,0,1),0,5,new Form("Bud",Vector3.one*.2f,moss),new Form("Rooted",new Vector3(2,2.8f,2),leaf));
        var rock=Block(group,"PushedRock",p+new Vector3(3.5f,1.25f,4),new Vector3(3,2.5f,2),stone);
        var lamp=new GameObject("NewSunlight");lamp.transform.SetParent(group,false);lamp.transform.position=p+new Vector3(1.4f,5,1);lamp.transform.rotation=Quaternion.Euler(90,0,0);
        var light=lamp.AddComponent<Light>();light.type=LightType.Spot;light.range=10;light.spotAngle=35;light.intensity=5;light.color=new Color(1,.9f,.55f);
        var chain=group.gameObject.AddComponent<TemporalGrowthChain>();chain.shadeTree=shade;chain.exposedState=2;chain.sunPlant=plant;chain.grownState=1;chain.pushedRock=rock.transform;chain.rockOffset=new Vector3(4,0,0);chain.sunlight=light;chain.growthSeconds=3;
        Sign(group,"6  FIND THE RELATIONSHIP\nAge A; sunlight grows B; roots push the rock",p+new Vector3(2,5,-4));
    }
    private static void Category7(Transform group)
    {
        var p=At(-52,133);var control=Target(group,"P7_MisalignedBridge",p,1,5,new Form("Repair",new Vector3(1,1,1),wood),new Form("Worn",new Vector3(1,.7f,1),rot),new Form("Collapsed",new Vector3(1,.2f,1),rot));
        var deck=Block(group,"RebuildDeck",p+new Vector3(0,1,8),new Vector3(2.5f,.25f,2.5f),wood);
        var reward=Block(group,"RebuildReward",p+new Vector3(0,2,12),Vector3.one*.5f,gold,false,PrimitiveType.Sphere);reward.SetActive(false);
        var reverse=group.gameObject.AddComponent<TemporalRebuildPuzzle>();reverse.target=control;reverse.deck=deck.transform;reverse.reward=reward;
        Sign(group,"7A  BREAK BEFORE REPAIR\nDirect Q stays short. E collapse, then Q realigns.",p+new Vector3(0,3,-3));
        var choice=group.gameObject.AddComponent<TemporalOptionalChoice>();choice.routes=new TimeTarget[3];choice.rewards=new GameObject[3];choice.requiredState=0;
        var brace=Block(group,"OneSharedBrace",At(-66,146,1),new Vector3(2,.25f,.35f),gold);choice.remainingBrace=brace;
        for(int i=0;i<3;i++)
        {
            var q=At(-78+i*12,154);
            choice.routes[i]=Target(group,"P7_ChoiceRoute_"+i,q,1,5,new Form("Open",new Vector3(.3f,.3f,.3f),wood),new Form("Closed",new Vector3(3,3,.5f),stone));
            choice.rewards[i]=Block(group,"OptionalReward_"+i,q+new Vector3(0,1.2f,4),Vector3.one*.65f,gold,false,PrimitiveType.Sphere);
            Block(group,"ChoiceWallLeft_"+i,q+new Vector3(-2.3f,1.8f,2),new Vector3(.4f,3.6f,5),stone);
            Block(group,"ChoiceWallRight_"+i,q+new Vector3(2.3f,1.8f,2),new Vector3(.4f,3.6f,5),stone);
            Block(group,"ChoiceBack_"+i,q+new Vector3(0,1.8f,4.5f),new Vector3(5,3.6f,.4f),stone);
        }
        Sign(group,"7B  ONE BRACE, THREE OPTIONAL ROUTES\nCommit one Q repair. Main story is never blocked.",At(-66,148,3));
        var fragile=Target(group,"P7_ExhaustibleSupport",At(-84,138),0,10,new Form("Useful",new Vector3(2,1.2f,2),wood),new Form("Spent",new Vector3(2,.15f,2),rot));
        var support=Block(group,"OptionalSupportStep",At(-84,141,.8f),new Vector3(2,1.6f,2),wood);
        var spent=fragile.gameObject.AddComponent<TemporalExhaustibleSource>();spent.target=fragile;spent.optionalSupport=support;
        Sign(group,"OPTIONAL: extraction destroys this support permanently\nOther sources and the main route remain.",At(-84,135,3));
    }
    private static void CutBasin(GameObject terrain)
    {
        var filter=terrain.GetComponent<MeshFilter>();var copy=UnityEngine.Object.Instantiate(filter.sharedMesh);copy.name="BT_ForestTerrain_PuzzleTrail";
        var vertices=copy.vertices;var triangles=copy.triangles;var kept=new List<int>();
        for(int i=0;i<triangles.Length;i+=3){var c=(terrain.transform.TransformPoint(vertices[triangles[i]])+terrain.transform.TransformPoint(vertices[triangles[i+1]])+terrain.transform.TransformPoint(vertices[triangles[i+2]]))/3f;if(c.x>29&&c.x<43&&c.z>79&&c.z<95)continue;kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);}
        copy.triangles=kept.ToArray();copy.RecalculateBounds();AssetDatabase.CreateAsset(copy,"Assets/03_EnvironmentArt/Models/BT_ForestTerrain_PuzzleTrail.asset");filter.sharedMesh=copy;
        var collider=terrain.GetComponent<MeshCollider>();collider.sharedMesh=null;collider.sharedMesh=copy;
    }
}
