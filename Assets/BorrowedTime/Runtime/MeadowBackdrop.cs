using UnityEngine;

namespace BorrowedTime.Menu
{
    /// <summary>World-space painting behind future 3D characters; UI remains transparent.</summary>
    [ExecuteAlways]
    public sealed class MeadowBackdrop : MonoBehaviour
    {
        public Camera menuCamera;
        public Transform background;
        public Transform characterPlacement;
        public Vector2 imageSize = new Vector2(1586, 992);
        public Vector2 characterViewport = new Vector2(.31f, .43f);
        void LateUpdate()
        {
            if (!menuCamera || !background) return;
            float aspect = imageSize.x / Mathf.Max(1, imageSize.y);
            float visibleHeight = 2 * menuCamera.orthographicSize;
            float height = Mathf.Max(visibleHeight, visibleHeight * menuCamera.aspect / aspect);
            background.localScale = new Vector3(height * aspect, height, 1);
            if (characterPlacement) characterPlacement.position = menuCamera.ViewportToWorldPoint(new Vector3(characterViewport.x, characterViewport.y, 12));
        }
    }
}
