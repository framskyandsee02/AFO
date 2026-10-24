using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OopsPlanet {
public class FactoryCheckpoint : MonoBehaviour {
    public BlobWorld world;public Vector3 respawn;public Vector3 size=new Vector3(5,3,5);bool visited;
    void Update(){if(!world||!world.player||world.player.paused)return;if(new Bounds(transform.position,size).Contains(world.player.transform.position+Vector3.up*.5f)){world.checkpoint=respawn;if(!visited){visited=true;world.player.Notify("จุดพักโรงงาน — พลาดแล้วกลับมาที่นี่",3);}}}
}
}
