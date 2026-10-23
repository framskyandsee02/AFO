#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace WobbleWorks.Characters.Editor
{
    [InitializeOnLoad]
    public static class PinoLocomotionValidation
    {
        const string ReportPath = "Assets/PinoCharacter/Evidence/Locomotion.json";
        const string BatchKey = "Pino.Locomotion.BatchRoute";
        const string FrameFolder = "Temp/PinoAllFormsLocomotionFrames";

        [Serializable] public sealed class Report
        {
            public string generatedUtc;
            public int passed, failed;
            public float maximumHopHeightMetres;
            public float gameplayHopHeightMetres;
            public float idleScaleVariation;
            public List<PinoCharacterValidation.Check> checks = new List<PinoCharacterValidation.Check>();
        }

        static PinoLocomotionValidation()
        {
            if (SessionState.GetBool(BatchKey, false)) EditorApplication.update += WaitForRoute;
        }

        public static void BuildValidateAndPlaytest()
        {
            try
            {
                PinoCharacterBuilder.BuildAll();
                PinoCharacterValidation.RunAll();
                RunAll();
                CapturePreview();
                PinoPlaymodeValidation.Start();
                SessionState.SetBool(BatchKey, true);
                EditorApplication.update += WaitForRoute;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        static void WaitForRoute()
        {
            const string path = "Assets/PinoCharacter/Evidence/PlaymodeIntegration.json";
            if (!SessionState.GetBool(BatchKey, false) || !File.Exists(path)) return;
            var report = JsonUtility.FromJson<PinoPlaymodeValidation.IntegrationReport>(File.ReadAllText(path));
            if (report == null || !report.completed) return;
            SessionState.SetBool(BatchKey, false);
            EditorApplication.update -= WaitForRoute;
            if (Application.isBatchMode) EditorApplication.Exit(report.passed ? 0 : 1);
        }

        [MenuItem("Tools/Pino Character/Validate Walk and Idle")]
        public static void RunAll()
        {
            var report = new Report { generatedUtc = DateTime.UtcNow.ToString("O") };
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PinoCharacterBuilder.PrefabPath), scene);
                var morph = instance.GetComponent<PinoMorphController>();
                var motion = instance.GetComponent<PinoLocomotionAnimator>();
                Check(motion && motion.MotionPivot, "Prefab contains configured motion pivot", "Upright pivot preserves the FBX axis correction", report);
                if (!motion || !motion.MotionPivot) throw new InvalidOperationException("Build the new Pino prefab first.");
                Transform pivot = motion.MotionPivot;
                Vector3 restPosition = pivot.localPosition, restScale = pivot.localScale;
                Quaternion restRotation = pivot.localRotation;
                Vector3 rootPosition = instance.transform.position, rootScale = instance.transform.localScale;
                morph.SetMorphForm(WobProperty.None);
                morph.IdleEnabled = true;
                float minScale = float.MaxValue, maxScale = float.MinValue, maxAngle = 0f, idleHop = 0f;
                for (int i = 0; i < 360; i++)
                {
                    morph.Tick(1f / 60f);
                    motion.Tick(1f / 60f, Vector3.zero, true);
                    minScale = Mathf.Min(minScale, pivot.localScale.y);
                    maxScale = Mathf.Max(maxScale, pivot.localScale.y);
                    maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(restRotation, pivot.localRotation));
                    idleHop = Mathf.Max(idleHop, motion.CurrentHopHeight);
                }
                report.idleScaleVariation = maxScale - minScale;
                Check(report.idleScaleVariation > .025f && report.idleScaleVariation < .1f && maxAngle > .8f,
                    "Idle has gentle squash and side-to-side jelly sway", "scaleRange=" + report.idleScaleVariation + ", angle=" + maxAngle, report);
                Check(idleHop == 0f && pivot.localPosition == restPosition, "Idle stays planted", "No repeated jumping while stationary", report);

                float maxHop = 0f, minHop = float.MaxValue;
                int contactFrames = 0;
                for (int i = 0; i < 240; i++)
                {
                    motion.Tick(1f / 60f, Vector3.forward * 5.5f, true);
                    maxHop = Mathf.Max(maxHop, motion.CurrentHopHeight);
                    minHop = Mathf.Min(minHop, motion.CurrentHopHeight);
                    if (motion.CurrentHopHeight < .001f) contactFrames++;
                }
                report.maximumHopHeightMetres = maxHop;
                Check(maxHop > .02f && maxHop < .035f && minHop >= 0f && contactFrames > 15,
                    "Walking makes repeated low hops with ground contact", "base height .52m: hop=" + maxHop + ", contactFrames=" + contactFrames, report);
                Check(Vector3.Distance(rootPosition, instance.transform.position) < .000001f && instance.transform.localScale == rootScale,
                    "Walking never moves or scales the player root", "Only the dedicated mesh pivot is animated", report);

                instance.transform.localScale = Vector3.one * (1.2f / .52f);
                float scaledHop = 0f;
                for (int i = 0; i < 120; i++)
                {
                    motion.Tick(1f / 60f, Vector3.forward * 5.5f, true);
                    scaledHop = Mathf.Max(scaledHop, instance.transform.TransformVector(Vector3.up * motion.CurrentHopHeight).magnitude);
                }
                report.gameplayHopHeightMetres = scaledHop;
                Check(scaledHop > .05f && scaledHop < .075f, "Gameplay-sized slime hops only about 6.6 cm", "hop=" + scaledHop, report);
                instance.transform.localScale = rootScale;
                Vector3 pausedPosition = pivot.localPosition, pausedScale = pivot.localScale;
                Quaternion pausedRotation = pivot.localRotation;
                motion.Tick(1f, Vector3.forward * 5.5f, true, true);
                Check(pivot.localPosition == pausedPosition && pivot.localScale == pausedScale && pivot.localRotation == pausedRotation,
                    "Pause freezes the walk and idle pose", "No presentation time advances", report);
                motion.Tick(1f / 60f, Vector3.forward * 5.5f, false);
                Check(motion.CurrentHopHeight == 0f, "Real jumps suppress the walking hop", "No extra airborne bounce", report);
                for (int i = 0; i < 60; i++) motion.Tick(1f / 60f, Vector3.zero, true);
                Check(motion.CurrentHopHeight == 0f, "Stopping returns smoothly to grounded idle", "The last walk pose is not left suspended", report);

                foreach (WobProperty form in new[] { WobProperty.Magnet, WobProperty.Spring, WobProperty.Gas, WobProperty.Liquid })
                {
                    morph.SetMorphForm(form);
                    motion.ResetMotion();
                    float idleMin = float.MaxValue, idleMax = float.MinValue, formHop = 0f;
                    for (int i = 0; i < 240; i++)
                    {
                        motion.Tick(1f / 60f, Vector3.zero, true);
                        idleMin = Mathf.Min(idleMin, pivot.localScale.y);
                        idleMax = Mathf.Max(idleMax, pivot.localScale.y);
                    }
                    Check(idleMax - idleMin > .01f, form + " has visible gentle idle motion", "scaleRange=" + (idleMax - idleMin), report);
                    for (int i = 0; i < 240; i++)
                    {
                        motion.Tick(1f / 60f, Vector3.forward * 5.5f, true);
                        formHop = Mathf.Max(formHop, motion.CurrentHopHeight);
                    }
                    Check(formHop > .002f && formHop < .04f, form + " has its own low walking bounce", "hopMetres=" + formHop, report);
                    Check(morph.GetMorphWeight(form.ToString()) > .999f && instance.transform.position == rootPosition,
                        form + " preserves ability geometry and player position", "Locomotion never writes ability weights or physics", report);
                }

                morph.SetMorphForm(WobProperty.Spring);
                morph.SetSpringCompression(1f); morph.Tick(0f);
                for (int i = 0; i < 60; i++) motion.Tick(1f / 60f, Vector3.forward * 5.5f, true);
                Check(motion.CurrentHopHeight == 0f && morph.GetMorphWeight("SpringCompress") > .999f,
                    "Fully charged spring suppresses the walking hop", "Charge compression remains intact", report);
                morph.SetMorphForm(WobProperty.Liquid);
                morph.SetLiquidFlow(1f); morph.Tick(0f);
                float flatHop = 0f, flatHeightError = 0f, flatTilt = 0f, flowWiggle = 0f;
                for (int i = 0; i < 240; i++)
                {
                    motion.Tick(1f / 60f, Vector3.forward * 3.5f, true);
                    flatHop = Mathf.Max(flatHop, motion.CurrentHopHeight);
                    flatHeightError = Mathf.Max(flatHeightError, Mathf.Abs(pivot.localScale.y - restScale.y));
                    flatTilt = Mathf.Max(flatTilt, Vector3.Angle(pivot.up, instance.transform.up));
                    flowWiggle = Mathf.Max(flowWiggle, Mathf.Abs(pivot.localScale.x - restScale.x));
                }
                Check(flatHop == 0f && flatHeightError < .000001f && flatTilt < .001f && flowWiggle > .005f,
                    "Flattened liquid wiggles sideways without gaining height", "hop=" + flatHop + ", heightError=" + flatHeightError + ", tilt=" + flatTilt, report);
                morph.SetMorphForm(WobProperty.Gas);
                float airborneBob = 0f;
                for (int i = 0; i < 180; i++)
                {
                    motion.Tick(1f / 60f, Vector3.zero, false);
                    airborneBob = Mathf.Max(airborneBob, motion.CurrentHopHeight);
                }
                Check(airborneBob > .002f && airborneBob < .012f && instance.transform.position == rootPosition,
                    "Gas keeps a small airborne bob without moving the player", "visualBobMetres=" + airborneBob, report);
                float largestStep = 0f;
                foreach (WobProperty form in new[] { WobProperty.Spring, WobProperty.Liquid, WobProperty.Magnet, WobProperty.None })
                {
                    morph.BlendToMorphForm(form, .4f);
                    for (int i = 0; i < 30; i++)
                    {
                        Vector3 before = pivot.localPosition;
                        morph.Tick(1f / 60f); motion.Tick(1f / 60f, Vector3.forward * 5.5f, true);
                        largestStep = Mathf.Max(largestStep, Vector3.Distance(before, pivot.localPosition));
                    }
                }
                Check(largestStep < .02f, "Locomotion blends smoothly between ability forms", "largestFrameOffset=" + largestStep, report);
                morph.SetMorphForm(WobProperty.None);
                morph.IdleEnabled = false;
                for (int i = 0; i < 60; i++) motion.Tick(1f / 60f, Vector3.zero, true);
                Check(Vector3.Distance(pivot.localScale, restScale) < .000001f && Quaternion.Angle(pivot.localRotation, restRotation) < .001f,
                    "Disabling idle permits an exact neutral preview", "Respects the existing IdleEnabled control", report);
                motion.Tick(.2f, Vector3.forward * 5.5f, true);
                motion.enabled = false;
                Check(pivot.localPosition == restPosition && pivot.localScale == restScale && pivot.localRotation == restRotation,
                    "Disabling the animator restores the mesh pivot", "No persistent transform offsets", report);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
                AssetDatabase.ImportAsset(ReportPath);
            }
            Debug.Log("PINO_LOCOMOTION_VALIDATION passed=" + report.passed + " failed=" + report.failed);
            if (report.failed > 0) throw new InvalidOperationException("Pino locomotion validation failed: " + ReportPath);
        }

        static void Check(bool passed, string name, string details, Report report)
        {
            report.checks.Add(new PinoCharacterValidation.Check { passed = passed, name = name, details = details });
            if (passed) report.passed++; else report.failed++;
        }

        [MenuItem("Tools/Pino Character/Capture Walk and Idle Preview")]
        public static void CapturePreview()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = null;
            Texture2D image = null;
            var disabledLights = new List<Light>();
            try
            {
                SceneManager.SetActiveScene(scene);
                ShaderUtil.allowAsyncCompilation = false;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PinoCharacterBuilder.PrefabPath);
                var characters = new PinoMorphController[10];
                var motions = new PinoLocomotionAnimator[10];
                for (int i = 0; i < characters.Length; i++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.transform.position = new Vector3((2 - i % 5) * 1.1f, 0, i < 5 ? -.9f : .9f);
                    characters[i] = instance.GetComponent<PinoMorphController>();
                    characters[i].SetMorphForm((WobProperty)(i % 5));
                    motions[i] = instance.GetComponent<PinoLocomotionAnimator>();
                    foreach (Transform child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
                }
                foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (light.gameObject.scene != scene && light.enabled) { disabledLights.Add(light); light.enabled = false; }
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.layer = 30;
                floor.transform.position = new Vector3(0, -.005f, 0);
                floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(PinoCharacterBuilder.Root + "/Materials/Pino_GalleryFloor.mat");
                var camera = new GameObject("Locomotion preview camera").AddComponent<Camera>();
                camera.transform.position = new Vector3(0, 5f, 7f);
                camera.transform.LookAt(new Vector3(0, .25f, 0));
                camera.orthographic = true; camera.orthographicSize = 1.75f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.85f, .92f, .94f);
                camera.cullingMask = 1 << 30;
                var key = new GameObject("Locomotion key").AddComponent<Light>();
                key.type = LightType.Directional; key.intensity = 1.3f; key.cullingMask = 1 << 30;
                key.shadows = LightShadows.Soft; key.shadowStrength = .55f;
                key.transform.rotation = Quaternion.Euler(45, -35, 0);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.4f, .45f, .5f);
                target = new RenderTexture(1500, 850, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                image = new Texture2D(1500, 850, TextureFormat.RGB24, false);
                Directory.CreateDirectory(FrameFolder);
                for (int frame = -30; frame < 72; frame++)
                {
                    for (int i = 0; i < characters.Length; i++)
                    {
                        characters[i].Tick(1f / 24f);
                        motions[i].Tick(1f / 24f, i < 5 ? Vector3.zero : Vector3.forward * 5.5f, true);
                    }
                    if (frame < 0) continue;
                    camera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 1500, 850), 0, 0); image.Apply();
                    File.WriteAllBytes(FrameFolder + "/frame_" + frame.ToString("D3") + ".png", image.EncodeToPNG());
                }
                Debug.Log("PINO_LOCOMOTION_PREVIEW_OK " + FrameFolder);
            }
            finally
            {
                RenderTexture.active = previousActive;
                foreach (Light light in disabledLights) if (light) light.enabled = true;
                if (image) Object.DestroyImmediate(image);
                if (target) { target.Release(); Object.DestroyImmediate(target); }
                ShaderUtil.allowAsyncCompilation = asyncCompilation;
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
#endif
