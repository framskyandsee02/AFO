using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet.Editor.Tests {
// EditMode tests for the central combination resolver (PowerCombos.Resolve).
public class PowerComboResolverTests {
    List<CombinationDefinition> combos;
    CombinationDefinition cannon, dash;
    static CombinationDefinition Combo(BlobProperty a,BlobProperty b,ComboEffect e) {
        var c=ScriptableObject.CreateInstance<CombinationDefinition>(); c.a=a; c.b=b; c.effect=e; return c;
    }
    [SetUp] public void SetUp() {
        cannon=Combo(BlobProperty.Magnet,BlobProperty.Pressure,ComboEffect.MagnetCannon);
        dash=Combo(BlobProperty.Pressure,BlobProperty.Bounce,ComboEffect.PressureDash);
        combos=new List<CombinationDefinition>{cannon,dash};
    }
    [TearDown] public void TearDown() { foreach(var c in combos) Object.DestroyImmediate(c); }
    [Test] public void SupportedPairResolves() {
        Assert.AreSame(cannon,PowerCombos.Resolve(BlobProperty.Magnet,BlobProperty.Pressure,combos));
        Assert.AreSame(dash,PowerCombos.Resolve(BlobProperty.Pressure,BlobProperty.Bounce,combos));
    }
    [Test] public void OrderDoesNotMatter() {
        Assert.AreSame(cannon,PowerCombos.Resolve(BlobProperty.Pressure,BlobProperty.Magnet,combos));
        Assert.AreSame(dash,PowerCombos.Resolve(BlobProperty.Bounce,BlobProperty.Pressure,combos));
    }
    [Test] public void UnsupportedPairFallsBackToNull() {
        Assert.IsNull(PowerCombos.Resolve(BlobProperty.Magnet,BlobProperty.Bounce,combos));
        Assert.IsNull(PowerCombos.Resolve(BlobProperty.Bounce,BlobProperty.Magnet,combos));
    }
    [Test] public void SamePowerOrEmptySlotNeverCombines() {
        Assert.IsNull(PowerCombos.Resolve(BlobProperty.Magnet,BlobProperty.Magnet,combos));
        Assert.IsNull(PowerCombos.Resolve(BlobProperty.Magnet,BlobProperty.None,combos));
        Assert.IsNull(PowerCombos.Resolve(BlobProperty.None,BlobProperty.None,combos));
    }
    [Test] public void NullOrMissingEntriesAreSafe() {
        Assert.IsNull(PowerCombos.Resolve(BlobProperty.Magnet,BlobProperty.Pressure,null));
        var withNull=new List<CombinationDefinition>{null,dash};
        Assert.IsNull(PowerCombos.Resolve(BlobProperty.Magnet,BlobProperty.Pressure,withNull));
        Assert.AreSame(dash,PowerCombos.Resolve(BlobProperty.Bounce,BlobProperty.Pressure,withNull));
    }
    [Test] public void PowerSetUsesResolver() {
        var set=ScriptableObject.CreateInstance<PowerSet>(); set.combinations.AddRange(combos);
        Assert.AreSame(cannon,set.Resolve(BlobProperty.Pressure,BlobProperty.Magnet));
        Assert.IsNull(set.Resolve(BlobProperty.Magnet,BlobProperty.Bounce));
        Object.DestroyImmediate(set);
    }
}
}
