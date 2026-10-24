#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BorrowedTime.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace BorrowedTime.Editor
{
    [InitializeOnLoad]
    public static class SleepingCompanionsValidation
    {
        const string Active = "SleepingCompanions.Validation.Active";
        const string ReportPath = "Art/BorrowedTime/SleepingCompanions/Validation.json";
        [Serializable] sealed class Check { public string name; public bool passed; }
        [Serializable] sealed class Report { public bool completed, success; public List<Check> checks = new List<Check>(); }
        static Report report;
        static int stage;
        static double next, started;
        static UIDocument preview;
        static PanelSettings panel;
        static RenderTexture texture, previousTexture;
        static Camera camera;
        static SleepingCompanions companions;

        static SleepingCompanionsValidation() { EditorApplication.update += Tick; }

        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Begin companion validation in Edit Mode.");
            Directory.CreateDirectory("Art/BorrowedTime/SleepingCompanions");
            report = new Report(); Save();
            stage = 0; started = 0; next = 0;
            SessionState.SetBool(Active, true);
            SessionState.SetBool(Active + ".Background", Application.runInBackground);
            Application.runInBackground = true;
            EditorSceneManager.OpenScene(BorrowedMenuBuilder.ScenePath);
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Active, false)) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (report == null) report = JsonUtility.FromJson<Report>(File.ReadAllText(ReportPath));
            if (started == 0) started = EditorApplication.timeSinceStartup;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 90) throw new Exception("Companion validation timed out.");
                if (EditorApplication.timeSinceStartup < next) return;
                companions = Object.FindFirstObjectByType<SleepingCompanions>();
                if (!companions || companions.menuDocument.rootVisualElement.panel == null) return;
                var root = companions.menuDocument.rootVisualElement;
                if (stage == 0)
                {
                    CheckResult(companions.human && companions.robot, "Both imported models are present in the menu");
                    CheckResult(companions.GetComponentsInChildren<Animator>().All(a => !a.enabled), "Sleeping poses are not overwritten by animation controllers");
                    CheckResult(companions.GetComponentsInChildren<SkinnedMeshRenderer>().All(r => r.sharedMaterials.All(m => m && m.shader && m.shader.name != "Hidden/InternalErrorShader")), "Both characters retain their imported materials");
                    PreparePreview();
                }
                else if (stage == 1)
                {
                    foreach (var model in new[] { companions.human, companions.robot })
                    {
                        var bounds = SleepingCompanionsSetup.BakedBounds(model.gameObject);
                        var center = camera.WorldToViewportPoint(bounds.center);
                        CheckResult(bounds.min.y > .08f && bounds.min.y < .65f && bounds.max.y < 4,
                            model.name + " lies on the meadow surface: " + bounds);
                        CheckResult(center.z > 0 && center.z < camera.farClipPlane && center.x > .03f && center.x < .57f && center.y > .10f && center.y < .90f,
                            model.name + " is visible beside the menu: " + center);
                    }
                    CheckResult(companions.EyeClosure > .99f, "Human starts asleep");
                    CheckResult(Weight(companions.humanFace, "Blink") > 99, "Actual human eyelid blend shape is closed");
                    CheckResult(Weight(companions.robotFace, "Sleep") > 99, "Robot keeps its sleeping expression");
                    Capture("Menu_Asleep");
                    Hover(root.Q<Button>("play-button"), true);
                }
                else if (stage == 2)
                {
                    CheckResult(companions.IsButtonHovered && Weight(companions.humanFace, "Blink") < 1, "Pointer entering Play opens the actual human eyes");
                    Capture("Menu_HoverAwake");
                    Hover(root.Q<Button>("play-button"), false);
                }
                else if (stage == 3)
                {
                    CheckResult(!companions.IsButtonHovered && Weight(companions.humanFace, "Blink") > 99, "Pointer leaving Play returns the human to sleep");
                    Hover(root.Q<Button>("setting-button"), true);
                }
                else if (stage == 4)
                {
                    CheckResult(Weight(companions.humanFace, "Blink") < 1, "Settings hover also opens the eyes");
                    Hover(root.Q<Button>("quit-button"), true);
                    Hover(root.Q<Button>("setting-button"), false);
                }
                else if (stage == 5)
                {
                    CheckResult(companions.IsButtonHovered && Weight(companions.humanFace, "Blink") < 1, "Moving directly between buttons keeps the human awake");
                    Hover(root.Q<Button>("quit-button"), false);
                }
                else if (stage == 6)
                {
                    CheckResult(Weight(companions.humanFace, "Blink") > 99 && Weight(companions.robotFace, "Sleep") > 99, "Both companions settle back to sleep after Exit hover ends");
                    Capture("Menu_ReturnToSleep");
                    Cleanup();
                    PreparePreview(720, 1280);
                }
                else if (stage == 7)
                {
                    foreach (var model in new[] { companions.human, companions.robot })
                    {
                        var bounds = SleepingCompanionsSetup.BakedBounds(model.gameObject);
                        var min = camera.WorldToViewportPoint(bounds.min);
                        var max = camera.WorldToViewportPoint(bounds.max);
                        CheckResult(min.x > 0 && max.x < .55f && min.y > .10f && max.y < .90f,
                            model.name + " remains beside the buttons in portrait layout");
                    }
                    Capture("Menu_Portrait");
                    Cleanup();
                    report.completed = true; report.success = report.checks.All(c => c.passed); Save();
                    SessionState.SetBool(Active, false);
                    Application.runInBackground = SessionState.GetBool(Active + ".Background", false);
                    EditorApplication.isPlaying = false;
                    Debug.Log("[Borrowed Time] Sleeping companions validation: " + report.success);
                }
                stage++; next = EditorApplication.timeSinceStartup + .65;
            }
            catch (Exception e)
            {
                CheckResult(false, e.ToString()); Cleanup(); report.completed = true; report.success = false; Save();
                SessionState.SetBool(Active, false);
                Application.runInBackground = SessionState.GetBool(Active + ".Background", false);
                EditorApplication.isPlaying = false; Debug.LogException(e);
            }
        }

        static float Weight(SkinnedMeshRenderer face, string expression) => face.GetBlendShapeWeight(SleepingCompanions.FindExpression(face, expression));

        static void Hover(Button button, bool entered)
        {
            if (entered) { using (var e = PointerEnterEvent.GetPooled()) { e.target = button; button.SendEvent(e); } }
            else { using (var e = PointerLeaveEvent.GetPooled()) { e.target = button; button.SendEvent(e); } }
        }

        static void PreparePreview(int width = 1920, int height = 1080)
        {
            camera = Camera.main; previousTexture = camera.targetTexture;
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); texture.Create(); camera.targetTexture = texture;
            camera.GetComponent<MeadowMenuView>().RefreshFraming();
            panel = Object.Instantiate(companions.menuDocument.panelSettings); panel.targetTexture = texture; panel.clearColor = false;
            var host = new GameObject("Temporary sleeping companions preview") { hideFlags = HideFlags.HideAndDontSave };
            preview = host.AddComponent<UIDocument>(); preview.panelSettings = panel;
            preview.visualTreeAsset = companions.menuDocument.visualTreeAsset;
            BorrowedMainMenu.InstallArtwork(preview.rootVisualElement.Q("menu-root"));
            preview.rootVisualElement.Q("foreground-foliage").style.display = DisplayStyle.None;
        }

        static void Capture(string name)
        {
            var previous = RenderTexture.active; var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply();
                File.WriteAllBytes("Art/BorrowedTime/SleepingCompanions/" + name + ".png", image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.DestroyImmediate(image); }
        }

        static void Cleanup()
        {
            if (camera) { camera.targetTexture = previousTexture; camera.GetComponent<MeadowMenuView>().RefreshFraming(); }
            if (preview) Object.DestroyImmediate(preview.gameObject);
            if (panel) Object.DestroyImmediate(panel);
            if (texture) { texture.Release(); Object.DestroyImmediate(texture); }
            preview = null; panel = null; texture = null;
        }

        static void CheckResult(bool passed, string name) { report.checks.Add(new Check { passed = passed, name = name }); Save(); }
        static void Save() { File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true)); }
    }
}
#endif
