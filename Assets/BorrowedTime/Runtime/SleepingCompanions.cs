using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BorrowedTime.Menu
{
    /// <summary>Posed menu characters. Only the human wakes when a menu button is hovered.</summary>
    [ExecuteAlways, DefaultExecutionOrder(200)]
    public sealed class SleepingCompanions : MonoBehaviour
    {
        public UIDocument menuDocument;
        public Camera menuCamera;
        [Min(.1f)] public float referenceScale = 4.2f;
        public Transform human, robot;
        public SkinnedMeshRenderer humanFace, robotFace;
        [Min(.05f)] public float eyeTransitionSeconds = .24f;
        public Vector3 humanRestPosition, robotRestPosition;
        public float EyeClosure { get; private set; } = 1;
        public bool IsButtonHovered => hovered.Count > 0;

        readonly List<Button> buttons = new List<Button>();
        readonly HashSet<VisualElement> hovered = new HashSet<VisualElement>();
        VisualElement boundRoot;
        int humanBlink = -1, robotSleep = -1;

        public void InitializeRestPose()
        {
            if (human) humanRestPosition = human.localPosition;
            if (robot) robotRestPosition = robot.localPosition;
            humanBlink = FindExpression(humanFace, "Blink");
            robotSleep = FindExpression(robotFace, "Sleep");
            EyeClosure = 1;
            ApplyEyes();
        }

        public static int FindExpression(SkinnedMeshRenderer face, string expression)
        {
            if (!face || !face.sharedMesh) return -1;
            for (int i = 0; i < face.sharedMesh.blendShapeCount; i++)
            {
                string name = face.sharedMesh.GetBlendShapeName(i);
                if (name == expression || name.EndsWith("." + expression)) return i;
            }
            return -1;
        }

        void OnEnable()
        {
            humanBlink = FindExpression(humanFace, "Blink");
            robotSleep = FindExpression(robotFace, "Sleep");
            EyeClosure = 1;
            ApplyEyes();
        }

        void BindButtons()
        {
            if (!menuDocument) return;
            var root = menuDocument.rootVisualElement;
            if (root == boundRoot && buttons.Count > 0) return;
            ReleaseButtons();
            boundRoot = root;
            foreach (string name in new[] { "play-button", "continue-button", "setting-button", "quit-button", "back-button" })
            {
                var button = root.Q<Button>(name);
                if (button == null) continue;
                buttons.Add(button);
                button.RegisterCallback<PointerEnterEvent>(PointerEntered);
                button.RegisterCallback<PointerLeaveEvent>(PointerLeft);
            }
        }

        void PointerEntered(PointerEnterEvent e) => hovered.Add((VisualElement)e.currentTarget);
        void PointerLeft(PointerLeaveEvent e) => hovered.Remove((VisualElement)e.currentTarget);

        void ReleaseButtons()
        {
            foreach (var button in buttons)
            {
                button.UnregisterCallback<PointerEnterEvent>(PointerEntered);
                button.UnregisterCallback<PointerLeaveEvent>(PointerLeft);
            }
            buttons.Clear(); hovered.Clear(); boundRoot = null;
        }

        void ApplyEyes()
        {
            if (humanFace && humanBlink >= 0) humanFace.SetBlendShapeWeight(humanBlink, EyeClosure * 100);
            if (robotFace && robotSleep >= 0) robotFace.SetBlendShapeWeight(robotSleep, 100);
        }

        void LateUpdate()
        {
            if (menuCamera)
            {
                float visibleWidth = 2 * menuCamera.orthographicSize * menuCamera.aspect;
                transform.localScale = Vector3.one * (referenceScale * Mathf.Min(1, visibleWidth / 32.8f));
            }
            if (Application.isPlaying)
            {
                BindButtons();
                EyeClosure = Mathf.MoveTowards(EyeClosure, IsButtonHovered ? 0 : 1,
                    Time.unscaledDeltaTime / Mathf.Max(.05f, eyeTransitionSeconds));
                float breath = Mathf.Sin(Time.unscaledTime * 1.45f);
                if (human) human.localPosition = humanRestPosition + Vector3.up * (breath * .004f);
                if (robot) robot.localPosition = robotRestPosition + Vector3.up * (Mathf.Sin(Time.unscaledTime * 1.2f + 1) * .003f);
            }
            else EyeClosure = 1;
            ApplyEyes();
        }

        void OnApplicationFocus(bool focused) { if (!focused) hovered.Clear(); }
        void OnDisable()
        {
            ReleaseButtons(); EyeClosure = 1; ApplyEyes();
            if (human) human.localPosition = humanRestPosition;
            if (robot) robot.localPosition = robotRestPosition;
        }
    }
}
