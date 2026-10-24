using UnityEngine;

namespace BorrowedTime.Menu
{
    /// <summary>Near-overhead framing for the actual mesh meadow, with space for a future model.</summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class MeadowMenuView : MonoBehaviour
    {
        [Range(65, 90)] public float downwardAngle = 78;
        public float cameraHeight = 22;
        public float maximumOrthoHeight = 9.6f;
        public float halfWidthLimit = 16.4f;
        public Vector3 target = Vector3.zero;
        public Transform characterPlacement;
        public Vector2 characterViewport = new Vector2(.31f, .43f);
        public float characterGroundHeight = .19f;
        Camera view;
        void OnEnable() { view = GetComponent<Camera>(); UpdateFraming(); }
        void LateUpdate() => UpdateFraming();
        public void RefreshFraming() => UpdateFraming();
        void UpdateFraming()
        {
            if (!view) view = GetComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = Mathf.Min(maximumOrthoHeight, halfWidthLimit / Mathf.Max(.3f, view.aspect));
            transform.position = target + new Vector3(0, cameraHeight, -cameraHeight / Mathf.Tan(downwardAngle * Mathf.Deg2Rad));
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
            if (characterPlacement)
            {
                Ray ray = view.ViewportPointToRay(new Vector3(characterViewport.x, characterViewport.y, 0));
                var ground = new Plane(Vector3.up, new Vector3(0, characterGroundHeight, 0));
                if (ground.Raycast(ray, out float distance)) characterPlacement.position = ray.GetPoint(distance);
            }
        }
    }
}
