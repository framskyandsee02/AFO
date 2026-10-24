using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OopsPlanet {
public class FactoryTravel : MonoBehaviour {
    public const string Outside="Assets/Scenes/OopsPlanet/Scenes/OopsPlanet_V2.unity";
    public const string Interior="Assets/Scenes/OopsPlanet/Scenes/OopsPlanet_Factory.unity";
    public static string Slot;
    public static BlobProperty First,Second;
    public static bool Arriving,Busy;
    public static Vector3 Arrival;
    public static float ArrivalYaw;
    float alpha; bool error; string message="กำลังเข้าสู่โรงงาน…";
    public static void Go(BlobWorld world,bool inside) {
        if(Busy || !world || !world.player)return;
        if(!world.Save()){world.player.Notify("บันทึกไม่สำเร็จ — ยังไม่เปลี่ยนฉาก");return;}
        Busy=true;Slot=world.saveSlot;First=world.player.property;Second=world.player.secondary;
        Arrival=inside?new Vector3(0,.1f,32):new Vector3(0,.1f,23);
        ArrivalYaw=inside?0:180;Arriving=true;
        var go=new GameObject("Factory scene transition");DontDestroyOnLoad(go);
        go.AddComponent<FactoryTravel>().StartCoroutine(Load(world,inside));
    }
    static IEnumerator Load(BlobWorld world,bool inside) {
        var host=FindFirstObjectByType<FactoryTravel>();world.player.paused=true;
        host.message=inside?"กำลังเข้าสู่โรงงาน…":"กำลังกลับสู่ฮับ…";
        for(float t=0;t<.3f;t+=Time.unscaledDeltaTime){host.alpha=t/.3f;yield return null;}
        host.alpha=1;
        AsyncOperation op=null;
        try {op=SceneManager.LoadSceneAsync(inside?Interior:Outside,LoadSceneMode.Single);}
        catch(System.Exception e){Debug.LogError("FACTORY_TRAVEL "+e);}
        if(op==null){world.player.paused=false;host.error=true;host.message="โหลดฉากไม่ได้ — กลับไปเล่นต่อได้";yield return new WaitForSecondsRealtime(1.5f);}
        else {while(!op.isDone)yield return null;yield return null;yield return null;}
        for(float t=0;t<.35f;t+=Time.unscaledDeltaTime){host.alpha=1-t/.35f;yield return null;}
        Busy=false;Destroy(host.gameObject);
    }
    void OnGUI(){if(alpha<=0)return;var old=GUI.color;GUI.color=new Color(.04f,.08f,.1f,alpha);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=new Color(1,1,1,alpha);var style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=24};var w=FindFirstObjectByType<BlobWorld>();if(w)style.font=w.uiFont;GUI.Label(new Rect(0,Screen.height*.45f,Screen.width,60),message,style);GUI.color=old;}
}
}
