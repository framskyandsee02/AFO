using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OopsPlanet {
public class BossJamReceiver:MonoBehaviour{public FactoryRepairBoss boss;void OnCollisionEnter(Collision c){boss.ScrapImpact(c.rigidbody,c.relativeVelocity.magnitude);}}
}
