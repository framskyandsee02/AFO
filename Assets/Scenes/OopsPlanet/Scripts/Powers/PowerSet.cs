using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
// The powers and the (deliberately small) list of supported combinations used by a level.
[CreateAssetMenu(menuName="OOPS Planet/Power Set",fileName="PowerSet_")]
public class PowerSet : ScriptableObject {
    public const int MaxActive=2;
    public List<PowerDefinition> powers=new List<PowerDefinition>();
    public List<CombinationDefinition> combinations=new List<CombinationDefinition>();
    [Tooltip("15 unordered power pairs. Each pair has one shared combo effect.")]
    public ComboTable table;
    public PowerDefinition Get(BlobProperty p) {
        foreach(var d in powers) if(d && d.property==p) return d;
        return null;
    }
    public CombinationDefinition Resolve(BlobProperty a,BlobProperty b) => PowerCombos.Resolve(a,b,combinations);
}
// Central combination resolver: order-independent, null when the pair is not supported.
public static class PowerCombos {
    public static CombinationDefinition Resolve(BlobProperty a,BlobProperty b,IReadOnlyList<CombinationDefinition> combos) {
        if(a==BlobProperty.None || b==BlobProperty.None || a==b || combos==null) return null;
        foreach(var c in combos)
            if(c && ((c.a==a && c.b==b) || (c.a==b && c.b==a))) return c;
        return null;
    }
}
}
