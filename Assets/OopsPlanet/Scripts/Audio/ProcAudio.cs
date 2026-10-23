using UnityEngine;
using System.Collections.Generic;
namespace OopsPlanet {
public enum Cue { Absorb, Swap, Refused, BounceLaunch, Rebound, Land, PressureWhoosh, Combo, Respawn, Happy, Unlock, Blink, UiClick }
public enum Ambience { Hub, HubRestored, Factory, FactoryRestored, Rice, RiceRestored }
// All game audio is generated in code (no third-party audio files, so no licence questions).
// BLOB never speaks: its "voice" is short pitched chirps whose contour carries the emotion.
public static class ProcAudio {
    const int Rate=44100;
    static readonly Dictionary<string,AudioClip> cache=new Dictionary<string,AudioClip>();
    static AudioClip Make(string name,float seconds,System.Func<float,float> f,bool loopFade=false) {
        if(cache.TryGetValue(name,out var c) && c) return c;
        int n=Mathf.CeilToInt(Rate*seconds); var data=new float[n];
        for(int i=0;i<n;i++) data[i]=Mathf.Clamp(f(i/(float)Rate),-1,1);
        if(loopFade) { int fade=Rate/10; for(int i=0;i<fade;i++) { float k=i/(float)fade; data[i]=data[i]*k+data[n-fade+i]*(1-k); } System.Array.Resize(ref data,n-fade); n-=fade; }
        c=AudioClip.Create(name,n,1,Rate,false); c.SetData(data,0); cache[name]=c; return c;
    }
    static float Env(float t,float dur,float attack=.01f) => Mathf.Clamp01(t/attack)*Mathf.Clamp01((dur-t)/(dur*.4f));
    // Sine glide f0->f1 with optional vibrato: the basis of BLOB's chirps.
    static AudioClip Chirp(string name,float f0,float f1,float dur,float vol=.25f,float vib=0) {
        return Make(name,dur,t=>{ float k=t/dur; float f=Mathf.Lerp(f0,f1,k*k*(3-2*k)); float phase=2*Mathf.PI*(f0*t+(f-f0)*t*.5f);
            return Mathf.Sin(phase+vib*Mathf.Sin(t*38))*vol*Env(t,dur); });
    }
    static uint seed=12345;
    static float Noise() { seed=seed*1664525u+1013904223u; return (seed>>9)/(float)(1<<23)*2-1; }
    public static AudioClip Get(Cue cue) {
        switch(cue) {
            case Cue.Absorb: return Chirp("Absorb boop",300,720,.28f,.28f);
            case Cue.Swap: return Make("Swap bip-bip",.26f,t=>Mathf.Sin(t*2*Mathf.PI*(t<.12f?660:880))*.22f*Env(t%.13f,.12f));
            case Cue.Refused: return Chirp("Refused hm",210,170,.32f,.25f,1.5f);
            case Cue.BounceLaunch: return Chirp("Bounce wheee",380,980,.45f,.25f,.6f);
            case Cue.Rebound: return Make("Rebound boing",.35f,t=>Mathf.Sin(2*Mathf.PI*(220+90*Mathf.Sin(t*30)*Mathf.Exp(-t*8))*t)*.3f*Mathf.Exp(-t*7));
            case Cue.Land: { seed=7; return Make("Land pof",.18f,t=>(Mathf.Sin(t*2*Mathf.PI*95)*.5f+Noise()*.25f)*Mathf.Exp(-t*25)); }
            case Cue.PressureWhoosh: { seed=11; float lp=0; return Make("Pressure whoosh",.45f,t=>{ float cut=Mathf.Lerp(.5f,.05f,t/.45f); lp+=(Noise()-lp)*cut; return lp*.9f*Env(t,.45f,.02f); }); }
            case Cue.Combo: return Make("Combo sparkle",.4f,t=>(Mathf.Sin(t*2*Mathf.PI*880)+Mathf.Sin(t*2*Mathf.PI*1320))*.12f*Mathf.Exp(-t*6));
            case Cue.Respawn: return Chirp("Respawn aww",620,240,.5f,.25f,.8f);
            case Cue.Happy: return Make("Happy trill",.6f,t=>Mathf.Sin(t*2*Mathf.PI*((int)(t*14)%2==0?740:988))*.2f*Env(t,.6f));
            case Cue.Unlock: return Make("Unlock fanfare",1.1f,t=>{ float[] n={392,523.3f,659.3f,784}; int k=Mathf.Min(3,(int)(t/.22f)); return (Mathf.Sin(t*2*Mathf.PI*n[k])+.4f*Mathf.Sin(t*4*Mathf.PI*n[k]))*.14f*Mathf.Exp(-(t-k*.22f)*4); });
            case Cue.Blink: return Chirp("Blink tick",1400,1600,.03f,.04f);
            default: return Chirp("UI click",900,700,.05f,.15f);
        }
    }
    public static AudioClip MagnetBuzz() => Make("Magnet buzz",1f,t=>(Mathf.Sin(t*2*Mathf.PI*60)*.5f+Mathf.Sin(t*2*Mathf.PI*120)*.3f+Mathf.Sign(Mathf.Sin(t*2*Mathf.PI*180))*.08f)*.25f);
    public static AudioClip Get(Ambience a) {
        switch(a) {
            case Ambience.Hub: return Make("Amb hub",8,t=>(Mathf.Sin(t*2*Mathf.PI*130.8f)+Mathf.Sin(t*2*Mathf.PI*196f)*.7f)*.05f*(.7f+.3f*Mathf.Sin(t*Mathf.PI/4)),true);
            case Ambience.HubRestored: return Make("Amb hub restored",8,t=>(Mathf.Sin(t*2*Mathf.PI*261.6f)+Mathf.Sin(t*2*Mathf.PI*329.6f)*.8f+Mathf.Sin(t*2*Mathf.PI*392f)*.6f)*.04f*(.7f+.3f*Mathf.Sin(t*Mathf.PI/4)),true);
            case Ambience.Factory: { seed=21; return Make("Amb factory",6,t=>{ float clank=(t%2.5f)<.08f?Noise()*Mathf.Exp(-(t%2.5f)*40)*.5f:0; return (Mathf.Sin(t*2*Mathf.PI*55)*.4f+Mathf.Sin(t*2*Mathf.PI*110)*.2f*(1+Mathf.Sin(t*3)))*.18f+clank; },true); }
            case Ambience.FactoryRestored: return Make("Amb factory restored",6,t=>(Mathf.Sin(t*2*Mathf.PI*220)+Mathf.Sin(t*2*Mathf.PI*330)*.5f)*.035f+Mathf.Sin(t*2*Mathf.PI*110)*.04f,true);
            case Ambience.Rice: { seed=31; float lp=0; return Make("Amb rice wind",8,t=>{ lp+=(Noise()-lp)*.02f; return lp*.6f*(.6f+.4f*Mathf.Sin(t*Mathf.PI/4)); },true); }
            default: { seed=41; float lp=0; return Make("Amb rice restored",8,t=>{ lp+=(Noise()-lp)*.02f; float bird=(t%1.7f)<.25f?Mathf.Sin(t*2*Mathf.PI*(2400+600*Mathf.Sin(t*40)))*.06f*Mathf.Sin((t%1.7f)/.25f*Mathf.PI):0; return lp*.4f+bird+Mathf.Sin(t*2*Mathf.PI*523)*.008f; },true); }
        }
    }
    public static AudioClip Jingle() => Make("Puzzle solved",1f,t=>{ float[] notes={523.3f,659.3f,784f,1046.5f}; int k=Mathf.Min(3,(int)(t/.2f)); float lt=t-k*.2f; return Mathf.Sin(t*notes[k]*Mathf.PI*2)*.2f*Mathf.Exp(-lt*6); });
    public static AudioClip Hum() => Make("Turbine hum",1f,t=>(Mathf.Sin(t*110*Mathf.PI*2)*.6f+Mathf.Sin(t*220*Mathf.PI*2)*.3f)*.3f);
}
}
