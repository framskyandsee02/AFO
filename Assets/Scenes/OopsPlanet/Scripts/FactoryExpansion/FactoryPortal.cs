using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OopsPlanet {
public class FactoryPortal : MonoBehaviour {
    public BlobWorld world;public bool toInterior;public Vector3 size=new Vector3(3,3,1.8f);float ready;
    void Start(){ready=Time.unscaledTime+1.2f;}
    void Update(){if(!world||!world.player||FactoryTravel.Busy||Time.unscaledTime<ready||world.player.paused)return;if(new Bounds(transform.position,size).Contains(world.player.transform.position+Vector3.up*.6f))FactoryTravel.Go(world,toInterior);}
}
}
