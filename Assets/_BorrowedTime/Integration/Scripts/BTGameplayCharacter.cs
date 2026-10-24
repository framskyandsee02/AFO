using UnityEngine;
namespace BorrowedTime.Characters {
// Presentation adapter for the existing puzzle controller. Movement, targeting,
// energy, saves and scene progression continue to belong to BTPlayer/the level.
[DefaultExecutionOrder(40)]
public class BTGameplayCharacter : MonoBehaviour {
    public BTPlayer player;
    public BTCharacterModel hero,robo;
    public Transform legacyVisual;
    Vector3 previousPosition;
    bool wasGrounded,groundSample,previousSeed,previousPlant,celebrated;
    string heldPose;
    float heldUntil;
    public float PlanarSpeed {get;private set;}
    void Start(){
        previousPosition=transform.position;
        previousSeed=player.game.hasMiracleSeed;
        var factory=player.game as BTFactoryGame;previousPlant=factory && factory.seedPlanted;
        hero.autoReturn=false;robo.autoReturn=false;
        hero.animator.applyRootMotion=false;robo.animator.applyRootMotion=false;
        Pose(hero,"Idle");Pose(robo,"Hover");robo.SetFace("Happy");
    }
    static void Pose(BTCharacterModel model,string pose){if(model && model.currentPose!=pose)model.PlayPose(pose);}
    void Hold(string pose,float duration){heldPose=pose;heldUntil=Time.time+duration;}
    void LateUpdate(){
        if(!player || !hero || !robo || !player.game)return;
        var game=player.game;Vector3 delta=transform.position-previousPosition;previousPosition=transform.position;
        PlanarSpeed=new Vector2(delta.x,delta.z).magnitude/Mathf.Max(.0001f,Time.deltaTime);
        if(delta.magnitude>5){heldUntil=0;groundSample=false;PlanarSpeed=0;}
        hero.animator.speed=robo.animator.speed=player.paused?0:1;
        if(player.paused)return;
        if(game.stage==0 || player.overview){Pose(hero,"Idle");Pose(robo,"Hover");return;}
        // Rounded paths and descending steps can drop isGrounded for a frame.
        // Keep the gait on nearby support while preserving real upward jumps.
        bool grounded=player.Controller && (player.Controller.isGrounded ||
            (player.velocity.y<=0 && Physics.SphereCast(transform.position+Vector3.up*.30f,.20f,Vector3.down,out _, .23f,~(1<<2),QueryTriggerInteraction.Ignore)));
        var input=player.CurrentInput;
        var factory=game as BTFactoryGame;bool planted=factory && factory.seedPlanted;
        if(planted&&!previousPlant)Hold("PlantSeed",2.5f);
        else if(game.hasMiracleSeed&&!previousSeed)Hold("Pickup",.7f);
        else if(input.interact)Hold("Interact",.55f);
        previousSeed=game.hasMiracleSeed;previousPlant=planted;
        if(game.completed){
            // Training/city complete just before the next scene; the final seed
            // planting keeps its own animation before the celebration.
            if(Time.time<heldUntil && heldPose=="PlantSeed")Pose(hero,heldPose);
            else if(!celebrated){Pose(hero,"Celebrate");celebrated=true;heldUntil=Time.time+3;}
            else if(Time.time>=heldUntil){Pose(hero,"Idle");hero.SetFace("Smile");}
            Pose(robo,"Happy");return;
        }
        celebrated=false;
        if(!grounded && (groundSample || Mathf.Abs(player.velocity.y)>1)){
            Pose(hero,player.velocity.y>0?"Jump":"Fall");heldUntil=0;
        }else if(grounded&&groundSample&&!wasGrounded){Hold("Land",.20f);Pose(hero,"Land");}
        else {
            if(input.aim&&player.target&&player.beam&&player.beam.enabled){
                if(input.absorb)Hold("Absorb",.15f);
                else if(input.scroll<0)Hold("Rewind",.65f);
                else if(input.scroll>0)Hold("Accelerate",.65f);
            }
            if(Time.time<heldUntil)Pose(hero,heldPose);
            else if(input.aim)Pose(hero,"Aim");
            else if(PlanarSpeed>.12f)hero.SetLocomotionSpeed(input.sprint?2:1);
            else hero.SetLocomotionSpeed(0);
        }
        wasGrounded=grounded;groundSample=true;
        Pose(robo,input.aim?"Scan":PlanarSpeed>.2f?"Follow":"Hover");
        if(robo.currentPose=="Hover")robo.SetFace("Happy");
        // The game computes beams in Update. Bones are animated afterwards, so
        // move only the beam's start to the current gauntlet pose in LateUpdate.
        if(player.beam&&player.beam.enabled&&player.rightHand)player.beam.SetPosition(0,player.rightHand.position);
    }
}
}
