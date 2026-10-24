using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OopsPlanet {
[DefaultExecutionOrder(-700)]
public class FactorySceneLink : MonoBehaviour {
    public BlobWorld world;
    void Awake(){if(world && FactoryTravel.Arriving && !string.IsNullOrEmpty(FactoryTravel.Slot))world.saveSlot=FactoryTravel.Slot;}
    IEnumerator Start(){yield return null;if(!world || !FactoryTravel.Arriving)yield break;world.player.Teleport(FactoryTravel.Arrival);world.player.yaw=FactoryTravel.ArrivalYaw;world.player.pitch=18;world.checkpoint=FactoryTravel.Arrival;world.player.SetProperties(FactoryTravel.First,FactoryTravel.Second);FactoryTravel.Arriving=false;}
}
}
