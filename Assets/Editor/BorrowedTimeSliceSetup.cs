using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One-time additive greybox/UI wiring. Never authors a final model.</summary>
public static class BorrowedTimeSliceSetup
{
    private static readonly string ArtPath = "Assets/03_EnvironmentArt/Greybox";

    [MenuItem("BORROWED TIME/Build Greybox Slice")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
        if (GameObject.Find("BT_Slice_Greybox") != null) throw new InvalidOperationException("Slice already wired; refusing duplicate build.");
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null || TMP_Settings.defaultFontAsset == null) throw new InvalidOperationException("URP/TMP resources unavailable.");
        System.IO.Directory.CreateDirectory(ArtPath);
        AssetDatabase.Refresh();
        var stone = Material("Stone", new Color(.24f,.30f,.31f), shader);
        var soil = Material("Soil", new Color(.30f,.27f,.21f), shader);
        var green = Material("Restored", new Color(.19f,.57f,.37f), shader);
        var dead = Material("Polluted", new Color(.39f,.32f,.23f), shader);
        var timber = Material("Timber", new Color(.58f,.40f,.22f), shader);
        var water = Material("Ravine", new Color(.07f,.16f,.21f), shader);
        var root = new GameObject("BT_Slice_Greybox");
        Box("StartBank", root.transform, new Vector3(0,-.5f,-1), new Vector3(20,1,16), soil);
        Box("FarBank", root.transform, new Vector3(0,-.5f,18), new Vector3(20,1,10), stone);
        Box("RavineBottom", root.transform, new Vector3(0,-5,10), new Vector3(20,.5f,6), water).GetComponent<Collider>().enabled = false;
        // Tall, visible greybox boundaries prevent bypassing the six-metre gap.
        Box("WestBoundary", root.transform, new Vector3(-10.6f,2,7), new Vector3(1.2f,5,33), stone);
        Box("EastBoundary", root.transform, new Vector3(10.6f,2,7), new Vector3(1.2f,5,33), stone);
        Box("StartBoundary", root.transform, new Vector3(0,2,-9.6f), new Vector3(22,5,1.2f), stone);
        Box("FarBoundary", root.transform, new Vector3(0,2,23.6f), new Vector3(22,5,1.2f), stone);
        var ground = GameObject.Find("Ground");
        if (ground != null) ground.SetActive(false);

        var polluted = new GameObject("PollutedDressing_Greybox"); polluted.transform.SetParent(root.transform);
        var restored = new GameObject("RestoredDressing_Greybox"); restored.transform.SetParent(root.transform);
        for (int i=0;i<6;i++)
        {
            float x = i%2==0 ? -7f : 7f;
            float z = -4f + (i/2)*4;
            Box("DeadMarker_"+i,polluted.transform,new Vector3(x,.5f,z),new Vector3(.6f,1,.6f),dead);
            Box("GreenMarker_"+i,restored.transform,new Vector3(x,.7f,z),new Vector3(1.2f,1.4f,1.2f),green);
        }
        restored.SetActive(false);
        var reveal = new GameObject("UnlockedZoneContent"); reveal.transform.SetParent(root.transform);
        Box("RestoredLandmark_Greybox",reveal.transform,new Vector3(0,1,20),new Vector3(2,2,2),green);
        reveal.SetActive(false);
        var barrier = Box("ZoneBarrier_Greybox",root.transform,new Vector3(0,1.25f,13.7f),new Vector3(20,2.5f,.5f),dead);

        var tree=GameObject.Find("Tree_Target").GetComponent<TimeTarget>();
        var bridge=GameObject.Find("Bridge_Target").GetComponent<TimeTarget>();
        // Retain the authored target roots, state order, costs and prerequisite.
        var bs=new SerializedObject(bridge); var states=bs.FindProperty("states");
        for(int i=0;i<states.arraySize;i++)
        {
            var state=states.GetArrayElementAtIndex(i);
            var visual=(GameObject)state.FindPropertyRelative("visualRoot").objectReferenceValue;
            if(visual==null)continue;
            visual.transform.localPosition=i==2?new Vector3(0,.1f,-2):new Vector3(0,.15f,0);
            visual.transform.localScale=i==2?new Vector3(2,.2f,1.5f):new Vector3(2,.3f,6.1f);
            var renderer=visual.GetComponent<Renderer>();if(renderer!=null)renderer.sharedMaterial=timber;
            visual.SetActive(i==2);
            var col=(Collider)state.FindPropertyRelative("stateCollider").objectReferenceValue;
            if(col!=null)col.enabled=i==2;
        }
        var puzzle=GameObject.Find("EnvironmentPuzzle").GetComponent<PuzzleCondition>();
        var zone=GameObject.Find("Zone").GetComponent<ZoneUnlock>();
        RefArray(zone,"barriersToRemove",barrier); RefArray(zone,"contentToReveal",reveal);
        var restoration=GameObject.Find("RestorationController").GetComponent<RestorationController>();
        Ref(restoration,"pollutedDressing",polluted);Ref(restoration,"restoredDressing",restored);
        Text(restoration,"restorationMessage","The path remembers. The land breathes again.");

        BuildCanvas(puzzle);
        Trigger("Hint_Spawn",root.transform,new Vector3(0,1,-2),new Vector3(5,3,4),"Time Gauntlet ready. Aim at the withered tree.");
        Trigger("Hint_Tree",root.transform,new Vector3(-4,1,3.5f),new Vector3(5,3,3),"Q rewinds. E ages. The tree remembers being younger.");
        Trigger("Hint_Finish",reveal.transform,new Vector3(3,1,17),new Vector3(7,3,4),"Restoration complete. You opened a future for this place.");
        var audioGO=new GameObject("AudioCueManager");audioGO.transform.SetParent(root.transform);
        var source=audioGO.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
        var audio=audioGO.AddComponent<AudioCueManager>();Ref(audio,"source",source);Ref(audio,"gauntlet",GameObject.Find("TimeGauntletController").GetComponent<TimeGauntletController>());
        Ref(audio,"energy",GameObject.Find("EnergyManager").GetComponent<EnergyManager>());Ref(audio,"puzzle",puzzle);Ref(audio,"zone",zone);
        var player=GameObject.Find("Player");player.transform.position=new Vector3(0,.1f,-3);player.transform.rotation=Quaternion.identity;
        EditorSceneManager.MarkSceneDirty(player.scene);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(player.scene);
        Debug.Log("BT greybox slice wired: map, HUD, tutorial, rejection display and silent audio.");
    }

    private static void BuildCanvas(PuzzleCondition puzzle)
    {
        var go=GameObject.Find("Canvas");var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=go.GetComponent<CanvasScaler>()??go.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var hud=go.GetComponent<HUDDisplay>();var hints=go.GetComponent<HintTextManager>();
        var energy=Panel("EnergyPanel",go.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(30,-30),new Vector2(330,115));
        Label("Title",energy.transform,"BORROWED TIME",new Vector2(15,-12),new Vector2(290,30),20);
        var bar=Panel("EnergyFill",energy.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(15,-78),new Vector2(290,12));
        var fill=bar.GetComponent<Image>();fill.color=new Color(.25f,.85f,.72f);fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;
        // A built-in sprite provides filled-image geometry without a custom art asset.
        fill.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Ref(hud,"energyFill",fill);Ref(hud,"energyLabel",Label("Energy",energy.transform,"100 / 100",new Vector2(15,-42),new Vector2(290,35),26));
        var target=Panel("TargetPanel",go.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,150),new Vector2(390,90));
        Ref(hud,"targetPanel",target);
        Ref(hud,"targetNameLabel",Label("TargetName",target.transform,"",new Vector2(18,-8),new Vector2(350,35),28));
        Ref(hud,"targetStateLabel",Label("TargetState",target.transform,"",new Vector2(18,-45),new Vector2(350,30),22));
        var progress=Panel("ProgressPanel",go.transform,new Vector2(1,1),new Vector2(1,1),new Vector2(-30,-30),new Vector2(270,105));
        Label("ProgressTitle",progress.transform,"RESTORE THE CROSSING",new Vector2(15,-12),new Vector2(245,30),18);
        Ref(hud,"puzzleProgressLabel",Label("Progress",progress.transform,"0 / 2",new Vector2(15,-47),new Vector2(240,35),28));Ref(hud,"trackedPuzzle",puzzle);
        var cross=Panel("Crosshair",go.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(7,7));Ref(hud,"crosshair",cross.GetComponent<Image>());
        var hint=Panel("HintPanel",go.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,50),new Vector2(1120,70));
        var group=hint.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
        var hintText=Label("Hint",hint.transform,"",new Vector2(20,-10),new Vector2(1080,50),28);hintText.alignment=TextAlignmentOptions.Center;
        Ref(hints,"hintLabel",hintText);Ref(hints,"hintGroup",group);
        var controls=Label("Controls",go.transform,"WASD Move   SHIFT Sprint   SPACE Jump   Q Rewind   E Age   ESC Cursor",new Vector2(30,26),new Vector2(1200,35),19);
        var rect=controls.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,0);rect.pivot=new Vector2(0,0);rect.anchoredPosition=new Vector2(30,12);
    }
    private static GameObject Panel(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;
        var image=go.GetComponent<Image>();image.color=new Color(.035f,.06f,.08f,.86f);image.raycastTarget=false;return go;
    }
    private static TextMeshProUGUI Label(string name,Transform parent,string text,Vector2 position,Vector2 size,float fontSize)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;
        var label=go.AddComponent<TextMeshProUGUI>();label.font=TMP_Settings.defaultFontAsset;label.text=text;label.fontSize=fontSize;label.color=new Color(.88f,.94f,.93f);label.raycastTarget=false;return label;
    }
    private static void Trigger(string name,Transform parent,Vector3 position,Vector3 size,string message)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=position;var col=go.AddComponent<BoxCollider>();col.isTrigger=true;col.size=size;
        // Static trigger volumes need a kinematic body to reliably notify the CharacterController.
        var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
        var hint=go.AddComponent<HintTrigger>();Text(hint,"message",message);
    }
    private static Material Material(string name,Color color,Shader shader)
    {
        string path=ArtPath+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;m=new Material(shader);m.color=color;AssetDatabase.CreateAsset(m,path);return m;
    }
    private static GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    private static void Ref(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void RefArray(UnityEngine.Object target,string field,GameObject value){var so=new SerializedObject(target);var a=so.FindProperty(field);a.arraySize=1;a.GetArrayElementAtIndex(0).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void Text(UnityEngine.Object target,string field,string value){var so=new SerializedObject(target);so.FindProperty(field).stringValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
}
