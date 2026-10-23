using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
// Heat + water makes a short-lived steam column that lifts BLOB (design §5: water -> steam, §6 F2 boiler room).
public class SteamSystem : MonoBehaviour {
    public BlobWorld world;
    public float lifetime=4f, radius=2.2f, height=7f, liftSpeed=7f;
    public int columnsRaised;
    // Any steam column (tests, steam inlets and turbines listen).
    public static event System.Action<Vector3> ColumnRaised;
    class Column { public Vector3 pos; public float until; }
    readonly List<Column> columns=new List<Column>();
    float nextPuff;
    void OnEnable() { ElementReaction.SteamRaised+=Raise; }
    void OnDisable() { ElementReaction.SteamRaised-=Raise; }
    void Raise(WorldMaterial source,Vector3 at) { RaiseAt(new Vector3(at.x,source.Bounds.max.y,at.z)); }
    // Steam made directly by a combo (Water + Heat spray, boiling).
    public void RaiseAt(Vector3 basePos) {
        columns.Add(new Column{pos=basePos,until=Time.time+lifetime}); columnsRaised++;
        ColumnRaised?.Invoke(basePos);
    }
    public bool InSteam(Vector3 p) {
        foreach(var c in columns) if(Time.time<c.until) {
            Vector3 d=p-c.pos; if(new Vector2(d.x,d.z).magnitude<radius && d.y>-1f && d.y<height) return true;
        }
        return false;
    }
    void Update() {
        columns.RemoveAll(c=>Time.time>=c.until);
        if(columns.Count==0) return;
        if(world && world.player && InSteam(world.player.transform.position))
            world.player.velocity.y=Mathf.Max(world.player.velocity.y,liftSpeed);
        if(Time.time>nextPuff) {
            nextPuff=Time.time+.08f;
            foreach(var c in columns) VfxKit.Play(k=>k.Burst(c.pos+Random.insideUnitSphere*.6f,new Color(.92f,.95f,1),2,4,.35f,1.2f,Vector3.up,15));
        }
    }
    public void Clear() { columns.Clear(); }
}
}
