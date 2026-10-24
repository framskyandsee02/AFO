#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
namespace OopsPlanet.Editor {
public static class FactoryExpansionCapture {
 public static string Take(string name,Vector3 at,Vector3 look,float ortho=0){
  string folder="Assets/Scenes/OopsPlanet/Evidence/FactoryExpansion";Directory.CreateDirectory(folder);
  var host=new GameObject("Temporary evidence camera");var c=host.AddComponent<Camera>();c.transform.position=at;c.transform.LookAt(look);c.fieldOfView=66;c.nearClipPlane=.1f;c.farClipPlane=350;c.clearFlags=CameraClearFlags.Skybox;c.orthographic=ortho>0;c.orthographicSize=ortho;
  var rt=new RenderTexture(1600,900,24);var previous=RenderTexture.active;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
  try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(folder+"/"+name+".png",tex.EncodeToPNG());}
  finally{RenderTexture.active=previous;c.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(host);}
  return Path.GetFullPath(folder+"/"+name+".png");
 }
}
}
#endif
