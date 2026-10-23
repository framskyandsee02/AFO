using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace OopsPlanet {
// P05 progression + persistence test. Uses its own save slots only; the player's save (oops_save.json) is
// fingerprinted before/after and must be untouched.
//  Session 1: locked Rice gate, unlock after the Factory, Hub stages, respawn, settings, save written.
//  Session 2 (run after stopping and re-entering Play Mode = a new game session): reload from disk, then
//  corrupt / missing / wrong-version / unknown-id saves fall back safely, and new game clears progress.
// Puzzles are completed through PuzzleObjective.Solve() here; their real solutions are covered by the P04 test.
public class OopsProgressTest : MonoBehaviour {
    [System.Serializable] public class Check { public string name;public bool passed;public string observed; }
    [System.Serializable] public class Report { public string utc;public int session;public List<Check> checks=new List<Check>();public int passed,failed; }
    public int session=1;
    const string Slot="p05_test";
    BlobWorld w; BlobPlayer p; Report report=new Report();
    void CheckThat(string name,bool pass,string observed) {
        report.checks.Add(new Check{name=name,passed=pass,observed=observed});
        if(pass)report.passed++;else report.failed++;
        Debug.Log("OOPS_P05."+session+" "+(pass?"PASS ":"FAIL ")+name+" | "+observed);
    }
    IEnumerator Wait(float seconds) { float until=Time.time+seconds;while(Time.time<until)yield return null; }
    IEnumerator To(Vector3 goal,float timeout=10f) {
        p.testAim=null; float until=Time.time+timeout;
        while(Time.time<until) { Vector3 d=goal-p.transform.position;d.y=0; if(d.magnitude<.25f)break; p.testMove=new Vector2(d.x,d.z).normalized; yield return null; }
        p.testMove=Vector2.zero;
    }
    static string Fingerprint(string path) { if(!File.Exists(path)) return "absent"; var fi=new FileInfo(path); return fi.Length+"@"+fi.LastWriteTimeUtc.Ticks; }
    static string FileText(string slot) { var path=SaveSystem.PathFor(slot); return File.Exists(path)?File.ReadAllText(path):""; }
    static void WriteRaw(string slot,string text) { File.WriteAllText(SaveSystem.PathFor(slot),text); }
    static void DeleteTestSlot(string slot) { foreach(var path in new[]{SaveSystem.PathFor(slot),Path.ChangeExtension(SaveSystem.PathFor(slot),".corrupt.json")}) if(File.Exists(path)) File.Delete(path); }
    IEnumerator Start() {
        w=GetComponent<BlobWorld>();p=w.player;p.testMode=true;p.paused=false;report.session=session;
        report.utc=System.DateTime.UtcNow.ToString("o");
        string mainPath=SaveSystem.PathFor(SaveSystem.DefaultSlot), mainBefore=Fingerprint(mainPath);
        if(session==1) yield return Session1(); else yield return Session2();
        CheckThat("Player save file untouched by the test",Fingerprint(mainPath)==mainBefore,"before="+mainBefore+" after="+Fingerprint(mainPath)+" path="+mainPath);
        Finish();
    }
    IEnumerator Session1() {
        DeleteTestSlot(Slot);
        w.UseSaveSlot(Slot,false); w.Restart(); p.testMode=true;
        yield return Wait(.5f);
        var gate=w.FindPuzzle("factory_gate"); var vent=w.FindPuzzle("factory_turbine"); var rice=w.FindPuzzle("rice_straw");
        var riceGate=w.zoneGates[0]; var hub=w.hub;
        CheckThat("New game: Factory open, Rice Fields locked, Hub stage 0",!riceGate.Unlocked && riceGate.Open01==0 && hub.Stage==0 && !hub.canopySmall.activeSelf && !hub.canopyFull.activeSelf && hub.hubAreaZone.label.Contains("ยังล็อก"),
            "rice="+riceGate.Unlocked+" stage="+hub.Stage);
        p.Teleport(new Vector3(16.5f,.1f,0)); yield return Wait(.2f);
        p.testMove=Vector2.right; yield return Wait(2.5f); p.testMove=Vector2.zero;
        CheckThat("Locked Rice gate blocks the path and tells BLOB why",p.transform.position.x<19.7f && p.feedback.Contains("ยังล็อก"),"x="+p.transform.position.x.ToString("F2")+" feedback="+p.feedback);
        gate.Solve(); yield return Wait(.3f);
        CheckThat("Gate alone does not unlock Rice (needs the whole Factory)",!riceGate.Unlocked && hub.Stage==0,"rice="+riceGate.Unlocked);
        vent.Solve(); yield return Wait(2f);
        CheckThat("Factory restored -> Rice gate unlocks and opens",riceGate.Unlocked && riceGate.Open01>=1 && p.feedback.Contains("เปิดแล้ว"),"open="+riceGate.Open01.ToString("F2")+" feedback="+p.feedback);
        CheckThat("Hub shows stage 1: small canopy, label updated",hub.Stage==1 && hub.canopySmall.activeSelf && !hub.canopyFull.activeSelf && hub.hubAreaZone.label.Contains("เปิดแล้ว"),"stage="+hub.Stage);
        string saved=FileText(Slot);
        CheckThat("Progress saved after unlock",saved.Contains("factory_gate") && saved.Contains("factory_turbine") && saved.Contains("\"rice\""),"save="+saved.Replace("\n"," ").Replace("  ",""));
        yield return To(new Vector3(28,0,0));
        CheckThat("BLOB walks through the opened gate into the Rice Fields",p.transform.position.x>27 && w.checkpointIndex==2,"x="+p.transform.position.x.ToString("F2")+" checkpoint="+w.checkpointIndex);
        yield return To(new Vector3(10,0,0)); yield return Wait(.3f);
        CheckThat("Returning to the Hub announces the change",hub.announcements==1 && p.feedback.Contains("ฮับ"),"announcements="+hub.announcements+" feedback="+p.feedback);
        w.Respawn(); yield return Wait(.3f);
        CheckThat("Respawn keeps unlock and solved puzzles (no regression in session)",riceGate.Unlocked && gate.State==PuzzleState.Solved && vent.State==PuzzleState.Solved && hub.Stage==1,"rice="+riceGate.Unlocked);
        rice.Solve(); yield return Wait(.5f);
        CheckThat("All zones restored -> Hub stage 2: full canopy, grass, complete label",hub.Stage==2 && hub.canopyFull.activeSelf && !hub.canopySmall.activeSelf && hub.stage2Extras[0].activeSelf && hub.hubAreaZone.label.Contains("ครบ"),"stage="+hub.Stage);
        w.ApplySettings(.21f,.6f); yield return null;
        var status=SaveSystem.Load(Slot,out var data);
        CheckThat("Save on disk has 3 puzzles, rice unlocked and settings",status==SaveSystem.LoadStatus.Ok && data.solvedPuzzles.Count==3 && data.unlockedZones.Contains("rice") && Mathf.Abs(data.lookSensitivity-.21f)<.001f && Mathf.Abs(data.masterVolume-.6f)<.001f,
            "status="+status+" solved="+data.solvedPuzzles.Count+" look="+data.lookSensitivity+" vol="+data.masterVolume);
    }
    IEnumerator Session2() {
        var gate=w.FindPuzzle("factory_gate"); var vent=w.FindPuzzle("factory_turbine"); var rice=w.FindPuzzle("rice_straw");
        var riceGate=w.zoneGates[0]; var hub=w.hub;
        var status=w.LoadFromSlot(Slot); p.testMode=true; yield return null;
        CheckThat("New session loads the saved game from disk",status==SaveSystem.LoadStatus.Ok && w.SolvedPuzzles==3 && gate.LastSolveFromSave && rice.LastSolveFromSave,"status="+status+" solved="+w.SolvedPuzzles);
        var riceReaction=rice.GetComponent<PuzzleReaction>(); var gateReaction=gate.GetComponent<PuzzleReaction>();
        var clear=(ClearZoneCondition)rice.conditions[0];
        CheckThat("World state restored instantly (gate open, Rice open, Hub stage 2, water flowing)",
            riceGate.Unlocked && riceGate.Open01>=1 && gateReaction.Blend01>=1 && riceReaction.Blend01>=1 && riceReaction.showWhenSolved[0].activeSelf && hub.Stage==2 && hub.canopyFull.activeSelf,
            "riceOpen="+riceGate.Open01+" gateBlend="+gateReaction.Blend01+" stage="+hub.Stage);
        CheckThat("Cleared straw is not shown back on the platform",clear.Count()==0,"strawInZone="+clear.Count());
        CheckThat("Settings restored",Mathf.Abs(p.lookSensitivity-.21f)<.001f && Mathf.Abs(AudioListener.volume-.6f)<.001f,"look="+p.lookSensitivity+" vol="+AudioListener.volume);
        p.Teleport(new Vector3(16.5f,.1f,0)); yield return Wait(.2f);
        yield return To(new Vector3(28,0,0));
        yield return To(new Vector3(10,0,0)); yield return Wait(.3f);
        CheckThat("Loaded progress is usable: walk Hub <-> Rice; no repeat Hub announcement",p.transform.position.x<11 && hub.announcements==0,"x="+p.transform.position.x.ToString("F2")+" announcements="+hub.announcements);
        // Corrupt save.
        DeleteTestSlot("p05_corrupt"); WriteRaw("p05_corrupt","{ this is not json ");
        status=w.LoadFromSlot("p05_corrupt"); yield return null;
        bool backup=File.Exists(Path.ChangeExtension(SaveSystem.PathFor("p05_corrupt"),".corrupt.json"));
        CheckThat("Corrupt save: safe fresh start, file kept as .corrupt.json, player told",status==SaveSystem.LoadStatus.Corrupt && w.SolvedPuzzles==0 && !riceGate.Unlocked && backup && File.Exists(SaveSystem.PathFor("p05_corrupt")) && p.feedback.Contains("เซฟ"),
            "status="+status+" backup="+backup+" feedback="+p.feedback);
        Vector3 before=p.transform.position; p.testMove=Vector2.up; yield return Wait(.5f); p.testMove=Vector2.zero;
        CheckThat("Game still playable after corrupt save",Vector3.Distance(before,p.transform.position)>1,"moved="+Vector3.Distance(before,p.transform.position).ToString("F2"));
        DeleteTestSlot("p05_missing");
        status=w.LoadFromSlot("p05_missing"); yield return null;
        CheckThat("Missing save: fresh start",status==SaveSystem.LoadStatus.Missing && w.SolvedPuzzles==0,"status="+status);
        WriteRaw("p05_version","{\"version\":99,\"solvedPuzzles\":[\"factory_gate\"],\"unlockedZones\":[\"rice\"]}");
        status=w.LoadFromSlot("p05_version"); yield return null;
        CheckThat("Wrong-version save: fresh start, nothing applied",status==SaveSystem.LoadStatus.WrongVersion && w.SolvedPuzzles==0 && !riceGate.Unlocked,"status="+status);
        WriteRaw("p05_unknown","{\"version\":1,\"solvedPuzzles\":[\"factory_gate\",\"bogus_id\"],\"unlockedZones\":[],\"lookSensitivity\":0.13,\"masterVolume\":1}");
        status=w.LoadFromSlot("p05_unknown"); yield return null;
        CheckThat("Unknown puzzle id ignored, valid ids applied",status==SaveSystem.LoadStatus.Ok && gate.State==PuzzleState.Solved && vent.State==PuzzleState.Active && !riceGate.Unlocked,"gate="+gate.State+" vent="+vent.State);
        // New game in the main test slot clears the saved progress.
        w.LoadFromSlot(Slot); yield return null;
        w.Restart(); yield return Wait(.3f);
        SaveSystem.Load(Slot,out var data);
        CheckThat("New game (Restart) clears saved progress",data.solvedPuzzles.Count==0 && data.unlockedZones.Count==0 && !riceGate.Unlocked && hub.Stage==0,"solved="+data.solvedPuzzles.Count);
        foreach(var s in new[]{"p05_corrupt","p05_missing","p05_version","p05_unknown"}) DeleteTestSlot(s);
        AudioListener.volume=1; p.lookSensitivity=.13f;
    }
    void Finish() {
        p.testMove=Vector2.zero; p.testMode=false; p.testAim=null;
        string path=TestReports.PathFor("p05-progress-test-"+session+".json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
        Debug.Log("OOPS_P05_DONE session="+session+" passed="+report.passed+" failed="+report.failed+" report="+path);
    }
}
}
