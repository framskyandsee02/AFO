using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet.Editor.Tests {
// EditMode tests for the v2 ordered combo table (Docs/DESIGN_V2_TH.md §4): 6 powers -> 30 ordered cells.
public class ComboTableTests {
    ComboTable table;
    [SetUp] public void SetUp() { table=ScriptableObject.CreateInstance<ComboTable>(); table.cells=ComboTableDefaults.Create(); }
    [TearDown] public void TearDown() { Object.DestroyImmediate(table); }
    [Test] public void HasThirtyUniqueCells() {
        Assert.AreEqual(30,table.cells.Count);
        var ids=new HashSet<string>(); foreach(var c in table.cells) Assert.IsTrue(ids.Add(c.id),"duplicate "+c.id);
    }
    [Test] public void EveryOrderedPairOfDifferentPowersExists() {
        foreach(var a in ComboTable.Powers) foreach(var b in ComboTable.Powers) {
            if(a==b) { Assert.IsNull(table.Get(a,b),"diagonal "+a); continue; }
            var c=table.Get(a,b); Assert.IsNotNull(c,a+"+"+b);
            Assert.AreEqual(a,c.primary); Assert.AreEqual(b,c.secondary); Assert.AreEqual(ComboTable.Id(a,b),c.id);
        }
    }
    [Test] public void OrderMatters() {
        Assert.AreNotSame(table.Get(BlobProperty.Magnet,BlobProperty.Pressure),table.Get(BlobProperty.Pressure,BlobProperty.Magnet));
        Assert.AreEqual("magnet+pressure",table.Get(BlobProperty.Magnet,BlobProperty.Pressure).id);
    }
    [Test] public void NoneNeverCombines() {
        Assert.IsNull(table.Get(BlobProperty.None,BlobProperty.Heat));
        Assert.IsNull(table.Get(BlobProperty.Water,BlobProperty.None));
    }
    [Test] public void ThaiTextFilled() {
        foreach(var c in table.cells) {
            Assert.IsFalse(string.IsNullOrWhiteSpace(c.nameTh),c.id+" name");
            Assert.IsFalse(string.IsNullOrWhiteSpace(c.effectTh),c.id+" effect");
            Assert.IsFalse(string.IsNullOrWhiteSpace(c.useTh),c.id+" use");
        }
    }
}
}
