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
    // Frame cap (P07): vSync is on in the Quality settings; this cap applies where vSync is not active
    // (driver forces it off, headless/batch runs) so the game never free-runs at thousands of fps.
    public const int MaxFrameRate=240;
    void Awake() { checkpoint=spawn; if(Application.targetFrameRate<=0) Application.targetFrameRate=MaxFrameRate; }
    void Update() {
        // Pause menu closed with ESC instead of "เล่นต่อ": still persist the settings that were changed.
        if(player && !player.paused && uiLook>=0) { ApplySettings(uiLook,uiVolume,uiAmbience); uiLook=-1; confirmNewGame=false; }
        if(!player || player.paused || finished) return;
        if(!player.testMode && UnityEngine.InputSystem.Keyboard.current!=null && UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame && Elements) journalOpen=!journalOpen;
        elapsed+=Time.deltaTime;
        // v2: puzzle-critical sources regenerate on a timer (design §3.3 point 5).
        if(sources!=null) foreach(var s in sources) if(s && s.RegenDue) s.Restore();
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
                    CompleteDemo("จบเดโม! ยอดเยี่ยมมาก");
                    break;
            }
        }
    }
    public void CompleteDemo(string message) {
        if(finished) return;
        finished=true; player.completed=true; player.Notify(message,4);
        if(!player.testMode) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        Debug.Log("OOPS_LEVEL_COMPLETE time="+elapsed+" deaths="+deaths);
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
        WorldMaterial.ResetAll();
        var steam=GetComponent<SteamSystem>(); if(steam) steam.Clear();
        MagnetAnchor.ClearTemporary();
        FerryRaft.ResetAll();
        foreach(var c in Object.FindObjectsByType<ElementCarrier>(FindObjectsSortMode.None)) c.Clear();
        if(Elements) Elements.discovered.Clear();
        if(ship) ship.ResetShip();
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
    [Tooltip("v2: crashed ship that collects parts; the demo ends when it is fully repaired.")]
    public ShipRepair ship;
    public SaveSystem.LoadStatus lastLoadStatus;
    public int saves;
    bool loading, hooked;
    void Start() {
        Hook();
        if(autoLoad) LoadFromSlot(saveSlot); else RefreshProgress(true,false);
        // Built-player smoke tests: OopsPlanet.exe -oopsAutoTest route,puzzle,... (see OopsBuildTestRunner).
        if(OopsBuildTestRunner.Requested(out var steps)) gameObject.AddComponent<OopsBuildTestRunner>().steps=steps;
    }
    void Hook() {
        if(hooked || puzzles==null) return; hooked=true;
        foreach(var pz in puzzles) if(pz) pz.Solved+=o=>{ if(!loading) RefreshProgress(false,true); };
        if(Elements) Elements.Discovered+=c=>{ if(!loading) Save(); };
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
        if(Elements) d.discoveredCombos.AddRange(Elements.discovered);
        if(ship) ship.WriteTo(d);
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
        if(Elements) foreach(var id in data.discoveredCombos) if(!Elements.discovered.Contains(id)) Elements.discovered.Add(id);
        if(ship) ship.ReadFrom(data);
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
    public int SolvedPuzzles { get { int n=0; if(puzzles!=null) foreach(var pz in puzzles) if(pz && !pz.isSecret && pz.State==PuzzleState.Solved) n++; return n; } }
    public int MainPuzzleCount { get { int n=0; if(puzzles!=null) foreach(var pz in puzzles) if(pz && !pz.isSecret) n++; return n; } }
    // The puzzle whose area BLOB is standing in (null outside puzzle areas).
    public PuzzleObjective CurrentPuzzle {
        get {
            if(puzzles==null || !player) return null;
            Vector3 p=player.transform.position+Vector3.up*.5f;
            foreach(var pz in puzzles) if(pz && !pz.isSecret && pz.area.Contains(p)) return pz;
            return null;
        }
    }
    // Thai objective panel text; also read by the P04 test to check the UI matches the real puzzle state.
    public string PuzzlePanelText() {
        var pz=CurrentPuzzle;
        string head="ฟื้นฟูโลก "+SolvedPuzzles+" / "+MainPuzzleCount+(ship?"  •  ชิ้นส่วนยาน "+(ship.installed.Count+ship.collected.Count)+" / "+(ship.parts!=null?ship.parts.Length:0)+" (ติดตั้ง "+ship.installed.Count+"/"+ship.partsToFinish+")":"");
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
    public const string ControlsTh="WASD เดิน  •  SPACE กระโดด / ค้างเพื่อเด้ง  •  เมาส์ มอง  •  คลิกขวาค้าง ดมพลัง\nE ค้าง ดูดซับ  •  F พลังหลัก (+ธาตุเสริม)  •  G พลังเสริม  •  Q สลับพลัง  •  J สมุดพลัง\nR กลับจุดเช็กพอยต์  •  ESC หยุดเกม / ตั้งค่า  •  TAB มุมมองรวม";
    // v2 power journal (J): the 30 ordered combo cells, discovered ones named.
    public bool journalOpen;
    BlobElementPowers elementsCache; bool elementsLooked;
    public BlobElementPowers Elements { get { if(!elementsLooked && player) { elementsCache=player.GetComponent<BlobElementPowers>(); elementsLooked=true; } return elementsCache; } }
    void OnGUI() {
        if(!player)return; EnsureStyles();
        float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
        float w=Screen.width/scale,h=Screen.height/scale;
        if(finished) FinishedMenu(w,h);
        else if(player.paused) PauseMenu(w,h);
        else if(journalOpen && Elements) JournalPanel(w,h);
        else {
            bool hasSources=sources!=null && sources.Length>0;
            if(overview) {
                Panel(new Rect(24,24,590,132));
                GUI.Label(new Rect(40,30,558,38),levelTitle,title);
                GUI.Label(new Rect(40,72,558,72),objective,body);
                Panel(new Rect(24,h-148,660,124));
                GUI.Label(new Rect(40,h-140,628,108),ControlsTh,small);
            }
            // Progress stays at the edge; detailed instructions belong to the overview.
            var pz=CurrentPuzzle;
            float progressW=360, px=w-progressW-24;
            Panel(new Rect(px,24,progressW,pz?100:40));
            string progress="ฟื้นฟู "+SolvedPuzzles+"/"+MainPuzzleCount;
            if(ship) progress+="   ยาน "+ship.installed.Count+"/"+ship.partsToFinish;
            GUI.Label(new Rect(px+12,30,progressW-24,28),progress+"   [TAB] รายละเอียด",small);
            if(pz) {
                GUI.Label(new Rect(px+12,59,progressW-24,28),pz.titleTh,body);
                GUI.Label(new Rect(px+12,87,progressW-24,28),pz.StatusTh,small);
                GUI.color=new Color(.25f,.3f,.35f); GUI.DrawTexture(new Rect(px+12,119,progressW-24,3),white);
                GUI.color=pz.State==PuzzleState.Solved?new Color(.3f,1,.5f):new Color(1,.8f,.3f);
                GUI.DrawTexture(new Rect(px+12,119,(progressW-24)*pz.Progress01,3),white); GUI.color=Color.white;
            }
            if(!overview && hasSources) {
                // Quiet reticle, with an absorb prompt only when there is a source to act on.
                GUI.color=new Color(1,1,1,.75f);
                GUI.DrawTexture(new Rect(w/2-2,h/2-2,4,4),white); GUI.color=Color.white;
                if(player.target && player.targetStatus!=BlobPlayer.NotAbsorbable) {
                    Rect prompt=new Rect(w/2-190,h/2+34,380,42);
                    Panel(prompt);
                    GUI.Label(new Rect(prompt.x+12,prompt.y+5,356,30),StatusTh(),small);
                    if(player.absorbProgress>0) {
                        GUI.color=BlobPlayer.PropertyColor(player.target.property);
                        GUI.DrawTexture(new Rect(prompt.x,prompt.yMax-3,prompt.width*Mathf.Clamp01(player.absorbProgress),3),white); GUI.color=Color.white;
                    }
                }
                // Only the two equipped powers are shown, without the six-power catalogue.
                float x=24,y=h-126;
                Panel(new Rect(x,y,380,102));
                DrawPowerSlot(new Rect(x+12,y+8,172,30),"F",player.property);
                DrawPowerSlot(new Rect(x+196,y+8,172,30),"G",player.secondary);
                var combo=player.powers?player.powers.ActiveCombo:null;
                var cell=Elements?Elements.ActiveCell:null;
                string comboName=cell!=null?cell.nameTh:combo?combo.nameTh:"";
                GUI.color=new Color(1,.9f,.6f);
                GUI.Label(new Rect(x+12,y+39,356,28),comboName,small); GUI.color=Color.white;
                GUI.Label(new Rect(x+12,y+69,356,26),"Q สลับ   •   J สมุดพลัง   •   ESC เมนู",small);
                if(player.Has(BlobProperty.Pressure) && player.powers && player.powers.PressureCooldown01>0) {
                    GUI.color=BlobPlayer.PropertyColor(BlobProperty.Pressure);
                    GUI.DrawTexture(new Rect(x,y+99,380*(1-player.powers.PressureCooldown01),3),white); GUI.color=Color.white;
                }
            }
            if(!string.IsNullOrEmpty(player.feedback)) {
                Panel(new Rect(w/2-260,154,520,48));
                GUI.Label(new Rect(w/2-246,160,492,36),player.feedback,body);
            }
            if(!player.testMode && Cursor.lockState!=CursorLockMode.Locked) {
                GUI.Label(new Rect(w/2-210,h-54,420,30),"คลิกหน้าจอเพื่อควบคุมกล้อง",small);
            }
        }
        GUI.matrix=Matrix4x4.identity;
    }
    void DrawPowerSlot(Rect r,string key,BlobProperty prop) {
        GUI.color=prop==BlobProperty.None?new Color(.4f,.45f,.5f):BlobPlayer.PropertyColor(prop);
        GUI.DrawTexture(new Rect(r.x,r.y+5,3,22),white); GUI.color=Color.white;
        GUI.Label(new Rect(r.x+10,r.y,r.width-10,r.height),"["+key+"] "+(prop==BlobProperty.None?"ว่าง":player.NameTh(prop)),body);
    }

    void JournalPanel(float w,float h) {
        var table=player.powers&&player.powers.set?player.powers.set.table:null; if(!table) return;
        Rect r=new Rect(w/2-470,110,940,560); Panel(r);
        GUI.Label(new Rect(r.x+24,r.y+10,600,36),"สมุดพลัง  "+Elements.discovered.Count+" / 30  (กด J เพื่อปิด)",title);
        GUI.Label(new Rect(r.x+24,r.y+48,900,26),"แถว = พลังหลัก (F, ท่าทาง)  •  คอลัมน์ = พลังเสริม (ธาตุ)  •  ช่องที่ยังไม่ค้นพบแสดงเป็น ???",small);
        var powers=ComboTable.Powers; float cw=140, ch=74, x0=r.x+110, y0=r.y+80;
        for(int j=0;j<powers.Length;j++) GUI.Label(new Rect(x0+j*cw,y0,cw,24),player.NameTh(powers[j]),small);
        for(int i=0;i<powers.Length;i++) {
            GUI.Label(new Rect(r.x+20,y0+28+i*ch,90,ch),player.NameTh(powers[i]),small);
            for(int j=0;j<powers.Length;j++) {
                var c=table.Get(powers[i],powers[j]);
                string t=c==null?"—":Elements.Knows(c.id)?c.nameTh:"???";
                GUI.color=c!=null&&Elements.Knows(c.id)?new Color(1,.92f,.6f):new Color(.7f,.72f,.75f);
                GUI.Label(new Rect(x0+j*cw,y0+28+i*ch,cw-6,ch),t,small); GUI.color=Color.white;
            }
        }
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
        GUI.Label(new Rect(r.x+30,r.y+72,500,110),"ใช้เวลา "+Mathf.FloorToInt(elapsed)+" วินาที  •  รีสปอน "+Mathf.Max(0,deaths)+" ครั้ง\nฟื้นฟูโลก "+SolvedPuzzles+" / "+MainPuzzleCount+"\nขอบคุณที่ช่วย BLOB ซ่อมดาว OOPS!",body);
        if(GUI.Button(new Rect(r.x+30,r.y+210,500,48),confirmNewGame?"กดอีกครั้งเพื่อยืนยัน: เริ่มเกมใหม่":"เล่นใหม่ตั้งแต่ต้น",buttonStyle)) {
            if(confirmNewGame) { confirmNewGame=false; Restart(); player.Resume(); } else confirmNewGame=true;
        }
    }
}
}
