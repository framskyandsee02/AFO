using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OopsPlanet {
public static class FactoryProgressStore {
    public static void Merge(BlobWorld world,SaveData current){
        SaveData old;if(SaveSystem.Load(world.saveSlot,out old)!=SaveSystem.LoadStatus.Ok)return;
        var puzzles=new HashSet<string>();foreach(var p in world.puzzles)if(p)puzzles.Add(p.id);
        var gates=new HashSet<string>();foreach(var g in world.zoneGates)if(g)gates.Add(g.zoneId);
        var parts=new HashSet<string>();if(world.ship)foreach(var p in world.ship.parts)if(p)parts.Add(p.id);
        KeepUnknown(old.solvedPuzzles,current.solvedPuzzles,puzzles);KeepUnknown(old.unlockedZones,current.unlockedZones,gates);
        KeepUnknown(old.shipParts,current.shipParts,parts);KeepUnknown(old.installedParts,current.installedParts,parts);
        foreach(var id in old.discoveredCombos){string key=ComboTable.NormalizeId(id);if(!current.discoveredCombos.Contains(key))current.discoveredCombos.Add(key);}
        current.shipParts.RemoveAll(id=>current.installedParts.Contains(id));
    }
    static void KeepUnknown(List<string> old,List<string> now,HashSet<string> owned){foreach(var id in old)if(!owned.Contains(id)&&!now.Contains(id))now.Add(id);}
}
}
