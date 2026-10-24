using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OopsPlanet {
public class FactoryTravelTest : MonoBehaviour {
    public static bool Done;public static string Result;string slot="test_factory_travel";System.Collections.Generic.List<string> checks=new System.Collections.Generic.List<string>();int failed;
    void Check(string label,bool passed,string detail){string line=(passed?"PASS ":"FAIL ")+label+" "+detail;checks.Add(line);if(!passed)failed++;Debug.Log("FACTORY_TRAVEL_TEST "+line);}
    BlobWorld World=>FindFirstObjectByType<BlobWorld>();
    void Update(){if(World){World.player.testMode=true;if(!FactoryTravel.Busy)World.player.paused=false;}}
    IEnumerator Wait(float seconds){float t=Time.unscaledTime+seconds;while(Time.unscaledTime<t)yield return null;}
    IEnumerator Loaded(string path){float until=Time.unscaledTime+35;while(Time.unscaledTime<until&&(SceneManager.GetActiveScene().path!=path||FactoryTravel.Busy))yield return null;yield return Wait(.5f);}
    IEnumerator Start(){
        Done=false;DontDestroyOnLoad(gameObject);World.UseSaveSlot(slot,false);World.Restart();World.player.SetProperties(BlobProperty.Water,BlobProperty.Magnet);
        FactoryTravel.Go(World,false);yield return Loaded(FactoryTravel.Outside);
        Check("factory to outside",SceneManager.GetActiveScene().path==FactoryTravel.Outside&&World.saveSlot==slot,"scene="+SceneManager.GetActiveScene().name+" slot="+World.saveSlot);
        string outsideId=World.puzzles.First(p=>p&&!p.transform.IsChildOf(GameObject.Find("Factory progress metadata").transform)).id;
        var seed=new SaveData();seed.solvedPuzzles.Add(outsideId);seed.solvedPuzzles.Add("f6_assembly");seed.solvedPuzzles.Add("factory_boss_repaired");seed.shipParts.Add("part_factory_core");seed.installedParts.Add("outside_part_fixture");SaveSystem.Save(slot,seed);World.LoadFromSlot(slot);World.player.SetProperties(BlobProperty.Water,BlobProperty.Magnet);
        yield return Wait(1.5f);World.player.Teleport(new Vector3(0,.1f,23));World.player.testMove=Vector2.up;
        yield return Loaded(FactoryTravel.Interior);World.player.testMove=Vector2.zero;
        Check("walking through facade loads factory",SceneManager.GetActiveScene().path==FactoryTravel.Interior,"scene="+SceneManager.GetActiveScene().name);
        Check("slot and powers transferred",World.saveSlot==slot&&World.player.property==BlobProperty.Water&&World.player.secondary==BlobProperty.Magnet,"slot="+World.saveSlot+" powers="+World.player.property+"/"+World.player.secondary);
        Check("factory progress loaded",World.FindPuzzle("f6_assembly").State==PuzzleState.Solved&&World.FindPuzzle("factory_boss_repaired").State==PuzzleState.Solved&&World.ship.Has("part_factory_core"),"parts="+World.ship.collected.Count);
        Check("loaded boss friendly",FindFirstObjectByType<FactoryRepairBoss>().repaired,"boss="+FindFirstObjectByType<FactoryRepairBoss>().stage);
        Check("single player camera",FindObjectsByType<BlobPlayer>(FindObjectsSortMode.None).Length==1&&FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"players="+FindObjectsByType<BlobPlayer>(FindObjectsSortMode.None).Length);
        yield return Wait(1.5f);World.player.testMove=Vector2.down;yield return Loaded(FactoryTravel.Outside);World.player.testMove=Vector2.zero;
        SaveData data;SaveSystem.Load(slot,out data);Check("walk exit and retain both scene progress",SceneManager.GetActiveScene().path==FactoryTravel.Outside&&World.FindPuzzle(outsideId).State==PuzzleState.Solved&&data.solvedPuzzles.Contains("factory_boss_repaired")&&data.shipParts.Contains("part_factory_core")&&data.installedParts.Contains("outside_part_fixture"),JsonUtility.ToJson(data));
        Result=string.Join("\n",checks)+"\nfailed="+failed;Directory.CreateDirectory("Assets/Scenes/OopsPlanet/Evidence/FactoryExpansion");File.WriteAllText("Assets/Scenes/OopsPlanet/Evidence/FactoryExpansion/travel-test.txt",Result);Done=true;World.player.paused=true;Debug.Log("FACTORY_TRAVEL_TEST_DONE failed="+failed);
    }
}
}
