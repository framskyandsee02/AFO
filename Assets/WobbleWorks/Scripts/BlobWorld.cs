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
        // Pause menu closed with ESC instead of "เล่นต่อ": still persist the settings that were changed.
        if(player && !player.paused && uiLook>=0) { ApplySettings(uiLook,uiVolume,uiAmbience); uiLook=-1; confirmNewGame=false; }
        if(!player || player.paused || finished) return;
        elapsed+=Time.deltaTime;
        Vector3 p=player.transform.position;
        foreach(var zone in zones) {
            if(!zone.Contains(p+Vector3.up*.2f)) continue;
            switch(zone.kind) {
                case BlobInteraction.Checkpoint:
                    if(zone.index>checkpointIndex) {
                        checkpointIndex=zone.index; checkpoint=zone.respawn;
                        player.Notify("บันทึกจุดเช็กพอยต์ "+zone.index+" แล้ว",3);
                    }
                    break;
                case BlobInteraction.Area:
                    objective=zone.label;
                    break;
                case BlobInteraction.Exit:
                    if(finished) break;
                    finished=true; player.completed=true; player.Notify("จบเดโม! ยอดเยี่ยมมาก",4);
                    Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
                    Debug.Log("OOPS_LEVEL_COMPLETE time="+elapsed+" deaths="+deaths);
                    break;
            }
        }
    }
    public void RecordProperty(BlobProperty p) { if(p!=BlobProperty.None) propertyMask|=1<<(int)p; }
    public event System.Action Respawned;
    public event System.Action<ZoneGate> ZoneUnlocked;
    public void Respawn() {
        if(resetting)return; resetting=true;
        player.Teleport(checkpoint);
        player.ClearProperties();
        if(player.powers) player.powers.ResetState();
        RestoreSources(false);
        deaths++; player.Notify("กลับมาที่จุดเช็กพอยต์แล้ว");
        resetting=false; Respawned?.Invoke();
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
            if(g && !g.Unlocked && g.RequirementsMet) { g.Unlock(instant); if(!instant) { if(player) player.Notify(g.zoneNameTh+" เปิดแล้ว! กลับไปที่ฮับเพื่อไปต่อ",4); ZoneUnlocked?.Invoke(g); } }
        if(hub) hub.Refresh(instant);
        if(save) Save();
    }
    public PuzzleObjective FindPuzzle(string id) { if(puzzles!=null) foreach(var pz in puzzles) if(pz && pz.id==id) return pz; return null; }
    public SaveData Snapshot() {
        var d=new SaveData();
        if(puzzles!=null) foreach(var pz in puzzles) if(pz && pz.State==PuzzleState.Solved) d.solvedPuzzles.Add(pz.id);
        if(zoneGates!=null) foreach(var g in zoneGates) if(g && g.Unlocked) d.unlockedZones.Add(g.zoneId);
        d.lookSensitivity=player?player.lookSensitivity:.13f; d.masterVolume=AudioListener.volume; d.ambienceVolume=ambience?ambience.ambienceVolume:1;
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
        if(ambience) ambience.ambienceVolume=data.ambienceVolume;
        loading=false;
        RefreshProgress(true,false);
        if(player && (lastLoadStatus==SaveSystem.LoadStatus.Corrupt||lastLoadStatus==SaveSystem.LoadStatus.WrongVersion))
            player.Notify("อ่านเซฟไม่ได้ ("+lastLoadStatus+") — เริ่มเกมใหม่อย่างปลอดภัย");
        Debug.Log("OOPS_SAVE load "+slot+" status="+lastLoadStatus+" solved="+SolvedPuzzles);
        return lastLoadStatus;
    }
    public void ApplySettings(float look,float volume) => ApplySettings(look,volume,ambience?ambience.ambienceVolume:1);
    public void ApplySettings(float look,float volume,float ambienceVolume) {
        if(player) player.lookSensitivity=Mathf.Clamp(look,.02f,1f);
        AudioListener.volume=Mathf.Clamp01(volume);
        if(ambience) ambience.ambienceVolume=Mathf.Clamp01(ambienceVolume);
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
    // ---- Thai UI (P06) ----
    // All HUD text uses the bundled Noto Sans Thai (SIL OFL 1.1, Assets/OopsPlanet/Fonts); no OS fonts.
    [Header("UI")]
    public Font uiFont;
    public ZoneAmbience ambience;
    GUIStyle buttonStyle;
    bool confirmNewGame;
    float uiLook=-1, uiVolume, uiAmbience;
    void EnsureStyles() {
        if(title!=null)return;
        white=Texture2D.whiteTexture;
        title=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=26,fontStyle=FontStyle.Bold,wordWrap=true};
        title.normal.textColor=Color.white;
        body=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=19,wordWrap=true}; body.normal.textColor=new Color(.9f,.95f,.98f);
        small=new GUIStyle(body){fontSize=16};
        buttonStyle=new GUIStyle(GUI.skin.button){font=uiFont,fontSize=18};
    }
    void Panel(Rect r) {
        GUI.color=new Color(.02f,.035f,.055f,.88f); GUI.DrawTexture(r,white); GUI.color=Color.white;
    }
    string SlotLine(string slot,string key,BlobProperty prop) {
        if(prop==BlobProperty.None) return slot+" ["+key+"]: ว่าง";
        var d=player.powers?player.powers.Def(prop):null;
        return slot+" ["+key+"]: "+player.NameTh(prop)+(d?"  —  "+d.hintTh:"");
    }
    // Thai text for BLOB's absorb target status (the status codes themselves stay stable for tests).
    public string StatusTh() {
        switch(player.targetStatus) {
            case BlobPlayer.Absorbable: return "กด E ค้างเพื่อดูดซับพลัง"+(player.target?player.NameTh(player.target.property):"");
            case BlobPlayer.NotAbsorbable: return "ดูดซับไม่ได้ — วัตถุนี้ไม่มีพลัง";
            case "OUT OF RANGE": return "ไกลเกินไป — เดินเข้าไปใกล้อีกนิด";
            case "AIM AT TARGET": return "เล็งให้ตรงเป้า";
            case "BLOCKED": return "มีสิ่งกีดขวาง — หามุมใหม่";
            default: return "เล็งไปที่วัตถุที่มีพลังเพื่อดูดซับ";
        }
    }
    public const string ControlsTh="WASD เดิน  •  SPACE กระโดด / ค้างเพื่อเด้ง  •  เมาส์ มอง\nE ค้าง ดูดซับ  •  F พลังหลัก  •  G พลังเสริม  •  Q สลับพลัง\nR กลับจุดเช็กพอยต์  •  ESC หยุดเกม / ตั้งค่า  •  TAB มุมมองรวม";
    void OnGUI() {
        if(!player)return; EnsureStyles();
        float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
        float w=Screen.width/scale,h=Screen.height/scale;
        Panel(new Rect(24,22,720,112));
        GUI.Label(new Rect(40,28,690,38),levelTitle,title);
        GUI.Label(new Rect(40,70,690,62),objective,body);
        Panel(new Rect(w-264,22,240,112));
        GUI.Label(new Rect(w-248,30,220,28),"จุดเช็กพอยต์  "+checkpointIndex+" / "+checkpointCount,body);
        GUI.Label(new Rect(w-248,62,220,70),"เวลา "+Mathf.FloorToInt(elapsed)+" วิ  •  รีสปอน "+Mathf.Max(0,deaths)+"\nTAB: มุมมองรวม",small);
        if(puzzles!=null && puzzles.Length>0) {
            var pz=CurrentPuzzle;
            Panel(new Rect(w-484,146,460,128));
            GUI.Label(new Rect(w-470,152,435,112),PuzzlePanelText(),small);
            if(pz && pz.State!=PuzzleState.Locked) {
                GUI.color=Color.gray; GUI.DrawTexture(new Rect(w-470,262,435,6),white);
                GUI.color=pz.State==PuzzleState.Solved?new Color(.3f,1,.5f):new Color(1,.8f,.3f);
                GUI.DrawTexture(new Rect(w-470,262,435*pz.Progress01,6),white); GUI.color=Color.white;
            }
        }
        bool hasSources=sources!=null && sources.Length>0;
        bool menu=player.paused||finished;
        if(!overview && hasSources && !menu) {
            GUI.Label(new Rect(w/2-12,h/2-18,30,34),"+",title);
            // Status panel sits below the crosshair so it never covers the world-space target marker above the source.
            Panel(new Rect(w/2-230,h/2+40,460,70));
            GUI.color=player.targetStatus==BlobPlayer.Absorbable?new Color(1,.95f,.6f):Color.white;
            GUI.Label(new Rect(w/2-216,h/2+45,435,30),StatusTh(),body);
            GUI.color=Color.gray; GUI.DrawTexture(new Rect(w/2-214,h/2+84,428,7),white);
            GUI.color=player.target?BlobPlayer.PropertyColor(player.target.property):Color.white;
            GUI.DrawTexture(new Rect(w/2-214,h/2+84,428*Mathf.Clamp01(player.absorbProgress),7),white); GUI.color=Color.white;
        }
        if(hasSources) {
            // Thai power panel: primary / secondary slots, the active combination and the swap key.
            Panel(new Rect(24,h-222,700,198));
            GUI.Label(new Rect(40,h-214,330,28),"พลังที่ใช้งาน (สูงสุด "+PowerSet.MaxActive+")",body);
            var props=HudProperties();
            for(int i=0;i<props.Count;i++) {
                bool primary=player.property==props[i], second=player.secondary==props[i];
                GUI.color=primary?BlobPlayer.PropertyColor(props[i]):second?Color.Lerp(BlobPlayer.PropertyColor(props[i]),Color.gray,.5f):new Color(.35f,.39f,.43f);
                GUI.DrawTexture(new Rect(360+i*115,h-206,105,primary?7:3),white);
                GUI.color=Color.white;
                GUI.Label(new Rect(360+i*115,h-198,112,26),player.NameTh(props[i])+(primary?" (หลัก)":second?" (เสริม)":""),small);
            }
            GUI.Label(new Rect(40,h-168,670,26),SlotLine("พลังหลัก","F",player.property),small);
            GUI.Label(new Rect(40,h-142,670,26),SlotLine("พลังเสริม","G",player.secondary),small);
            var combo=player.powers?player.powers.ActiveCombo:null;
            string comboLine=player.secondary==BlobProperty.None?"คอมโบ: ต้องมี 2 พลัง":
                combo?"คอมโบ: "+combo.nameTh+"  —  "+combo.howTh:"คอมโบ: คู่นี้ผสมกันไม่ได้ (ใช้แยกกัน)";
            GUI.color=combo?new Color(1,.9f,.5f):Color.white;
            GUI.Label(new Rect(40,h-116,670,26),comboLine,small);
            GUI.color=Color.white;
            if(player.Has(BlobProperty.Pressure) && player.powers) {
                float cd=player.powers.PressureCooldown01;
                GUI.Label(new Rect(40,h-88,170,24),cd>0?"แรงดัน: กำลังชาร์จ":"แรงดัน: พร้อมใช้",small);
                GUI.color=Color.gray; GUI.DrawTexture(new Rect(200,h-76,220,6),white);
                GUI.color=BlobPlayer.PropertyColor(BlobProperty.Pressure); GUI.DrawTexture(new Rect(200,h-76,220*(1-cd),6),white); GUI.color=Color.white;
            }
            GUI.Label(new Rect(40,h-60,670,26),"Q: สลับพลังหลัก/เสริม  •  ดูดพลังใหม่ พลังที่เก่าที่สุดจะหลุด",small);
        }
        Panel(new Rect(w-664,h-124,640,100));
        GUI.Label(new Rect(w-650,h-118,620,92),hasSources?ControlsTh:"WASD เดิน  •  SPACE กระโดด  •  เมาส์ มอง\nR กลับจุดเช็กพอยต์  •  ESC หยุดเกม / ตั้งค่า  •  TAB มุมมองรวม",small);
        if(!string.IsNullOrEmpty(player.feedback)) {
            Panel(new Rect(w/2-260,146,520,54)); GUI.Label(new Rect(w/2-246,154,495,42),player.feedback,body);
        }
        if(finished) FinishedMenu(w,h);
        else if(player.paused) PauseMenu(w,h);
        else if(!player.testMode && Cursor.lockState!=CursorLockMode.Locked) {
            Panel(new Rect(w/2-260,h/2+128,520,40));
            GUI.Label(new Rect(w/2-246,h/2+133,495,32),"คลิกที่หน้าจอเกมเพื่อควบคุมกล้องด้วยเมาส์",body);
        }
        GUI.matrix=Matrix4x4.identity;
    }
    void PauseMenu(float w,float h) {
        if(uiLook<0) { uiLook=player.lookSensitivity; uiVolume=AudioListener.volume; uiAmbience=ambience?ambience.ambienceVolume:1; }
        Rect r=new Rect(w/2-300,h/2-250,600,500); Panel(r);
        GUI.Label(new Rect(r.x+30,r.y+18,540,40),"หยุดเกมชั่วคราว",title);
        if(GUI.Button(new Rect(r.x+30,r.y+70,540,44),"เล่นต่อ",buttonStyle)) { CloseMenu(); return; }
        if(GUI.Button(new Rect(r.x+30,r.y+122,540,44),"กลับจุดเช็กพอยต์ล่าสุด (ลองใหม่)",buttonStyle)) { Respawn(); CloseMenu(); return; }
        if(GUI.Button(new Rect(r.x+30,r.y+174,540,44),confirmNewGame?"กดอีกครั้งเพื่อยืนยัน: ล้างความคืบหน้าทั้งหมด":"เริ่มเกมใหม่",buttonStyle)) {
            if(confirmNewGame) { Restart(); CloseMenu(); return; }
            confirmNewGame=true;
        }
        GUI.Label(new Rect(r.x+30,r.y+236,540,30),"ตั้งค่า (บันทึกอัตโนมัติ)",body);
        uiVolume=Slider(r.x+30,r.y+272,"ระดับเสียงรวม",uiVolume,0,1);
        uiAmbience=Slider(r.x+30,r.y+322,"เสียงบรรยากาศ",uiAmbience,0,1);
        uiLook=Slider(r.x+30,r.y+372,"ความไวเมาส์",uiLook,.03f,.4f);
        AudioListener.volume=uiVolume; player.lookSensitivity=uiLook; if(ambience) ambience.ambienceVolume=uiAmbience;
        GUI.Label(new Rect(r.x+30,r.y+426,540,60),"พลังแต่ละแบบมีชิ้นส่วนบนตัว BLOB ต่างกัน: เกือกม้า = แม่เหล็ก, วาล์ว = แรงดัน, สปริง = เด้ง",small);
    }
    float Slider(float x,float y,string label,float value,float min,float max) {
        GUI.Label(new Rect(x,y,200,30),label,small);
        float v=GUI.HorizontalSlider(new Rect(x+210,y+10,260,20),value,min,max);
        GUI.Label(new Rect(x+480,y,70,30),Mathf.RoundToInt((v-min)/(max-min)*100)+"%",small);
        return v;
    }
    void CloseMenu() {
        confirmNewGame=false;
        if(uiLook>=0) ApplySettings(uiLook,uiVolume,uiAmbience);
        uiLook=-1;
        player.Resume();
    }
    void FinishedMenu(float w,float h) {
        Rect r=new Rect(w/2-280,h/2-150,560,300); Panel(r);
        GUI.Label(new Rect(r.x+30,r.y+20,500,40),"จบเดโมแล้ว!",title);
        GUI.Label(new Rect(r.x+30,r.y+72,500,110),"ใช้เวลา "+Mathf.FloorToInt(elapsed)+" วินาที  •  รีสปอน "+Mathf.Max(0,deaths)+" ครั้ง\nฟื้นฟูโลก "+SolvedPuzzles+" / "+(puzzles!=null?puzzles.Length:0)+"\nขอบคุณที่ช่วย BLOB ซ่อมดาว OOPS!",body);
        if(GUI.Button(new Rect(r.x+30,r.y+210,500,48),confirmNewGame?"กดอีกครั้งเพื่อยืนยัน: เริ่มเกมใหม่":"เล่นใหม่ตั้งแต่ต้น",buttonStyle)) {
            if(confirmNewGame) { confirmNewGame=false; Restart(); player.Resume(); } else confirmNewGame=true;
        }
    }
}
}
