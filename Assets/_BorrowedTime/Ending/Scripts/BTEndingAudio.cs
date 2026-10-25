using UnityEngine;
using BorrowedTime.Menu;
namespace BorrowedTime.Ending {
[DefaultExecutionOrder(100)]
public sealed class BTEndingAudio : MonoBehaviour {
 public BTEndingCutscene director;
 public AudioSource music,soundDesign;
 [Range(0,1)] public float masterVolume=.85f,musicVolume=.06f,sfxVolume=1f;
 float lastTime=-1;
 AudioSource menuMusic;
 bool resumeMenuMusic;
 public int restarts;
 static float S(float t,float a,float b){return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));}
 void Start(){
  var menu=Object.FindFirstObjectByType<MenuAudioFeedback>();
  if(menu&&menu.music){menuMusic=menu.music;resumeMenuMusic=menuMusic.isPlaying;if(resumeMenuMusic)menuMusic.Pause();}
  Route(music,MenuAudioChannel.Music);Route(soundDesign,MenuAudioChannel.Effects);
 }
 static void Route(AudioSource source,MenuAudioChannel channel){if(!source)return;var bus=source.GetComponent<MenuAudioBus>();if(!bus)bus=source.gameObject.AddComponent<MenuAudioBus>();bus.channel=channel;}
 void Update(){
 if(!director)return;
 if(director.manualPreview){Stop();lastTime=-1;return;}
 Sync(director.elapsed);
 }
 public void Sync(float t){
 if(!music||!soundDesign||!music.clip||!soundDesign.clip)return;
 if(t>=BTEndingCutscene.Duration){Stop();lastTime=t;return;}
 // Restart on actual timeline jumps only. Audio time reports can be stale,
 // and using drift/isPlaying each frame repeatedly restarts the whole soundtrack.
 float expected=Application.isPlaying?Time.unscaledDeltaTime:0;
 bool seek=lastTime<0||t<lastTime-.05f||t-lastTime>Mathf.Max(.75f,expected+.25f);
 float duck=1-.38f*(S(t,54,56)-S(t,64,66));
 music.volume=masterVolume*musicVolume*S(t,0,2.5f)*(1-S(t,72,76))*duck;
 soundDesign.volume=masterVolume*sfxVolume;
 if(seek){
 music.Stop();soundDesign.Stop();
 music.Play();music.timeSamples=Mathf.Clamp((int)(Mathf.Repeat(t,music.clip.length)*music.clip.frequency),0,music.clip.samples-1);
 soundDesign.Play();soundDesign.timeSamples=Mathf.Clamp((int)(t*soundDesign.clip.frequency),0,soundDesign.clip.samples-1);
 restarts++;
 }
 lastTime=t;
 }
 public void Stop(){if(music)music.Stop();if(soundDesign)soundDesign.Stop();}
 void OnDisable(){Stop();lastTime=-1;}
 void OnDestroy(){if(resumeMenuMusic&&menuMusic)menuMusic.UnPause();}
}
}
