using System;
using UnityEngine;
namespace BorrowedTime.Characters {
[DefaultExecutionOrder(100)]
public class BTCharacterArticulation : MonoBehaviour {
    [Serializable] public class Joint {
        public string name;
        public Transform bone,tip;
        public Quaternion rest;
        public Vector3 axis;
        public float min,max,angle;
    }
    public BTCharacterModel model;
    public Joint[] joints=new Joint[0];
    public bool manualPose;
    [SerializeField] Transform[] restBones;
    [SerializeField] Vector3[] restPositions;
    [SerializeField] Quaternion[] restRotations;
    bool resumeAnimator;
    public void CaptureRestPose(){
        restBones=model.animator.GetComponentsInChildren<Transform>(true);
        restPositions=new Vector3[restBones.Length];restRotations=new Quaternion[restBones.Length];
        for(int i=0;i<restBones.Length;i++){restPositions[i]=restBones[i].localPosition;restRotations[i]=restBones[i].localRotation;}
        foreach(var j in joints){
            j.rest=j.bone.localRotation;
            Vector3 direction=j.tip.position-j.bone.position;
            Vector3 hinge=Vector3.Cross(direction.normalized,transform.forward).normalized;
            if(j.name.StartsWith("LowerLeg"))hinge=-hinge;
            j.axis=Quaternion.Inverse(j.bone.rotation)*hinge;
        }
    }
    void Restore(){
        if(restBones==null)return;
        for(int i=0;i<restBones.Length;i++)if(restBones[i]){restBones[i].localPosition=restPositions[i];restBones[i].localRotation=restRotations[i];}
    }
    public void SetManualPose(bool enabled){
        if(manualPose==enabled)return;
        if(enabled){resumeAnimator=model.animator.enabled;model.animator.enabled=false;Restore();manualPose=true;ApplyPose();}
        else{manualPose=false;Restore();model.animator.enabled=resumeAnimator;if(resumeAnimator){model.animator.Rebind();model.PlayPose("Idle");}}
    }
    public void SetAngle(string name,float angle){foreach(var j in joints)if(j.name==name){j.angle=Mathf.Clamp(angle,j.min,j.max);return;}}
    public float GetAngle(string name){foreach(var j in joints)if(j.name==name)return j.angle;return 0;}
    public void ResetAngles(){foreach(var j in joints)j.angle=0;ApplyPose();}
    public void ApplyPose(){if(manualPose)foreach(var j in joints)j.bone.localRotation=j.rest*Quaternion.AngleAxis(j.angle,j.axis);}
    void LateUpdate(){ApplyPose();}
    void OnDisable(){if(manualPose && model && model.animator)SetManualPose(false);}
}
}
