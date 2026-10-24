using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet.Editor.Tests {
public class ComboTableTests {
    ComboTable table;
    [SetUp] public void SetUp(){table=ScriptableObject.CreateInstance<ComboTable>();table.cells=ComboTableDefaults.Create();}
    [TearDown] public void TearDown(){Object.DestroyImmediate(table);}
    [Test] public void FifteenUniquePairsCoverAllSixPowers(){
        Assert.AreEqual(15,table.cells.Count);var ids=new HashSet<string>();foreach(var c in table.cells)Assert.IsTrue(ids.Add(c.id));
        foreach(var a in ComboTable.Powers)foreach(var b in ComboTable.Powers){if(a==b)Assert.IsNull(table.Get(a,b));else Assert.IsNotNull(table.Get(a,b));}
    }
    [Test] public void BothOrdersHaveTheSameEffectAndIdentity(){foreach(var a in ComboTable.Powers)foreach(var b in ComboTable.Powers)if(a!=b){Assert.AreSame(table.Get(a,b),table.Get(b,a));Assert.AreEqual(ComboTable.Id(a,b),ComboTable.Id(b,a));}}
    [Test] public void LegacyDiscoveryKeysNormalizeWithoutLosingTheirPair(){Assert.AreEqual("electric+heat",ComboTable.NormalizeId("heat+electric"));Assert.AreEqual("magnet+water",ComboTable.NormalizeId("water+magnet"));}
    [Test] public void NoneNeverCombines(){Assert.IsNull(table.Get(BlobProperty.None,BlobProperty.Heat));Assert.IsNull(table.Get(BlobProperty.Water,BlobProperty.None));}
    [Test] public void EveryComboHasPlayerInstructions(){foreach(var c in table.cells){Assert.IsTrue(c.implemented);Assert.IsFalse(string.IsNullOrWhiteSpace(c.nameTh));Assert.IsFalse(string.IsNullOrWhiteSpace(c.effectTh));Assert.IsFalse(string.IsNullOrWhiteSpace(c.useTh));}}
}
}
