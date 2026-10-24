using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OopsPlanet {
public class BossServiceStation:MonoBehaviour,IElementReceiver{public FactoryRepairBoss boss;public int station;public void OnElement(BlobProperty e,string combo,Vector3 point){boss.Service(station,e,combo);}}
}
