using System;
using System.Collections.Generic;
using BorrowedTime.Menu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

/// <summary>Shared captions, keyboard skip and sound cues for the opening film.</summary>
public sealed class CutscenePresentation : MonoBehaviour
{
    public string Shot { get; private set; }
    Label caption, chapter;
    AudioSource effects, engine;
    Action skip;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    UnityEngine.TextCore.Text.FontAsset fontAsset;

    public void Initialize(UIDocument document, Font font, Action skipAction)
    {
        skip = skipAction;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        var root = document.rootVisualElement;
        var top = new VisualElement { name = "cinema-top", pickingMode = PickingMode.Ignore };
        top.style.position = Position.Absolute; top.style.left = top.style.right = top.style.top = 0;
        top.style.height = Length.Percent(7); top.style.backgroundColor = Color.black;
        root.Insert(0, top);
        var bottom = new VisualElement { name = "cinema-bottom", pickingMode = PickingMode.Ignore };
        bottom.style.position = Position.Absolute; bottom.style.left = bottom.style.right = bottom.style.bottom = 0;
        bottom.style.height = Length.Percent(13); bottom.style.backgroundColor = new Color(.015f, .025f, .028f, .94f);
        root.Insert(1, bottom);
        chapter = new Label("BORROWED TIME  /  PROLOGUE") { pickingMode = PickingMode.Ignore };
        chapter.style.position = Position.Absolute; chapter.style.left = 36; chapter.style.top = 16;
        chapter.style.fontSize = 18; chapter.style.letterSpacing = 3; chapter.style.color = new Color(.77f,.88f,.8f);
        top.Add(chapter);
        caption = new Label { name = "cutscene-caption", pickingMode = PickingMode.Ignore };
        caption.style.position = Position.Absolute; caption.style.left = Length.Percent(5);
        caption.style.right = Length.Percent(19); caption.style.top = 13; caption.style.bottom = 8;
        caption.style.fontSize = 27; caption.style.whiteSpace = WhiteSpace.Normal;
        caption.style.unityTextAlign = TextAnchor.MiddleCenter; caption.style.color = Color.white;
        if(font) { fontAsset=UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(font); caption.style.unityFontDefinition=FontDefinition.FromSDFFont(fontAsset); }
        bottom.Add(caption);
        effects = gameObject.AddComponent<AudioSource>(); effects.playOnAwake=false; effects.spatialBlend=0; effects.volume=.65f;
        gameObject.AddComponent<MenuAudioBus>();
        var motor = new GameObject("Van engine"); motor.transform.SetParent(transform,false);
        engine=motor.AddComponent<AudioSource>(); engine.playOnAwake=false; engine.loop=true; engine.volume=.17f;
        motor.AddComponent<MenuAudioBus>();
        var button=root.Q<Button>();
        if(button!=null) { button.name="cutscene-skip"; button.text="Skip / Esc"; button.style.fontSize=23; button.style.height=48; button.style.borderTopWidth=button.style.borderBottomWidth=button.style.borderLeftWidth=button.style.borderRightWidth=2; button.RegisterCallback<PointerEnterEvent>(_=>Cue("hover")); button.clicked+=()=>Cue("click"); }
    }

    public void SetShot(string shot, string text) { Shot=shot; caption.text=text; }
    public void RequestSkip() => skip?.Invoke();
    void Update() { if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)RequestSkip(); }
    public void Engine(float speed)
    {
        if(!engine)return;
        if(speed<0) { engine.Stop(); return; }
        engine.pitch=.65f+Mathf.Clamp01(speed)*.7f;
        if(!engine.clip)engine.clip=MakeClip("engine");
        if(!engine.isPlaying)engine.Play();
    }
    public void Cue(string cue) { if(effects)effects.PlayOneShot(MakeClip(cue),cue=="step"?.3f:.65f); }
    AudioClip MakeClip(string cue)
    {
        if(clips.TryGetValue(cue,out var cached))return cached;
        int rate=22050; float duration=cue=="portal"?2.2f:cue=="engine"?1f:cue=="drawer"?.8f:cue=="letter"?.45f:.18f;
        var data=new float[Mathf.CeilToInt(rate*duration)]; var random=new System.Random(171);
        for(int i=0;i<data.Length;i++)
        {
            float t=i/(float)rate, u=t/duration, noise=(float)random.NextDouble()*2-1;
            float envelope=Mathf.Sin(Mathf.PI*u)*Mathf.Exp(-u*(cue=="portal"?0:3));
            float sample;
            switch(cue)
            {
                case "engine": sample=(Mathf.Sin(2*Mathf.PI*65*t)+.35f*Mathf.Sin(2*Mathf.PI*130*t)+noise*.12f)*.18f; envelope=1; break;
                case "portal": sample=(Mathf.Sin(2*Mathf.PI*(170*t+180*t*t))+.4f*Mathf.Sin(2*Mathf.PI*523.25f*t)+noise*.14f)*.27f; break;
                case "letter": sample=(noise*.4f+Mathf.Sin(2*Mathf.PI*880*t)*.15f)*.3f; break;
                case "drawer": sample=noise*.17f+Mathf.Sin(2*Mathf.PI*140*t)*.15f; break;
                case "step": sample=noise*.25f+Mathf.Sin(2*Mathf.PI*95*t)*.2f; break;
                case "chime": sample=(Mathf.Sin(2*Mathf.PI*659.25f*t)+Mathf.Sin(2*Mathf.PI*987.77f*t)*.5f)*.23f; break;
                default: sample=Mathf.Sin(2*Mathf.PI*(cue=="hover"?660:440)*t)*.2f; break;
            }
            data[i]=sample*envelope;
        }
        var clip=AudioClip.Create("Opening "+cue,data.Length,1,rate,false);clip.SetData(data,0);clips.Add(cue,clip);return clip;
    }
    void OnDestroy() { foreach(var clip in clips.Values)if(clip)Destroy(clip);if(fontAsset)Destroy(fontAsset); }
}
