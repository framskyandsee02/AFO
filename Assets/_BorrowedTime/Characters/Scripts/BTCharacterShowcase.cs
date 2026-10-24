using UnityEngine;
using UnityEngine.InputSystem;
namespace BorrowedTime.Characters {
public class BTCharacterShowcase : MonoBehaviour {
    public BTCharacterModel hero,robo;
    public Transform turntable;
    public Font font;
    public Camera previewCamera;
    public bool autoRotate;
    public BTCharacterArticulation articulation;
    bool leftSide,keyboardDriving,powerDriving;
    int page,rightMode;
    readonly string[] jointNames={"UpperArm","LowerArm","UpperLeg","LowerLeg"};
    readonly string[] jointLabels={"ต้นแขน","ท่อนแขน / ศอก","ต้นขา","หน้าแข้ง / เข่า"};
    readonly string[] faces={"Neutral","Serious","Surprised","Smile","Thinking","Tired","Concerned","Sad","Blink"};
    readonly string[] roboFaces={"Happy","Neutral","Excited","Warning","Sad","Question","Angry","Sleep","Blink"};
    GUIStyle label,button,heading;
    void Start(){hero.PlayPose("Idle");robo.PlayPose("Hover");robo.SetFace("Happy");Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    void Update(){
        var keys=Keyboard.current;var mouse=Mouse.current;
        if(!articulation||!articulation.manualPose){
            var clip=hero.GetClip(hero.currentPose);bool busy=clip!=null&&!clip.loop;
            if(mouse!=null && mouse.rightButton.isPressed){
                float wheel=mouse.scroll.y.ReadValue();string pose=mouse.leftButton.isPressed?"Absorb":wheel>0?"Accelerate":wheel<0?"Rewind":null;
                if(pose!=null){powerDriving=true;if(hero.currentPose!=pose)hero.PlayPose(pose);}
            }else if(powerDriving){powerDriving=false;hero.PlayPose("Idle");}
            else if(keys!=null){
                Vector2 move=new Vector2((keys.dKey.isPressed?1:0)-(keys.aKey.isPressed?1:0),(keys.wKey.isPressed?1:0)-(keys.sKey.isPressed?1:0));
                if(keys.spaceKey.wasPressedThisFrame&&!busy){hero.PlayPose("JumpStart");keyboardDriving=false;}
                else if(!busy && move.sqrMagnitude>.01f){keyboardDriving=true;hero.SetLocomotion(move,keys.leftShiftKey.isPressed,keys.leftCtrlKey.isPressed);}
                else if(!busy && keyboardDriving){keyboardDriving=false;hero.SetLocomotion(Vector2.zero,false,keys.leftCtrlKey.isPressed);}
            }
        }
        if(autoRotate)turntable.Rotate(0,22*Time.deltaTime,0);
        if(mouse!=null && mouse.leftButton.isPressed&&!mouse.rightButton.isPressed && mouse.position.x.ReadValue()>250&&mouse.position.x.ReadValue()<Screen.width-250)turntable.Rotate(0,-mouse.delta.x.ReadValue()*.35f,0);
    }
    bool PoseButton(Rect rect,string text,bool active){Color previous=GUI.backgroundColor;if(active)GUI.backgroundColor=new Color(.30f,.90f,.72f);bool clicked=GUI.Button(rect,text,button);GUI.backgroundColor=previous;return clicked;}
    void OnGUI(){
        if(label==null){label=new GUIStyle(GUI.skin.label){font=font,fontSize=17,wordWrap=true};heading=new GUIStyle(label){fontSize=24,fontStyle=FontStyle.Bold};button=new GUIStyle(GUI.skin.button){font=font,fontSize=17};}
        GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1440f,Screen.height/900f,1));
        GUI.Box(new Rect(20,18,1400,104),"");GUI.Label(new Rect(42,30,1300,40),"BORROWED TIME / CHARACTER STUDIO",heading);
        GUI.Label(new Rect(42,76,1300,35),"ผู้เล่น "+hero.clips.Length+" ท่า • Robo "+robo.clips.Length+" ท่า • เลือกท่า / สีหน้า • ลากตรงกลางเพื่อหมุน",label);
        bool manual=articulation&&articulation.manualPose;
        GUI.Box(new Rect(20,145,215,520),"");GUI.Label(new Rect(38,158,185,35),manual?"ขยับข้อต่อ":"ท่าผู้เล่น",heading);
        if(manual){
            if(GUI.Button(new Rect(38,205,87,36),leftSide?"ซ้าย ✓":"ซ้าย",button))leftSide=true;
            if(GUI.Button(new Rect(131,205,87,36),leftSide?"ขวา":"ขวา ✓",button))leftSide=false;
            string side=leftSide?".L":".R";
            for(int i=0;i<jointNames.Length;i++){
                string name=jointNames[i]+side;float value=articulation.GetAngle(name);
                GUI.Label(new Rect(38,250+i*70,183,30),jointLabels[i]+" "+Mathf.RoundToInt(value)+"°",label);
                articulation.SetAngle(name,GUI.HorizontalSlider(new Rect(38,285+i*70,180,20),value,i==0?-60:i==2?-50:0,i==0?110:i==2?100:135));
            }
        }else{
            int pages=Mathf.Max(1,Mathf.CeilToInt(hero.clips.Length/7f));page=Mathf.Clamp(page,0,pages-1);
            if(GUI.Button(new Rect(38,200,86,34),"◀ "+(page+1),button))page=(page+pages-1)%pages;
            if(GUI.Button(new Rect(132,200,86,34),pages+" ▶",button))page=(page+1)%pages;
            for(int i=0;i<7;i++){
                int index=page*7+i;if(index>=hero.clips.Length)break;var spec=hero.clips[index];
                if(PoseButton(new Rect(38,245+i*41,180,36),spec.label,hero.currentPose==spec.name)){keyboardDriving=false;powerDriving=false;hero.PlayPose(spec.name);}
            }
        }
        if(GUI.Button(new Rect(38,550,180,42),autoRotate?"หยุดหมุน":"หมุนรอบตัว",button))autoRotate=!autoRotate;
        if(articulation && GUI.Button(new Rect(38,610,180,42),manual?"กลับสู่แอนิเมชัน":"ขยับข้อต่อเอง",button)){keyboardDriving=false;powerDriving=false;articulation.SetManualPose(!manual);}
        if(GUI.Button(new Rect(20,680,215,46),"กระพริบตา",button)){if(hero.currentFace=="Blink")hero.SetFace("Neutral");if(robo.currentFace=="Blink")robo.SetFace("Happy");hero.BlinkNow();robo.BlinkNow();}
        if(articulation && GUI.Button(new Rect(20,740,215,46),"ท่างอแขน / ขา",button)){
            articulation.SetManualPose(true);articulation.ResetAngles();
            foreach(string side in new[]{"R","L"}){articulation.SetAngle("UpperArm."+side,35);articulation.SetAngle("LowerArm."+side,90);articulation.SetAngle("UpperLeg."+side,side=="L"?45:0);articulation.SetAngle("LowerLeg."+side,side=="L"?85:15);}
        }
        GUI.Box(new Rect(1195,145,225,675),"");GUI.Label(new Rect(1212,158,190,35),"สีหน้า / Robo",heading);
        string[] tabs={"ผู้เล่น","Robo","ท่า Robo"};for(int i=0;i<3;i++)if(PoseButton(new Rect(1206+i*68,205,64,34),tabs[i],rightMode==i))rightMode=i;
        if(rightMode==2){for(int i=0;i<robo.clips.Length;i++){var spec=robo.clips[i];if(PoseButton(new Rect(1212,255+i*45,190,39),spec.label,robo.currentPose==spec.name))robo.PlayPose(spec.name);}}
        else{var list=rightMode==0?faces:roboFaces;var model=rightMode==0?hero:robo;for(int i=0;i<list.Length;i++)if(PoseButton(new Rect(1212,255+i*47,190,39),list[i],model.currentFace==list[i]))model.SetFace(list[i]);}
        var current=hero.GetClip(hero.currentPose);GUI.Label(new Rect(1212,735,190,70),current==null?hero.currentPose:current.category+"\n"+current.label,label);
        GUI.Box(new Rect(260,820,920,65),"");GUI.Label(new Rect(280,828,880,55),"WASD เดิน • Shift วิ่ง • Ctrl ย่อ • Space กระโดด\nRMB + ล้อเมาส์: ถอยคืนสภาพ / ดันเร่งสภาพ • RMB + LMB ดูดพลัง",label);
    }
}
}
