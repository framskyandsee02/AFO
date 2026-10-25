using UnityEngine;
namespace BorrowedTime {
public class BTSkyCityPropPresentation:MonoBehaviour {
 public BTSkyCityGame game;public Transform generatorRotor,valveWheel,organic;
 public GameObject supportIntact,supportBroken,repairBand;public Transform[] batteryLights;
 void LateUpdate(){if(!game||game.nodes==null)return;if(generatorRotor&&game.GeneratorReady)generatorRotor.Rotate(Vector3.right,Time.deltaTime*95,Space.Self);if(valveWheel)valveWheel.localRotation=Quaternion.Euler(0,0,(1-game.nodes[2].value)*270);if(repairBand)repairBand.SetActive(true);if(batteryLights!=null)foreach(var t in batteryLights)if(t)t.localScale=new Vector3(1,Mathf.Max(.06f,game.nodes[11].value),1);}
}
}
