#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using OopsPlanet;
using UnityEngine.SceneManagement;

namespace WobbleWorks.Characters.Editor
{
    /// <summary>Repeatable geometry, transition and read-only adapter checks using the imported character.</summary>
    public static class PinoCharacterValidation
    {
        const string PrefabPath = "Assets/PinoCharacter/Prefabs/PinoCharacter.prefab";
        const string ReportPath = "Assets/PinoCharacter/Evidence/Validation.json";
        static readonly string[] Shapes = { "Magnet", "Spring", "SpringCompress", "SpringStretch", "Gas", "Liquid", "LiquidFlow", "Absorb", "Idle", "Land" };

        [Serializable] public sealed class Check
        {
            public string name;
            public bool passed;
            public string details;
        }

        [Serializable] public sealed class Report
        {
            public string generatedUtc;
            public string unityVersion;
            public string prefab;
            public int passed;
            public int failed;
            public List<Check> checks = new List<Check>();
        }

        [MenuItem("Tools/Pino Character/Validate Character")]
        public static void RunAll()
        {
            var report = new Report { generatedUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion, prefab = PrefabPath };
            Scene originalActive = SceneManager.GetActiveScene();
            Scene testScene = default;
            bool previousBackground = Application.runInBackground;
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Require(prefab, "Character prefab exists", PrefabPath, report);
                testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(testScene);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, testScene);
                var controller = instance.GetComponent<PinoMorphController>();
                Require(controller, "Prefab has morph controller", instance.name, report);
                controller.IdleEnabled = false;
                controller.SetVisualPaused(false);
                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var body = renderers.FirstOrDefault(r => r.name.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0);
                Require(body && body.sharedMesh, "Imported body is skinned morph mesh", "Blend shapes must not become static meshes", report);
                var eyes = renderers.Where(r => r.name.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                Require(eyes.Length == 2, "Two original eye meshes retained", "count=" + eyes.Length, report);

                foreach (var renderer in new[] { body }.Concat(eyes))
                {
                    Require(renderer.sharedMesh.isReadable, "Readable morph mesh: " + renderer.name, "Importer Read/Write enabled", report);
                    foreach (string shape in Shapes)
                        Require(FindShape(renderer.sharedMesh, shape) >= 0, "Morph imported: " + renderer.name + "/" + shape, "FBX blend shape", report);
                }
                controller.SetMorphForm(BlobProperty.None);
                controller.Tick(0f);
                Vector3[] basis = WorldVertices(body);
                Vector3 rootScale = instance.transform.localScale;
                float normalHeight = Height(basis);
                float bottom = basis.Min(v => v.y);
                float bodyCenterZ = (basis.Min(v => v.z) + basis.Max(v => v.z)) * .5f;
                float eyeCenterZ = eyes.SelectMany(WorldVertices).Average(v => v.z);
                Require(Mathf.Abs(normalHeight - .52f) < .01f, "Normal import has correct metre scale and up axis", "height=" + normalHeight, report);
                Require(Mathf.Abs(bottom) < .005f, "Normal body rests at the character origin", "minimumY=" + bottom, report);
                Require(eyeCenterZ > bodyCenterZ + .02f, "Character face points toward Unity forward +Z", "eyesZ=" + eyeCenterZ + ", bodyCenterZ=" + bodyCenterZ, report);

                // Prove Spring first, then the remaining forms, using baked geometry.
                controller.SetMorphForm(BlobProperty.Bounce);
                controller.Tick(0f);
                Vector3[] spring = WorldVertices(body);
                Require(MaxDistance(basis, spring) > .001f, "Spring changes real geometry", "maxVertexDelta=" + MaxDistance(basis, spring), report);
                float springHeight = Height(spring);
                CheckAttachments(body, eyes, "Spring", report);
                controller.SetSpringCompression(1f);
                controller.Tick(.1f);
                Vector3[] compressed = WorldVertices(body);
                Require(Height(compressed) < springHeight * .96f, "Spring compression changes height", "base=" + springHeight + ", compressed=" + Height(compressed), report);
                CheckAttachments(body, eyes, "Spring compressed", report);
                controller.SetSpringCompression(0f);
                controller.SetSpringStretch(1f);
                controller.Tick(.1f);
                Vector3[] stretched = WorldVertices(body);
                Require(Height(stretched) > springHeight * 1.04f, "Spring extension changes height", "base=" + springHeight + ", stretched=" + Height(stretched), report);
                CheckAttachments(body, eyes, "Spring stretched", report);

                foreach (BlobProperty form in new[] { BlobProperty.Magnet, BlobProperty.Pressure, BlobProperty.Water })
                {
                    controller.SetMorphForm(form);
                    controller.Tick(0f);
                    Vector3[] vertices = WorldVertices(body);
                    Require(MaxDistance(basis, vertices) > .001f, form + " changes real geometry", "maxVertexDelta=" + MaxDistance(basis, vertices), report);
                    CheckAttachments(body, eyes, form.ToString(), report);
                }
                controller.SetMorphForm(BlobProperty.Water);
                controller.Tick(0f);
                float liquidHeight = Height(WorldVertices(body));
                controller.SetLiquidFlow(1f);
                controller.Tick(.1f);
                Require(Height(WorldVertices(body)) < liquidHeight * .96f, "Liquid flow flattens real mesh", "base=" + liquidHeight + ", flow=" + Height(WorldVertices(body)), report);
                CheckAttachments(body, eyes, "Liquid flattened", report);

                // Retarget midway without snapping, then prove exact reversible geometry.
                controller.SetMorphForm(BlobProperty.None);
                controller.BlendToMorphForm(BlobProperty.Bounce, 1f);
                controller.Tick(.35f);
                Vector3[] beforeInterrupt = WorldVertices(body);
                controller.BlendToMorphForm(BlobProperty.Pressure, .4f);
                Vector3[] afterInterrupt = WorldVertices(body);
                Require(MaxDistance(beforeInterrupt, afterInterrupt) < .00001f, "Interrupted transition is continuous", "snapDelta=" + MaxDistance(beforeInterrupt, afterInterrupt), report);
                controller.Tick(.5f);
                for (int i = 0; i < 12; i++)
                {
                    controller.BlendToMorphForm(new[] { BlobProperty.Magnet, BlobProperty.Bounce, BlobProperty.Pressure, BlobProperty.Water }[i % 4], .12f);
                    controller.Tick(.05f);
                    controller.ReturnToNormal(.08f);
                    controller.Tick(.1f);
                }
                Require(MaxDistance(basis, WorldVertices(body)) < .00001f, "Repeated morphs restore exact Normal mesh", "maxResidual=" + MaxDistance(basis, WorldVertices(body)), report);
                Require(Vector3.Distance(rootScale, instance.transform.localScale) < .00001f, "Morphing does not resize character root", "Geometry and facial morphs own deformation", report);

                controller.SetMorphWeight("Gas", .5f);
                Require(Mathf.Abs(controller.GetMorphWeight("Gas") - .5f) < .00001f && MaxDistance(basis, WorldVertices(body)) > .001f,
                    "Named weight API changes geometry in normalized units", "Gas override=.5 (not 50)", report);
                controller.ReturnToNormal(0f);
                Require(MaxDistance(basis, WorldVertices(body)) < .00001f, "ReturnToNormal clears manual shape overrides", "No lingering Gas override", report);
                var gasProfile = AssetDatabase.LoadAssetAtPath<CharacterMaterialProfile>("Assets/PinoCharacter/Profiles/Gas.asset");
                Require(gasProfile && gasProfile.material && gasProfile.opacity < 1f, "Gas appearance profile supports transparency", "Material and opacity are authored assets", report);
                controller.SetCharacterMaterial(gasProfile);
                Require(body.sharedMaterial == gasProfile.material, "Character material API applies authored profile", body.sharedMaterial ? body.sharedMaterial.name : "missing", report);
                controller.SetMorphForm(BlobProperty.None);

                controller.SetMorphForm(BlobProperty.None);
                controller.BlendToMorphForm(BlobProperty.Bounce, 1f);
                controller.Tick(.2f);
                float pausedWeight = controller.GetMorphWeight("Spring");
                controller.SetVisualPaused(true);
                controller.Tick(.5f);
                Require(Mathf.Abs(pausedWeight - controller.GetMorphWeight("Spring")) < .00001f, "Paused presentation freezes transition", "weight=" + pausedWeight, report);
                controller.SetVisualPaused(false);
                controller.Tick(1f);
                Require(controller.GetMorphWeight("Spring") > .999f, "Transition resumes after pause", "Spring=" + controller.GetMorphWeight("Spring"), report);

                controller.SetMorphForm(BlobProperty.None);
                controller.SetAbsorptionProgress(.6f);
                controller.Tick(.05f);
                Require(controller.GetMorphWeight("Absorb") > .001f, "Absorption reaction deforms geometry", "Absorb=" + controller.GetMorphWeight("Absorb"), report);
                controller.SetAbsorptionProgress(0f);
                controller.SetMorphForm(BlobProperty.None);
                controller.PlayLandingReaction(1f);
                controller.Tick(.025f);
                Require(controller.GetMorphWeight("Land") > .001f, "Landing reaction deforms geometry", "Land=" + controller.GetMorphWeight("Land"), report);
                controller.SetMorphForm(BlobProperty.None);
                controller.IdleEnabled = true;
                controller.Tick(.17f);
                Require(controller.GetMorphWeight("Idle") > .0001f, "Idle is a geometry animation", "Idle=" + controller.GetMorphWeight("Idle"), report);
                controller.IdleEnabled = false;

                CheckAccessories(controller, instance, report);
                CheckSavedPreviewPersistence(controller, instance, report);
                CheckAdapter(controller, instance, report);
            }
            catch (Exception exception)
            {
                if (report.failed == 0) Record(false, "Unexpected validation exception", exception.ToString(), report);
            }
            finally
            {
                if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
                if (testScene.IsValid() && testScene.isLoaded) EditorSceneManager.CloseScene(testScene, true);
                Application.runInBackground = previousBackground;
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
                AssetDatabase.ImportAsset(ReportPath);
            }
            string summary = "PINO_VALIDATION passed=" + report.passed + " failed=" + report.failed + " report=" + ReportPath;
            if (report.failed > 0) throw new InvalidOperationException(summary);
            Debug.Log(summary);
        }

        static void CheckAccessories(PinoMorphController controller, GameObject instance, Report report)
        {
            Renderer[] all = instance.GetComponentsInChildren<Renderer>(true);
            Renderer[] magnets = all.Where(r => r.name.IndexOf("Magnet", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            Renderer[] springs = all.Where(r => r.name.IndexOf("SpringCoil", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            Require(magnets.Length > 0 && springs.Length > 0, "Ability-specific parts imported", "magnet=" + magnets.Length + ", spring=" + springs.Length, report);
            controller.SetMorphForm(BlobProperty.None); controller.Tick(0f);
            Require(magnets.All(r => !r.enabled) && springs.All(r => !r.enabled), "Normal hides special parts", "No unwanted attachments", report);
            controller.SetMorphForm(BlobProperty.Magnet); controller.Tick(0f);
            Require(magnets.Any(r => r.enabled) && springs.All(r => !r.enabled), "Magnet shows only magnetic parts", "Renderer visibility", report);
            controller.SetMorphForm(BlobProperty.Bounce); controller.Tick(0f);
            Require(springs.Any(r => r.enabled) && magnets.All(r => !r.enabled), "Spring shows only coil parts", "Renderer visibility", report);
            controller.SetMorphForm(BlobProperty.None); controller.Tick(0f);
        }

        static void CheckSavedPreviewPersistence(PinoMorphController controller, GameObject instance, Report report)
        {
            // Regression: renderer weights serialize, but controller parameters and
            // MaterialPropertyBlocks can otherwise disappear when a saved scene opens.
            string path = "Assets/PinoCharacter/Evidence/PinoPreviewValidation-" + Guid.NewGuid().ToString("N") + ".prefab";
            GameObject loaded = null;
            controller.Tick(2f); // Finish transient reactions; only the authored preview should persist.
            try
            {
                controller.SetMorphForm(BlobProperty.Bounce);
                controller.SetSpringCompression(1f);
                controller.Tick(0f);
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                loaded = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), instance.scene);
                var reloaded = loaded.GetComponent<PinoMorphController>();
                reloaded.Tick(0f);
                Require(reloaded.TargetForm == BlobProperty.Bounce && reloaded.GetMorphWeight("SpringCompress") > .999f,
                    "Saved Spring compression survives reload and Tick(0)", "SpringCompress=" + reloaded.GetMorphWeight("SpringCompress"), report);
                UnityEngine.Object.DestroyImmediate(loaded); loaded = null;

                controller.SetMorphForm(BlobProperty.Water);
                controller.SetLiquidFlow(1f);
                controller.Tick(0f);
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                loaded = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), instance.scene);
                reloaded = loaded.GetComponent<PinoMorphController>();
                reloaded.Tick(0f);
                Require(reloaded.TargetForm == BlobProperty.Water && reloaded.GetMorphWeight("LiquidFlow") > .999f,
                    "Saved Liquid flow survives reload and Tick(0)", "LiquidFlow=" + reloaded.GetMorphWeight("LiquidFlow"), report);
                UnityEngine.Object.DestroyImmediate(loaded); loaded = null;

                controller.SetMorphForm(BlobProperty.Pressure);
                controller.Tick(0f);
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                loaded = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), instance.scene);
                var gasBody = loaded.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0);
                var block = new MaterialPropertyBlock();
                gasBody.GetPropertyBlock(block);
                float opacity = block.GetColor("_BaseColor").a;
                Require(gasBody.enabled && gasBody.sharedMaterial && gasBody.sharedMaterial.GetFloat("_Surface") > .5f && opacity > .1f && opacity < .99f,
                    "Saved Gas body remains visible with restored transparency", "enabled=" + gasBody.enabled + ", alpha=" + opacity, report);
            }
            finally
            {
                if (loaded) UnityEngine.Object.DestroyImmediate(loaded);
                AssetDatabase.DeleteAsset(path);
                controller.SetMorphForm(BlobProperty.None);
                controller.Tick(0f);
            }
        }

        static void CheckAdapter(PinoMorphController controller, GameObject instance, Report report)
        {
            var host = new GameObject("Pino validation — inactive gameplay source");
            host.SetActive(false); // BlobPlayer.Awake must not run; its gameplay world is deliberately absent.
            var driver = new GameObject("Legacy visual direction driver");
            driver.transform.SetParent(host.transform, false);
            driver.transform.localRotation = Quaternion.Euler(0f, 43f, 0f);
            driver.transform.localScale = Vector3.one * .2f;
            var visibleLegacy = driver.AddComponent<MeshRenderer>();
            visibleLegacy.enabled = true;
            var hiddenLegacy = new GameObject("Already hidden legacy renderer");
            hiddenLegacy.transform.SetParent(driver.transform, false);
            var hiddenRenderer = hiddenLegacy.AddComponent<MeshRenderer>();
            hiddenRenderer.enabled = false;
            var source = host.AddComponent<BlobPlayer>();
            source.visual = driver.transform;
            source.property = BlobProperty.Bounce;
            source.bounceCharge = .73f;
            source.absorbProgress = .42f;
            source.velocity = new Vector3(0f, 6f, 0f);
            instance.transform.SetParent(host.transform, false);
            var adapter = instance.GetComponent<PinoGameplayAdapter>();
            if (!adapter) adapter = instance.AddComponent<PinoGameplayAdapter>();
            adapter.Configure(source, controller, driver.transform);
            Vector3 priorScale = instance.transform.localScale;
            Vector3 priorPosition = source.transform.position;
            adapter.SyncNow(true, .016f); controller.Tick(.5f);
            Require(controller.TargetForm == BlobProperty.Bounce && controller.GetMorphWeight("SpringCompress") > .6f,
                "Adapter reads existing Spring state", "form=" + controller.TargetForm + ", compression=" + controller.GetMorphWeight("SpringCompress"), report);
            Require(source.property == BlobProperty.Bounce && Mathf.Approximately(source.bounceCharge, .73f) &&
                Mathf.Approximately(source.absorbProgress, .42f) && source.velocity == new Vector3(0f, 6f, 0f) && source.transform.position == priorPosition,
                "Adapter leaves gameplay state unchanged", "No ability, movement, velocity or input writes", report);
            Require(Quaternion.Angle(instance.transform.rotation, driver.transform.rotation) < .01f && instance.transform.localScale == priorScale,
                "Adapter copies orientation without legacy scale", "Separate sibling visual", report);
            Require(!visibleLegacy.enabled && !hiddenRenderer.enabled, "Legacy meshes hidden while Pino is active", "Captured renderer states", report);

            source.property = BlobProperty.Water;  source.bounceCharge = 0f; source.absorbProgress = 0f;
            adapter.SyncNow(false, .016f); controller.Tick(1f);
            Require(controller.TargetForm == BlobProperty.Water && controller.GetMorphWeight("LiquidFlow") < .001f,
                "Adapter does not invent a flatten ability", "Water has no clearance/flatten mechanic in OOPS", report);
            source.property = BlobProperty.None;
            adapter.SyncNow(false, .016f); controller.Tick(1f);
            Require(controller.TargetForm == BlobProperty.None && controller.GetMorphWeight("Liquid") < .001f,
                "Adapter returns to Normal on None", "Respawn-compatible state mapping", report);
            adapter.Configure(null, controller);
            Require(visibleLegacy.enabled && !hiddenRenderer.enabled, "Legacy renderer states restored exactly", "Previously disabled renderers stay disabled", report);
        }

        static void CheckAttachments(SkinnedMeshRenderer body, SkinnedMeshRenderer[] eyes, string pose, Report report)
        {
            Vector3[] bodyVertices = WorldVertices(body);
            int[] triangles = body.sharedMesh.triangles;
            foreach (var eye in eyes)
            {
                Vector3[] eyeVertices = WorldVertices(eye);
                Vector3 center = Vector3.zero;
                foreach (Vector3 vertex in eyeVertices) center += vertex;
                center /= eyeVertices.Length;
                float distance = float.PositiveInfinity;
                for (int i = 0; i < triangles.Length; i += 3)
                    distance = Mathf.Min(distance, Vector3.Distance(center, ClosestPointOnTriangle(center, bodyVertices[triangles[i]], bodyVertices[triangles[i + 1]], bodyVertices[triangles[i + 2]])));
                Require(distance < .03f, "Eye follows surface: " + pose + "/" + eye.name, "surfaceGapMetres=" + distance, report);
            }
        }

        static Vector3[] Bake(SkinnedMeshRenderer renderer)
        {
            var mesh = new Mesh();
            renderer.BakeMesh(mesh, false);
            Vector3[] vertices = mesh.vertices;
            UnityEngine.Object.DestroyImmediate(mesh);
            return vertices;
        }

        static Vector3[] WorldVertices(SkinnedMeshRenderer renderer)
        {
            Vector3[] vertices = Bake(renderer);
            for (int i = 0; i < vertices.Length; i++) vertices[i] = renderer.transform.TransformPoint(vertices[i]);
            return vertices;
        }

        static int FindShape(Mesh mesh, string name)
        {
            int exact = mesh.GetBlendShapeIndex(name);
            if (exact >= 0) return exact;
            for (int i = 0; i < mesh.blendShapeCount; i++)
                if (mesh.GetBlendShapeName(i).EndsWith("." + name, StringComparison.Ordinal)) return i;
            return -1;
        }

        static float MaxDistance(Vector3[] a, Vector3[] b)
        {
            if (a.Length != b.Length) return float.PositiveInfinity;
            float max = 0f;
            for (int i = 0; i < a.Length; i++) max = Mathf.Max(max, Vector3.Distance(a[i], b[i]));
            return max;
        }

        static float Height(Vector3[] vertices)
        {
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            foreach (Vector3 vertex in vertices) { min = Mathf.Min(min, vertex.y); max = Mathf.Max(max, vertex.y); }
            return max - min;
        }

        static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a, ac = c - a, ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return a;
            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return b;
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f) return a + ab * (d1 / (d1 - d3));
            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return c;
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f) return a + ac * (d2 / (d2 - d6));
            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f) return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
            float denominator = va + vb + vc;
            if (Mathf.Abs(denominator) < 1e-16f) return a;
            return a + ab * (vb / denominator) + ac * (vc / denominator);
        }

        static void Require(bool passed, string name, string details, Report report)
        {
            Record(passed, name, details, report);
            if (!passed) throw new InvalidOperationException(name + ": " + details);
        }

        static void Record(bool passed, string name, string details, Report report)
        {
            report.checks.Add(new Check { name = name, passed = passed, details = details });
            if (passed) report.passed++; else report.failed++;
        }
    }
}
#endif

