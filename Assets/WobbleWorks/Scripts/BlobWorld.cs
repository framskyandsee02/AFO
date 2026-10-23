using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
public class BlobWorld : MonoBehaviour {
    public BlobPlayer player;
    public BlobInteractable[] sources, zones;
    public PuzzleObjective[] puzzles;
    public Vector3 spawn=new Vector3(0,.1f,0);
    public bool finished,overview;
    public int checkpointIndex;
    public int propertyMask;
    public string levelTitle="OOPS! PLANET";
    public int checkpointCount=2;
    public Vector3 overviewPosition=new Vector3(22,70,-40),overviewTarget=new Vector3(22,0,18);
    public Vector3 checkpoint;
    public string objective="";
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
        foreach(var zone in zones) {
            if(!zone.Contains(p+Vector3.up*.2f)) continue;
            switch(zone.kind) {
                case BlobInteraction.Checkpoint:
                    if(zone.index>checkpointIndex) {
                        checkpointIndex=zone.index; checkpoint=zone.respawn;
                        player.Notify("CHECKPOINT "+zone.index+" SAVED",3);
                    }
                    break;
                case BlobInteraction.Area:
                    objective=zone.label;
                    break;
                case BlobInteraction.Exit:
                    if(finished) break;
                    finished=true; player.completed=true; player.Notify("DEMO COMPLETE",4);
                    Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
                    Debug.Log("OOPS_LEVEL_COMPLETE time="+elapsed+" deaths="+deaths);
                    break;
            }
        }
    }
    public void RecordProperty(BlobProperty p) { if(p!=BlobProperty.None) propertyMask|=1<<(int)p; }
    public void Respawn() {
        if(resetting)return; resetting=true;
        player.Teleport(checkpoint);
        player.ClearProperties();
        if(player.powers) player.powers.ResetState();
        RestoreSources(false);
        deaths++; player.Notify("RESPAWN — CHECKPOINT RESTORED");
        resetting=false;
    }
    // Restart = new game: the level and all progress are reset and the cleared progress is saved.
    public void Restart() { ResetLevel(); Save(); }
    void ResetLevel() {
        checkpoint=spawn; checkpointIndex=0;
        propertyMask=0; finished=false; elapsed=0; deaths=-1;
        player.completed=false; player.paused=false; overview=false;
        RestoreSources(true);
        BlobMagnetic.ResetAll();
        PuzzleItem.ResetAll();
        if(puzzles!=null) foreach(var pz in puzzles) if(pz) pz.ResetPuzzle(true);
        if(zoneGates!=null) foreach(var g in zoneGates) if(g) g.Lock();
        if(hub) hub.Refresh(true);
        Respawn();
    }

    // ---- Progression + persistence (P05) ----
    // Single scene by design: Hub, Factory and Rice Fields are one small contiguous greybox, so zones are
    // gated with ZoneGates instead of scene loading (no load hitches, one place to test, simple save state).
    [Header("Progress / save")]
    public string saveSlot=SaveSystem.DefaultSlot;
    public bool autoLoad=true;
    public ZoneGate[] zoneGates;
    public HubProgress hub;
    public SaveSystem.LoadStatus lastLoadStatus;
    public int saves;
    bool loading, hooked;
    void Start() {
        Hook();
        if(autoLoad) LoadFromSlot(saveSlot); else RefreshProgress(true,false);
    }
    void Hook() {
        if(hooked || puzzles==null) return; hooked=true;
        foreach(var pz in puzzles) if(pz) pz.Solved+=o=>{ if(!loading) RefreshProgress(false,true); };
    }
    // Unlock zones whose requirements are now met, update the Hub, optionally save.
    public void RefreshProgress(bool instant,bool save) {
        if(zoneGates!=null) foreach(var g in zoneGates)
            if(g && !g.Unlocked && g.RequirementsMet) { g.Unlock(instant); if(!instant && player) player.Notify(g.zoneNameTh+" เปิดแล้ว! กลับไปที่ฮับเพื่อไปต่อ",4); }
        if(hub) hub.Refresh(instant);
        if(save) Save();
    }
    public PuzzleObjective FindPuzzle(string id) { if(puzzles!=null) foreach(var pz in puzzles) if(pz && pz.id==id) return pz; return null; }
    public SaveData Snapshot() {
        var d=new SaveData();
        if(puzzles!=null) foreach(var pz in puzzles) if(pz && pz.State==PuzzleState.Solved) d.solvedPuzzles.Add(pz.id);
        if(zoneGates!=null) foreach(var g in zoneGates) if(g && g.Unlocked) d.unlockedZones.Add(g.zoneId);
        d.lookSensitivity=player?player.lookSensitivity:.13f; d.masterVolume=AudioListener.volume;
        return d;
    }
    public bool Save() { saves++; return SaveSystem.Save(saveSlot,Snapshot()); }
    // Switch save slot (tests use their own slot so the player's save is never touched).
    public void UseSaveSlot(string slot,bool loadNow) { if(loadNow) LoadFromSlot(slot); else saveSlot=slot; }
    public SaveSystem.LoadStatus LoadFromSlot(string slot) {
        Hook(); saveSlot=slot; loading=true;
        ResetLevel();
        lastLoadStatus=SaveSystem.Load(slot,out var data);
        foreach(var id in data.solvedPuzzles) { var pz=FindPuzzle(id); if(pz) pz.Solve(true); else Debug.LogWarning("OOPS_SAVE unknown puzzle id ignored: "+id); }
        if(zoneGates!=null) foreach(var g in zoneGates) if(g && (data.unlockedZones.Contains(g.zoneId)||g.RequirementsMet)) g.Unlock(true);
        if(player) player.lookSensitivity=data.lookSensitivity;
        AudioListener.volume=data.masterVolume;
        loading=false;
        RefreshProgress(true,false);
        if(player && (lastLoadStatus==SaveSystem.LoadStatus.Corrupt||lastLoadStatus==SaveSystem.LoadStatus.WrongVersion))
            player.Notify("อ่านเซฟไม่ได้ ("+lastLoadStatus+") — เริ่มเกมใหม่อย่างปลอดภัย");
        Debug.Log("OOPS_SAVE load "+slot+" status="+lastLoadStatus+" solved="+SolvedPuzzles);
        return lastLoadStatus;
    }
    public void ApplySettings(float look,float volume) {
        if(player) player.lookSensitivity=Mathf.Clamp(look,.02f,1f);
        AudioListener.volume=Mathf.Clamp01(volume);
        Save();
    }
    public int SolvedPuzzles { get { int n=0; if(puzzles!=null) foreach(var pz in puzzles) if(pz && pz.State==PuzzleState.Solved) n++; return n; } }
    // The puzzle whose area BLOB is standing in (null outside puzzle areas).
    public PuzzleObjective CurrentPuzzle {
        get {
            if(puzzles==null || !player) return null;
            Vector3 p=player.transform.position+Vector3.up*.5f;
            foreach(var pz in puzzles) if(pz && pz.area.Contains(p)) return pz;
            return null;
        }
    }
    // Thai objective panel text; also read by the P04 test to check the UI matches the real puzzle state.
    public string PuzzlePanelText() {
        var pz=CurrentPuzzle;
        string head="ฟื้นฟูโลก "+SolvedPuzzles+" / "+(puzzles!=null?puzzles.Length:0);
        return pz?head+"\nปริศนา: "+pz.titleTh+"\n"+pz.StatusTh:head+"\nไปที่โรงงานหรือนาข้าวเพื่อแก้ปริศนา";
    }
    // Consumed sources come back on restart, and on respawn when they opt in (avoids softlocks).
    public void RestoreSources(bool all) {
        if(sources==null) return;
        foreach(var s in sources) if(s && s.kind==BlobInteraction.Source && (all||s.restoreOnRespawn)) s.Restore();
    }
    List<BlobProperty> hudProperties;
    List<BlobProperty> HudProperties() {
        if(hudProperties!=null) return hudProperties;
        hudProperties=new List<BlobProperty>();
        foreach(var s in sources) if(s && s.property!=BlobProperty.None && !hudProperties.Contains(s.property)) hudProperties.Add(s.property);
        hudProperties.Sort();
        return hudProperties;
    }
    void EnsureStyles() {
        if(title!=null)return;
        white=Texture2D.whiteTexture;
        // Thai-capable OS font (Windows ships Leelawadee UI / Tahoma); falls back to the default font elsewhere.
        var thai=Font.CreateDynamicFontFromOSFont(new[]{"Leelawadee UI","Tahoma","Arial"},16);
        title=new GUIStyle(GUI.skin.label){font=thai,fontSize=24,fontStyle=FontStyle.Bold,wordWrap=true};
        title.normal.textColor=Color.white;
        body=new GUIStyle(GUI.skin.label){font=thai,fontSize=16,wordWrap=true}; body.normal.textColor=new Color(.88f,.94f,.98f);
        small=new GUIStyle(body){fontSize=13};
    }
    void Panel(Rect r) {
        GUI.color=new Color(.025f,.045f,.065f,.92f); GUI.DrawTexture(r,white); GUI.color=Color.white;
    }
    string SlotLine(string slot,string key,BlobProperty prop) {
        if(prop==BlobProperty.None) return slot+" ["+key+"]: ว่าง";
        var d=player.powers?player.powers.Def(prop):null;
        return slot+" ["+key+"]: "+player.NameTh(prop)+(d?"  —  "+d.hintTh:"");
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
        if(puzzles!=null && puzzles.Length>0) {
            var pz=CurrentPuzzle;
            Panel(new Rect(w-464,142,440,118));
            GUI.Label(new Rect(w-450,148,415,108),PuzzlePanelText(),small);
            if(pz && pz.State!=PuzzleState.Locked) {
                GUI.color=Color.gray; GUI.DrawTexture(new Rect(w-450,248,415,5),white);
                GUI.color=pz.State==PuzzleState.Solved?new Color(.3f,1,.5f):new Color(1,.8f,.3f);
                GUI.DrawTexture(new Rect(w-450,248,415*pz.Progress01,5),white); GUI.color=Color.white;
            }
        }
        bool hasSources=sources!=null && sources.Length>0;
        if(!overview && hasSources) {
            GUI.Label(new Rect(w/2-12,h/2-16,30,30),"+",title);
            // Status panel sits below the crosshair so it never covers the world-space target marker above the source.
            Panel(new Rect(w/2-200,h/2+40,400,64));
            GUI.Label(new Rect(w/2-188,h/2+45,380,24),player.targetStatus,body);
            GUI.color=Color.gray; GUI.DrawTexture(new Rect(w/2-184,h/2+77,368,6),white);
            GUI.color=player.target?BlobPlayer.PropertyColor(player.target.property):Color.white;
            GUI.DrawTexture(new Rect(w/2-184,h/2+77,368*Mathf.Clamp01(player.absorbProgress),6),white); GUI.color=Color.white;
        }
        if(hasSources) {
            // Thai power panel: primary / secondary slots, the active combination and the swap key.
            Panel(new Rect(24,h-208,640,184));
            GUI.Label(new Rect(40,h-200,600,26),"พลังที่ใช้งาน (สูงสุด "+PowerSet.MaxActive+")",body);
            var props=HudProperties();
            for(int i=0;i<props.Count;i++) {
                bool primary=player.property==props[i], second=player.secondary==props[i];
                GUI.color=primary?BlobPlayer.PropertyColor(props[i]):second?Color.Lerp(BlobPlayer.PropertyColor(props[i]),Color.gray,.5f):new Color(.35f,.39f,.43f);
                GUI.DrawTexture(new Rect(330+i*105,h-190,95,primary?6:3),white);
                GUI.color=Color.white;
                GUI.Label(new Rect(330+i*105,h-182,100,22),player.NameTh(props[i])+(primary?" (หลัก)":second?" (เสริม)":""),small);
            }
            GUI.Label(new Rect(40,h-158,610,24),SlotLine("พลังหลัก","F",player.property),small);
            GUI.Label(new Rect(40,h-134,610,24),SlotLine("พลังเสริม","G",player.secondary),small);
            var combo=player.powers?player.powers.ActiveCombo:null;
            string comboLine=player.secondary==BlobProperty.None?"คอมโบ: ต้องมี 2 พลัง":
                combo?"คอมโบ: "+combo.nameTh+"  —  "+combo.howTh:"คอมโบ: คู่นี้ผสมกันไม่ได้ (ใช้แยกกัน)";
            GUI.color=combo?new Color(1,.9f,.5f):Color.white;
            GUI.Label(new Rect(40,h-108,610,24),comboLine,small);
            GUI.color=Color.white;
            if(player.Has(BlobProperty.Pressure) && player.powers) {
                float cd=player.powers.PressureCooldown01;
                GUI.Label(new Rect(40,h-82,160,22),cd>0?"แรงดัน: ชาร์จ":"แรงดัน: พร้อม",small);
                GUI.color=Color.gray; GUI.DrawTexture(new Rect(170,h-72,200,5),white);
                GUI.color=BlobPlayer.PropertyColor(BlobProperty.Pressure); GUI.DrawTexture(new Rect(170,h-72,200*(1-cd),5),white); GUI.color=Color.white;
            }
            GUI.Label(new Rect(40,h-56,610,24),"Q: สลับพลังหลัก/เสริม  •  ดูดพลังใหม่: พลังเก่าสุดจะถูกทิ้ง",small);
        }
        Panel(new Rect(w-584,h-112,560,88));
        GUI.Label(new Rect(w-568,h-104,530,72),"WASD move  |  SPACE jump  |  Mouse look\n"+(hasSources?"E hold absorb  |  F / G power  |  Q swap  |  ":"")+"R respawn\nT restart  |  ESC pause  |  TAB overview",small);
        if(!string.IsNullOrEmpty(player.feedback)) {
            Panel(new Rect(w/2-250,145,500,50)); GUI.Label(new Rect(w/2-238,155,480,35),player.feedback,body);
        }
        if(player.paused || finished) {
            Panel(new Rect(w/2-240,h/2-130,480,250));
            GUI.Label(new Rect(w/2-214,h/2-110,440,40),finished?"DEMO COMPLETE":"PAUSED",title);
            GUI.Label(new Rect(w/2-214,h/2-62,420,65),finished?"Demo route complete.\nTime: "+Mathf.FloorToInt(elapsed)+"s / retries: "+deaths:"Click Resume, then use the mouse to aim.\nAbsorb up to two properties.",body);
            if(GUI.Button(new Rect(w/2-210,h/2+20,200,42),finished?"REPLAY LEVEL":"RESUME")) {
                if(finished)Restart(); player.Resume();
            }
            if(GUI.Button(new Rect(w/2+10,h/2+20,200,42),"RESTART LEVEL")) { Restart();player.Resume(); }
        } else if(!player.testMode && Cursor.lockState!=CursorLockMode.Locked) {
            GUI.Label(new Rect(w/2-235,h/2+120,500,35),"CLICK THE GAME VIEW TO CAPTURE THE MOUSE",body);
        }
        GUI.matrix=Matrix4x4.identity;
    }
}
}
