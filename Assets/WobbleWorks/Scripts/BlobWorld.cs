using UnityEngine;
using UnityEngine.InputSystem;
namespace OopsPlanet {
public class BlobWorld : MonoBehaviour {
    public BlobPlayer player;
    public BlobInteractable[] sources, zones;
    public Rigidbody crate;
    public Vector3 crateStart,spawn=new Vector3(0,.1f,0);
    public GameObject bridge;
    public Transform plate;
    public Renderer plateLamp;
    public bool bridgeOpen,finished,overview;
    public int checkpointIndex;
    public int propertyMask;
    public bool pipePassed;
    // legacyRoute: the original linear Wobble Greybox 01 route (crate/plate/bridge, four-property exit).
    public bool legacyRoute=true;
    public string levelTitle="WOBBLE WORKS / GREYBOX 01";
    public int checkpointCount=3;
    public Vector3 overviewPosition=new Vector3(54,68,32),overviewTarget=new Vector3(0,3,57);
    public Vector3 checkpoint;
    public float plateHold;
    public string objective="01  MOVE / JUMP  —  Follow the numbered route";
    public int deaths;
    public float elapsed;
    bool resetting;
    GUIStyle title,body,small;
    Texture2D white;
    void Awake() { checkpoint=spawn; }
    void Update() {
        if(!player || player.paused || finished) return;
        elapsed+=Time.deltaTime;
        Vector3 p=player.transform.position;
        float z=p.z;
        if(legacyRoute) objective=z<14?"01  MOVE / JUMP — Cross the two-metre gap":
            z<26?"02  ABSORB — Aim at the source, hold E":
            z<48?"03  MAGNET — Hold F to pull the marked cube onto the red plate":
            z<62?"04  SPRING — Hold SPACE to compress, release + move forward":
            z<82?"05  GAS — Absorb GAS, enter the shaft, hold SPACE to rise":
            z<105?"06  LIQUID — Absorb LIQUID, hold F through the low tunnel":
            "EXIT — Reach the green finish marker";
        if(legacyRoute && crate && !bridgeOpen) {
            Vector3 d=crate.position-plate.position;
            bool inside=Mathf.Abs(d.x)<.95f && Mathf.Abs(d.z)<.95f && Mathf.Abs(d.y)<1.5f;
            plateHold=inside?plateHold+Time.deltaTime:0;
            if(plateHold>.4f) {
                bridgeOpen=true; bridge.SetActive(true);
                if(plateLamp)plateLamp.material.color=new Color(.2f,1,.45f);
                player.Notify("PLATE LOCKED — BRIDGE OPEN",3);
            }
        }
        if(crate && crate.position.y<-6) ResetCrate();
        foreach(var zone in zones) {
            if(!zone.Contains(p+Vector3.up*.2f)) continue;
            switch(zone.kind) {
                case BlobInteraction.Checkpoint:
                    if(zone.index>checkpointIndex) {
                        checkpointIndex=zone.index; checkpoint=zone.respawn;
                        player.Notify("CHECKPOINT "+zone.index+" SAVED",3);
                    }
                    break;
                case BlobInteraction.Pipe:
                    if(player.property==BlobProperty.Liquid && player.flattened && p.z>101) pipePassed=true;
                    break;
                case BlobInteraction.Area:
                    if(!legacyRoute) objective=zone.label;
                    break;
                case BlobInteraction.Exit:
                    if(finished) break;
                    if(!legacyRoute || (pipePassed && propertyMask==30 && bridgeOpen)) {
                        finished=true; player.completed=true; player.Notify("GREYBOX COMPLETE",4);
                        Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
                        Debug.Log((legacyRoute?"WOBBLE":"OOPS")+"_LEVEL_COMPLETE time="+elapsed+" deaths="+deaths);
                    } else player.Notify("Complete the four property stations first");
                    break;
                case BlobInteraction.Reset:
                    if(!player.testMode && Keyboard.current!=null && Keyboard.current.qKey.wasPressedThisFrame) ResetCrate();
                    break;
            }
        }
    }
    public bool InGasVent(Vector3 p) {
        foreach(var z in zones) if(z.kind==BlobInteraction.GasVent && z.Contains(p+Vector3.up*.5f))return true;
        return false;
    }
    public void RecordProperty(BlobProperty p) { if(p!=BlobProperty.None) propertyMask|=1<<(int)p; }
    public void ResetCrate() {
        if(!crate) return;
        crate.position=crateStart; crate.rotation=Quaternion.identity;
        crate.linearVelocity=Vector3.zero; crate.angularVelocity=Vector3.zero; plateHold=0;
        Physics.SyncTransforms();
    }
    public void Respawn() {
        if(resetting)return; resetting=true;
        player.Teleport(checkpoint); player.flattened=false;
        player.Controller.height=1.3f; player.Controller.radius=.36f; player.Controller.center=Vector3.up*.65f; player.Controller.stepOffset=.22f;
        player.SetProperty(BlobProperty.None,false);
        ResetCrate(); deaths++; player.Notify("RESPAWN — CHECKPOINT RESTORED");
        resetting=false;
    }
    public void Restart() {
        checkpoint=spawn; checkpointIndex=0; bridgeOpen=false; if(bridge)bridge.SetActive(false);
        propertyMask=0; pipePassed=false; finished=false; elapsed=0; deaths=-1;
        player.completed=false; player.paused=false; overview=false;
        if(plateLamp)plateLamp.material.color=BlobPlayer.PropertyColor(BlobProperty.Magnet);
        Respawn();
    }
    void EnsureStyles() {
        if(title!=null)return;
        white=Texture2D.whiteTexture;
        title=new GUIStyle(GUI.skin.label){fontSize=24,fontStyle=FontStyle.Bold,wordWrap=true};
        title.normal.textColor=Color.white;
        body=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true}; body.normal.textColor=new Color(.88f,.94f,.98f);
        small=new GUIStyle(body){fontSize=13};
    }
    void Panel(Rect r) {
        GUI.color=new Color(.025f,.045f,.065f,.92f); GUI.DrawTexture(r,white); GUI.color=Color.white;
    }
    void OnGUI() {
        if(!player)return; EnsureStyles();
        float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
        float w=Screen.width/scale,h=Screen.height/scale;
        Panel(new Rect(24,22,650,100));
        GUI.Label(new Rect(40,30,620,35),levelTitle,title);
        GUI.Label(new Rect(40,70,620,45),objective,body);
        Panel(new Rect(w-244,22,220,110));
        GUI.Label(new Rect(w-228,32,200,28),"CHECKPOINT  "+checkpointIndex+" / "+checkpointCount,body);
        GUI.Label(new Rect(w-228,62,200,50),"TIME "+Mathf.FloorToInt(elapsed)+"s  /  RETRIES "+deaths+"\nTAB: overview",small);
        bool hasSources=sources!=null && sources.Length>0;
        if(!overview && hasSources) {
            GUI.Label(new Rect(w/2-12,h/2-16,30,30),"+",title);
            Panel(new Rect(w/2-200,h/2-100,400,64));
            GUI.Label(new Rect(w/2-188,h/2-95,380,24),player.targetStatus,body);
            GUI.color=Color.gray; GUI.DrawTexture(new Rect(w/2-184,h/2-63,368,6),white);
            GUI.color=player.target?BlobPlayer.PropertyColor(player.target.property):Color.white;
            GUI.DrawTexture(new Rect(w/2-184,h/2-63,368*Mathf.Clamp01(player.absorbProgress),6),white); GUI.color=Color.white;
        }
        if(hasSources) {
        Panel(new Rect(24,h-148,560,124));
        GUI.Label(new Rect(40,h-140,520,26),"ONE ACTIVE PROPERTY — "+player.property.ToString().ToUpper(),body);
        for(int i=1;i<=4;i++) {
            GUI.color=(int)player.property==i?BlobPlayer.PropertyColor((BlobProperty)i):new Color(.35f,.39f,.43f);
            GUI.DrawTexture(new Rect(40+(i-1)*132,h-105,120,4),white);
            GUI.Label(new Rect(40+(i-1)*132,h-95,125,25),((BlobProperty)i).ToString().ToUpper(),small);
        }
        GUI.color=Color.white;
        string ability=player.property==BlobProperty.Magnet?"F: pull marked crate":player.property==BlobProperty.Spring?"Hold SPACE / release: launch  "+Mathf.RoundToInt(player.springCharge*100)+"%":
            player.property==BlobProperty.Gas?"SPACE in purple shaft: rise / gentle fall":player.property==BlobProperty.Liquid?"Hold F: flatten / release outside tunnel":"Aim + hold E to acquire a property";
        GUI.Label(new Rect(40,h-65,520,32),ability,small);
        }
        Panel(new Rect(w-584,h-112,560,88));
        GUI.Label(new Rect(w-568,h-104,530,72),hasSources?"WASD move  |  SPACE jump  |  Mouse look\nE hold absorb  |  F property action  |  R respawn\nT restart level  |  ESC pause  |  Q near console: reset crate":
            "WASD move  |  SPACE jump  |  Mouse look\nR respawn  |  T restart  |  ESC pause  |  TAB overview",small);
        if(!string.IsNullOrEmpty(player.feedback)) {
            Panel(new Rect(w/2-270,145,540,50)); GUI.Label(new Rect(w/2-253,155,510,35),player.feedback,body);
        }
        if(player.paused || finished) {
            Panel(new Rect(w/2-240,h/2-130,480,250));
            GUI.Label(new Rect(w/2-214,h/2-110,440,40),finished?(legacyRoute?"GREYBOX COMPLETE":"DEMO COMPLETE"):"PAUSED",title);
            GUI.Label(new Rect(w/2-214,h/2-62,420,65),finished?(legacyRoute?"Four properties. One connected route.":"Demo route complete.")+"\nTime: "+Mathf.FloorToInt(elapsed)+"s / retries: "+deaths:"Click Resume, then use the mouse to aim.\nAbsorb a new property to replace the old one.",body);
            if(GUI.Button(new Rect(w/2-210,h/2+20,200,42),finished?"REPLAY LEVEL":"RESUME")) {
                if(finished)Restart(); player.Resume();
            }
            if(GUI.Button(new Rect(w/2+10,h/2+20,200,42),"RESTART LEVEL")) { Restart();player.Resume(); }
        } else if(!player.testMode && Cursor.lockState!=CursorLockMode.Locked) {
            GUI.Label(new Rect(w/2-235,h-205,500,35),"CLICK THE GAME VIEW TO CAPTURE THE MOUSE",body);
        }
        GUI.matrix=Matrix4x4.identity;
    }
}
}
