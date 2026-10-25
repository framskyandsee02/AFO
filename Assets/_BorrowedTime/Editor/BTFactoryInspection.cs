#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace BorrowedTime.Editor {
public static class BTFactoryInspection {
 public const string Out="Captures/TrainingComfort/Factory";
 [MenuItem("Tools/Borrowed Time/Factory/Inspect factory")]
 public static void Inspect(){
  Directory.CreateDirectory(Out);var g=Object.FindFirstObjectByType<BTFactoryGame>();
  File.WriteAllText(Out+"/Scene.txt","FunFirst="+g.funFirst+"\n"+string.Join("\n",Object.FindObjectsByType<BTTimeTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(t=>t.name+" | "+t.kind+" | "+t.transform.position+" | age="+t.age+" initial="+t.initialAge+" life="+t.life+" regen="+t.regenPerSecond+" scale="+t.transform.lossyScale+" active="+t.gameObject.activeSelf))+"\nEXPANSION\n"+string.Join("\n",g.GetComponent<BTExpansionWorld>().puzzles.Select(p=>p.id+" | "+p.title+" approach="+p.approach+" ages="+string.Join(",",p.ages))));
  Pictures();
 }
 [MenuItem("Tools/Borrowed Time/Factory/Capture factory")]
 public static void Pictures(){
  var asyncProperty=typeof(ShaderUtil).GetProperty("allowAsyncCompilation",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);if(asyncProperty!=null)asyncProperty.SetValue(null,false);Directory.CreateDirectory(Out);var g=Object.FindFirstObjectByType<BTFactoryGame>();var go=new GameObject("Temporary factory evidence camera");var c=go.AddComponent<Camera>();c.CopyFrom(g.player.view);c.orthographic=false;c.fieldOfView=58;
  Capture(c,"Overview",new Vector3(-48,50,-58),new Vector3(0,5,1));
  Capture(c,"Receiving",new Vector3(5,3,-27),new Vector3(-4,1.5f,-21));
  Capture(c,"Reactor",new Vector3(15,9,-12),new Vector3(0,8,11));
  Capture(c,"ResearchWing",new Vector3(8,6,-32),new Vector3(0,1,-54));
  Object.DestroyImmediate(go);var sh=Shader.Find("Borrowed Time/Factory worn surface");File.WriteAllText(Out+"/Shader.txt","supported="+sh.isSupported+" passes="+sh.passCount+"\n"+string.Join("\n",ShaderUtil.GetShaderMessages(sh).Select(m=>m.severity+" "+m.message+" "+m.file+":"+m.line)));
 }
 public static void Capture(Camera c,string name,Vector3 p,Vector3 look){c.transform.position=p;c.transform.LookAt(look);var rt=new RenderTexture(1440,900,24);var old=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1440,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1440,900),0,0);t.Apply();File.WriteAllBytes(Out+"/"+name+".png",t.EncodeToPNG());c.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(rt);Object.DestroyImmediate(t);}
}}
#endif
