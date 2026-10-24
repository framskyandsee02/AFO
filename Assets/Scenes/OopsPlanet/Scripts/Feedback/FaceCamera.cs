using UnityEngine;
namespace OopsPlanet {
// Keeps world-space labels facing the camera.
public class FaceCamera : MonoBehaviour {
    Transform cam;
    void LateUpdate() { if(!cam) { var c=Camera.main; if(!c) return; cam=c.transform; } transform.rotation=cam.rotation; }
}
}
