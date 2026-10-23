using NUnit.Framework;
using System.IO;
namespace OopsPlanet.Editor.Tests {
// EditMode tests for save v2: v1 files migrate (new lists empty), v2 lists round-trip, unknown versions are refused.
// Uses its own slot and deletes it; the player's saves are never touched.
public class SaveV2Tests {
    const string Slot="editmode_save_v2_test";
    [TearDown] public void TearDown() { foreach(var f in new[]{SaveSystem.PathFor(Slot),Path.ChangeExtension(SaveSystem.PathFor(Slot),".corrupt.json")}) if(File.Exists(f)) File.Delete(f); }
    [Test] public void V1FileMigratesToV2() {
        File.WriteAllText(SaveSystem.PathFor(Slot),"{\"version\":1,\"solvedPuzzles\":[\"factory_gate\"],\"unlockedZones\":[\"rice\"],\"lookSensitivity\":0.2,\"masterVolume\":0.5}");
        Assert.AreEqual(SaveSystem.LoadStatus.Ok,SaveSystem.Load(Slot,out var d));
        Assert.AreEqual(SaveData.CurrentVersion,d.version);
        Assert.AreEqual(2,SaveData.CurrentVersion);
        CollectionAssert.AreEqual(new[]{"factory_gate"},d.solvedPuzzles);
        Assert.IsNotNull(d.discoveredCombos); Assert.IsEmpty(d.discoveredCombos);
        Assert.IsNotNull(d.shipParts); Assert.IsNotNull(d.installedParts);
    }
    [Test] public void V2ListsRoundTrip() {
        var d=new SaveData(); d.discoveredCombos.Add("heat+electric"); d.shipParts.Add("part_h1"); d.installedParts.Add("part_f1"); d.unlockedZones.Add("shortcut_dike");
        Assert.IsTrue(SaveSystem.Save(Slot,d));
        Assert.AreEqual(SaveSystem.LoadStatus.Ok,SaveSystem.Load(Slot,out var r));
        CollectionAssert.AreEqual(d.discoveredCombos,r.discoveredCombos);
        CollectionAssert.AreEqual(d.shipParts,r.shipParts);
        CollectionAssert.AreEqual(d.installedParts,r.installedParts);
        CollectionAssert.AreEqual(d.unlockedZones,r.unlockedZones);
    }
    [Test] public void UnknownVersionRefused() {
        File.WriteAllText(SaveSystem.PathFor(Slot),"{\"version\":99,\"solvedPuzzles\":[\"x\"]}");
        Assert.AreEqual(SaveSystem.LoadStatus.WrongVersion,SaveSystem.Load(Slot,out var d));
        Assert.IsEmpty(d.solvedPuzzles);
    }
}
}
