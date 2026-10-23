using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
// The one element x material rule table for the whole game (Docs/DESIGN_V2_TH.md §5).
// Powers and combos (stage C) call Apply(); puzzles listen to WorldMaterial.Changed.
public static class ElementReaction {
    public const float WetSeconds=20f, HeatSeconds=10f, PowerSeconds=10f;
    public const float BurnSeconds=6f, FireLifetime=15f, FireSpreadRadius=3f, FireNeighbourGap=1.5f, SpreadDelay=2f;
    public const float ConductGap=.6f; public const int ConductMaxNodes=12;
    public static int reactions;                         // test / debug counter
    public static event System.Action<WorldMaterial,Vector3> SteamRaised;
    public static void Apply(BlobProperty element,WorldMaterial m,float strength=1,Vector3 direction=default) {
        if(!m || m.consumed) return;
        reactions++;
        switch(element) {
            case BlobProperty.Water:
                if(m.burning) m.Extinguish();
                if(m.kind==MaterialKind.Plant) m.Grow();
                else if(m.kind==MaterialKind.Mud && !m.dried) m.Consume("washed");
                else if(m.kind==MaterialKind.Metal||m.kind==MaterialKind.Machine) { if(m.Heated) { m.Cool(); SteamRaised?.Invoke(m,m.Bounds.center); } }
                if(m.kind!=MaterialKind.WaterBody) m.SetWet(WetSeconds);
                break;
            case BlobProperty.Heat:
                if(m.Flammable) m.Ignite(m.Bounds.center,Time.time);
                else if(m.kind==MaterialKind.Ice||m.kind==MaterialKind.Wax) m.Consume("melted");
                else if(m.kind==MaterialKind.Mud) m.Dry();
                else if(m.kind==MaterialKind.Metal) m.Heat(HeatSeconds);
                else if(m.kind==MaterialKind.WaterBody) { m.Raise("steam"); SteamRaised?.Invoke(m,m.Bounds.center); }
                // Wet flammables stay fire-proof for the whole wet time (design §5); other wet things dry off.
                if(m.Wet && !m.Flammable && m.kind!=MaterialKind.WaterBody) { m.wetUntil=0; m.Raise("dried-off"); }
                break;
            case BlobProperty.Electric:
                Conduct(m);
                break;
            case BlobProperty.Pressure:
                if(m.burning) m.Extinguish();       // small fires blow out
                break;
            // Magnet and Bounce act through physics (BlobMagnetic, Bounce movement), not material state.
        }
    }
    // Electricity travels through touching conductive materials (metal, water, wet things, machines) and powers machines.
    public static List<WorldMaterial> Conduct(WorldMaterial start) {
        var reached=new List<WorldMaterial>();
        if(!start || start.consumed) return reached;
        var open=new Queue<WorldMaterial>(); open.Enqueue(start); reached.Add(start);
        while(open.Count>0 && reached.Count<ConductMaxNodes) {
            var m=open.Dequeue();
            if(m.kind==MaterialKind.Machine) m.PowerUp(PowerSeconds);
            if(!m.Conductive) continue;
            Bounds b=m.Bounds; b.Expand(ConductGap*2);
            foreach(var n in WorldMaterial.All) {
                if(reached.Contains(n) || n.consumed || !n.Conductive) continue;
                if(b.Intersects(n.Bounds)) { reached.Add(n); open.Enqueue(n); }
            }
        }
        return reached;
    }
    // Called every frame by a burning material: ignite flammable neighbours, but never farther than FireSpreadRadius
    // from where this fire started, and never after FireLifetime (fires always burn out).
    public static void SpreadFire(WorldMaterial burning) {
        if(Time.time>burning.fireStarted+FireLifetime) return;
        if(Time.time<burning.ignitedAt+SpreadDelay) return;      // a fresh fire can still be put out before it spreads
        Bounds b=burning.Bounds; b.Expand(FireNeighbourGap*2);
        foreach(var n in WorldMaterial.All) {
            if(n==burning || n.burning || !n.Flammable || n.Wet) continue;
            if(!b.Intersects(n.Bounds)) continue;
            if(Vector3.Distance(n.Bounds.center,burning.fireOrigin)>FireSpreadRadius) continue;
            n.Ignite(burning.fireOrigin,burning.fireStarted);
        }
    }
    // Apply an element to every material inside a volume (blasts, splashes, slams).
    public static int ApplyArea(BlobProperty element,Bounds area,float strength=1) {
        int n=0; foreach(var m in WorldMaterial.All.ToArray()) if(!m.consumed && area.Intersects(m.Bounds)) { Apply(element,m,strength); n++; }
        return n;
    }
}
}
