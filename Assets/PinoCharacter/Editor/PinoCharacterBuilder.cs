#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using OopsPlanet;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace WobbleWorks.Characters.Editor
{
    /// <summary>Builds only Pino-owned assets and a copy of the team's gameplay scene.</summary>
    public static class PinoCharacterBuilder
    {
        public const string Root = "Assets/PinoCharacter";
        public const string ModelPath = Root + "/Models/Pino_Morphs.fbx";
        public const string PrefabPath = Root + "/Prefabs/PinoCharacter.prefab";
        public const string GalleryPath = Root + "/Scenes/PinoMorphDemo.unity";
        public const string IntegrationPath = Root + "/Scenes/PinoGameplayIntegration.unity";
        public const string SourceGameplayPath = "Assets/Scenes/OopsPlanet/Scenes/OopsPlanet_V2.unity";
        public const string GalleryImagePath = "Art/Pino/Morphing/Unity_PinoMorphGallery.png";
        const int GalleryLayer = 30;
        const float GameplayVisualHeight = 1.2f;
        static readonly BlobProperty[] Forms = { BlobProperty.None, BlobProperty.Magnet, BlobProperty.Bounce, BlobProperty.Pressure, BlobProperty.Water };

        [MenuItem("Tools/Pino Character/Build Character Assets")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Build Pino assets outside Play Mode.");
            EnsureFolders();
            ConfigureImporter();
            Material[] materials = CreateMaterials();
            CharacterMaterialProfile[] profiles = CreateProfiles(materials);
            BuildPrefab(materials, profiles);
            BuildGallery();
            BuildIntegrationScene();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Root + "/Materials" }))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterMaterialProfile", new[] { Root + "/Profiles" }))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
            Debug.Log("PINO_BUILD_OK prefab=" + PrefabPath + " gallery=" + GalleryPath + " integration=" + IntegrationPath);
        }

        static void EnsureFolders()
        {
            foreach (string suffix in new[] { "Models", "Textures", "Materials", "Profiles", "Prefabs", "Scenes", "Evidence" })
            {
                string path = Root + "/" + suffix;
                if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "PinoCharacter");
                if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(Root, suffix);
            }
        }

        static void ConfigureImporter()
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (!importer) throw new FileNotFoundException("Export the existing Pino model to " + ModelPath + " first.");
            importer.globalScale = 1;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importBlendShapes = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importBlendShapeNormals = ModelImporterNormals.Calculate;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = true;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!model) throw new InvalidOperationException("Pino FBX did not import as a model.");
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name.Contains("Pino.Body"));
            if (!body || !body.sharedMesh || body.sharedMesh.blendShapeCount < 10)
                throw new InvalidOperationException("Pino body must import with ten morph shapes as a SkinnedMeshRenderer.");
        }

        static Material AssetMaterial(string name, Color tint, float smoothness, float metallic = 0)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("The existing project's URP/Lit shader is unavailable.");
            if (!material) { material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path); }
            else material.shader = shader;
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Surface", 0);
            material.SetFloat("_AlphaClip", 0);
            material.SetFloat("_ZWrite", 1);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color value); return value; }

        static Material[] CreateMaterials()
        {
            string texturePath = Root + "/Textures/Pino_Body_BaseColor.png";
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (!textureImporter) throw new FileNotFoundException("Pino body colour texture is missing: " + texturePath);
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.sRGBTexture = true;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var body = AssetMaterial("Pino_Jelly", Color.white, .88f);
            body.SetTexture("_BaseMap", texture);
            var eyes = AssetMaterial("Pino_CocoaEyes", Hex("633323"), .23f);
            var red = AssetMaterial("Pino_MagnetRed", Hex("E84360"), .72f, .55f);
            var silver = AssetMaterial("Pino_MagnetSilver", Hex("D9EDF1"), .82f, .8f);
            var spring = AssetMaterial("Pino_SpringMetal", Hex("57C8ED"), .82f, .72f);
            var gas = AssetMaterial("Pino_GasJelly", new Color(1, 1, 1, .64f), .88f);
            gas.SetTexture("_BaseMap", texture);
            gas.SetFloat("_Surface", 1);
            gas.SetFloat("_Blend", 0);
            gas.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            gas.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            gas.SetFloat("_ZWrite", 0);
            gas.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            gas.SetOverrideTag("RenderType", "Transparent");
            gas.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(gas);
            return new[] { body, eyes, red, silver, spring, gas };
        }

        static CharacterMaterialProfile[] CreateProfiles(Material[] materials)
        {
            var profiles = new List<CharacterMaterialProfile>();
            foreach (BlobProperty form in Forms)
            {
                string path = Root + "/Profiles/" + (form == BlobProperty.None ? "Normal" : PinoMorphController.ShapeNameFor(form)) + ".asset";
                var profile = AssetDatabase.LoadAssetAtPath<CharacterMaterialProfile>(path);
                if (!profile) { profile = ScriptableObject.CreateInstance<CharacterMaterialProfile>(); AssetDatabase.CreateAsset(profile, path); }
                profile.name = "Pino " + (form == BlobProperty.None ? "Normal" : PinoMorphController.ShapeNameFor(form));
                profile.form = form;
                profile.material = form == BlobProperty.Pressure ? materials[5] : materials[0];
                profile.tint = form == BlobProperty.Magnet ? new Color(.93f, .97f, 1) :
                    form == BlobProperty.Bounce ? new Color(1, .98f, .94f) :
                    form == BlobProperty.Pressure ? new Color(.82f, .95f, 1) :
                    form == BlobProperty.Water ? new Color(.83f, 1, .96f) : Color.white;
                profile.opacity = form == BlobProperty.Pressure ? .64f : 1;
                EditorUtility.SetDirty(profile);
                profiles.Add(profile);
            }
            return profiles.ToArray();
        }

        static void BuildPrefab(Material[] materials, CharacterMaterialProfile[] profiles)
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = EditorUtility.CreateGameObjectWithHideFlags("PinoVisual", HideFlags.HideAndDontSave);
                SceneManager.MoveGameObjectToScene(root, preview);
                root.hideFlags = HideFlags.None;
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), preview);
                model.name = "Pino Morph Model";
                model.transform.SetParent(root.transform, false);
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                var bodies = renderers.Where(r => r.name.Contains("Pino.Body")).ToArray();
                var eyes = renderers.Where(r => r.name.Contains("Pino.Eye")).ToArray();
                if (bodies.Length != 1 || eyes.Length != 2)
                    throw new InvalidOperationException("Pino must retain the original body and two eye meshes.");
                // FBX axis conversion can reverse the authored -Y face. Canonical
                // Unity forward is +Z; use actual eye placement, not an assumed exporter axis.
                float eyeZ = eyes.Average(r => BakedRootSpaceBounds(r, root.transform).center.z);
                float bodyZ = BakedRootSpaceBounds(bodies[0], root.transform).center.z;
                if (eyeZ < bodyZ)
                    model.transform.localRotation = Quaternion.Euler(0, 180, 0) * model.transform.localRotation;
                // Keep the FBX root's Z-up -> Y-up correction. Replacing its
                // rotation with yaw alone would lay the character on its side.
                Bounds normalBounds = BakedRootSpaceBounds(bodies[0], root.transform);
                float forwardEyes = eyes.Average(r => BakedRootSpaceBounds(r, root.transform).center.z);
                if (Mathf.Abs(normalBounds.size.y - .52f) > .01f || Mathf.Abs(normalBounds.min.y) > .006f)
                    throw new InvalidOperationException("Pino must stand upright at the original 0.52 m height and ground origin after FBX orientation calibration. Bounds=" + normalBounds);
                if (forwardEyes <= normalBounds.center.z + .05f)
                    throw new InvalidOperationException("Pino's two eyes must face Unity +Z after FBX orientation calibration.");
                var magnets = renderers.Where(r => r.name.Contains("Pino.Magnet")).ToArray();
                var springs = renderers.Where(r => r.name.Contains("Pino.SpringCoil")).ToArray();
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.name.Contains("Pino.Body")) renderer.sharedMaterial = materials[0];
                    else if (renderer.name.Contains("Pino.Eye")) renderer.sharedMaterial = materials[1];
                    else if (renderer.name.Contains("Pino.SpringCoil")) renderer.sharedMaterial = materials[4];
                    else if (renderer.name.Contains("Pino.Magnet"))
                    {
                        Material[] slots = renderer.sharedMaterials;
                        for (int i = 0; i < slots.Length; i++)
                        {
                            string imported = slots[i] ? slots[i].name.ToLowerInvariant() : "";
                            slots[i] = imported.Contains("silver") || imported.Contains("pole") || imported.Contains("cap") ? materials[3] : materials[2];
                        }
                        renderer.sharedMaterials = slots;
                    }
                    if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
                }
                var controller = root.AddComponent<PinoMorphController>();
                controller.Configure(model.GetComponentsInChildren<SkinnedMeshRenderer>(true), bodies, magnets, springs, profiles);
                controller.SetMorphForm(BlobProperty.None);
                // Animate an upright Unity-space pivot; the FBX child retains its axis correction.
                var motionPivot = new GameObject("Pino Motion").transform;
                motionPivot.SetParent(root.transform, false);
                model.transform.SetParent(motionPivot, false);
                root.AddComponent<PinoLocomotionAnimator>().Configure(motionPivot, controller, normalBounds.size.y);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        static Scene OpenOwnedScene(string path, out bool openedHere)
        {
            Scene existing = SceneManager.GetSceneByPath(path);
            if (existing.IsValid() && existing.isLoaded)
            {
                if (existing.isDirty) throw new InvalidOperationException("Save pending edits in " + path + " before rebuilding generated Pino content.");
                openedHere = false;
                return existing;
            }
            openedHere = true;
            return File.Exists(path) ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive) : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        }

        static void RestoreActiveScene(Scene previous, Scene owned, bool openedHere)
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (openedHere && owned.IsValid() && owned.isLoaded) EditorSceneManager.CloseScene(owned, true);
        }

        static void BuildGallery()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene gallery = OpenOwnedScene(GalleryPath, out bool openedHere);
            try
            {
                SceneManager.SetActiveScene(gallery);
                foreach (GameObject obj in gallery.GetRootGameObjects()) if (obj.name == "Pino Character Gallery") Object.DestroyImmediate(obj);
                var root = new GameObject("Pino Character Gallery");
                SceneManager.MoveGameObjectToScene(root, gallery);
                root.transform.position = new Vector3(5000, 0, 5000);
                AddGalleryCharacter(root.transform, new Vector3(1.8f, 0, -.80f), BlobProperty.None, "NORMAL", 0, 0, 0);
                AddGalleryCharacter(root.transform, new Vector3(.6f, 0, -.80f), BlobProperty.Magnet, "MAGNET", 0, 0, 0);
                AddGalleryCharacter(root.transform, new Vector3(-.6f, 0, -.80f), BlobProperty.Bounce, "SPRING", 0, 0, 0);
                AddGalleryCharacter(root.transform, new Vector3(-1.8f, 0, -.80f), BlobProperty.Pressure, "GAS", 0, 0, 0);
                AddGalleryCharacter(root.transform, new Vector3(1.8f, 0, .80f), BlobProperty.Water, "LIQUID", 0, 0, 0);
                AddGalleryCharacter(root.transform, new Vector3(.6f, 0, .80f), BlobProperty.Bounce, "SPRING / COMPRESS", 1, 0, 0);
                AddGalleryCharacter(root.transform, new Vector3(-.6f, 0, .80f), BlobProperty.Bounce, "SPRING / STRETCH", 0, 1, 0);
                AddGalleryCharacter(root.transform, new Vector3(-1.8f, 0, .80f), BlobProperty.Water, "LIQUID / FLOW", 0, 0, 1);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Gallery floor";
                floor.transform.SetParent(root.transform, false);
                floor.transform.localPosition = new Vector3(0, -.007f, 0);
                floor.transform.localScale = Vector3.one * 1.7f;
                Object.DestroyImmediate(floor.GetComponent<Collider>());
                floor.GetComponent<Renderer>().sharedMaterial = AssetMaterial("Pino_GalleryFloor", Hex("B4CFD0"), .45f);
                var cameraObject = new GameObject("Pino Gallery Camera");
                cameraObject.transform.SetParent(root.transform, false);
                cameraObject.transform.localPosition = new Vector3(0, 3.35f, 6.2f);
                cameraObject.transform.LookAt(root.transform.position + new Vector3(0, .27f, .12f));
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 1.8f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Hex("DAE8E5");
                camera.cullingMask = 1 << GalleryLayer;
                AddGalleryLight(root.transform, "Gallery key", Quaternion.Euler(48, -35, 0), 1.2f, Color.white);
                AddGalleryLight(root.transform, "Gallery fill", Quaternion.Euler(35, 140, 0), .35f, Hex("C6F2FF"));
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.34f, .38f, .42f);
                SetLayerRecursive(root, GalleryLayer);
                EditorSceneManager.SaveScene(gallery, GalleryPath);
            }
            finally { RestoreActiveScene(previous, gallery, openedHere); }
        }

        static void AddGalleryCharacter(Transform parent, Vector3 position, BlobProperty form, string label, float compress, float stretch, float flow)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), parent.gameObject.scene);
            obj.name = label; obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
            var controller = obj.GetComponent<PinoMorphController>();
            controller.IdleEnabled = false;
            controller.SetMorphForm(form);
            controller.SetSpringCompression(compress);
            controller.SetSpringStretch(stretch);
            controller.SetLiquidFlow(flow);
            controller.Tick(0);
            var textObject = new GameObject("Label");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position + new Vector3(0, .015f, .51f);
            textObject.transform.localRotation = Quaternion.Euler(70, 180, 0);
            var text = textObject.AddComponent<TextMesh>();
            text.text = label; text.fontSize = 48; text.characterSize = .012f;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.GetComponent<Renderer>().sharedMaterial = text.font.material;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.color = Hex("214E5C");
        }

        static void AddGalleryLight(Transform parent, string name, Quaternion rotation, float intensity, Color color)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false); obj.transform.localRotation = rotation;
            var light = obj.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = intensity;
            light.color = color; light.shadows = LightShadows.Soft; light.cullingMask = 1 << GalleryLayer;
        }

        static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, layer);
        }

        static void BuildIntegrationScene()
        {
            if (SceneManager.GetSceneByPath(IntegrationPath).isLoaded)
                throw new InvalidOperationException("Close the generated Pino integration scene before rebuilding.");
            File.Copy(SourceGameplayPath, IntegrationPath, true);
            AssetDatabase.ImportAsset(IntegrationPath, ImportAssetOptions.ForceSynchronousImport);
            Scene previous = SceneManager.GetActiveScene();
            Scene integration = OpenOwnedScene(IntegrationPath, out bool openedHere);
            try
            {
                SceneManager.SetActiveScene(integration);
                BlobPlayer player = integration.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<BlobPlayer>(true)).FirstOrDefault();
                if (!player || !player.visual) throw new InvalidOperationException("The copied scene has no configured BlobPlayer.visual.");
                foreach (var old in player.GetComponentsInChildren<PinoGameplayAdapter>(true))
                {
                    if (old.transform.parent != player.transform || old.gameObject.name != "PinoVisual")
                        throw new InvalidOperationException("An existing Pino adapter is attached outside the generated PinoVisual. Preserve it and review the integration hierarchy before rebuilding.");
                    Object.DestroyImmediate(old.gameObject);
                }
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), integration);
                obj.name = "PinoVisual"; obj.transform.SetParent(player.transform, false);
                var controller = obj.GetComponent<PinoMorphController>();
                var body = obj.GetComponentsInChildren<Renderer>(true).First(r => r.name.Contains("Pino.Body"));
                float height = MeasureCurrentHeight(body);
                if (height < .01f || height > 2) throw new InvalidOperationException("Unexpected Pino import height: " + height + " metres. Check FBX unit settings.");
                obj.transform.localScale = Vector3.one * (GameplayVisualHeight / height);
                SetLayerRecursive(obj, 2);
                var adapter = obj.AddComponent<PinoGameplayAdapter>();
                adapter.Configure(player, controller, player.visual);
                adapter.SyncNow(true, 0);
                foreach (Renderer renderer in player.visual.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                EditorSceneManager.SaveScene(integration, IntegrationPath);
            }
            finally { RestoreActiveScene(previous, integration, openedHere); }
        }

        static float MeasureCurrentHeight(Renderer renderer)
        {
            if (!(renderer is SkinnedMeshRenderer skinned)) return renderer.bounds.size.y;
            var mesh = new Mesh();
            try
            {
                skinned.BakeMesh(mesh);
                float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
                foreach (Vector3 vertex in mesh.vertices)
                {
                    float y = renderer.transform.TransformPoint(vertex).y;
                    minimum = Mathf.Min(minimum, y); maximum = Mathf.Max(maximum, y);
                }
                return maximum - minimum;
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        static Bounds BakedRootSpaceBounds(Renderer renderer, Transform root)
        {
            Mesh temporary = null;
            try
            {
                Mesh mesh;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    temporary = new Mesh();
                    skinned.BakeMesh(temporary);
                    mesh = temporary;
                }
                else
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    mesh = filter ? filter.sharedMesh : null;
                }
                if (!mesh || mesh.vertexCount == 0) throw new InvalidOperationException("Cannot measure the authored Pino geometry: " + renderer.name);
                Vector3[] vertices = mesh.vertices;
                Matrix4x4 toRoot = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                var bounds = new Bounds(toRoot.MultiplyPoint3x4(vertices[0]), Vector3.zero);
                for (int i = 1; i < vertices.Length; i++) bounds.Encapsulate(toRoot.MultiplyPoint3x4(vertices[i]));
                return bounds;
            }
            finally { if (temporary) Object.DestroyImmediate(temporary); }
        }

        [MenuItem("Tools/Pino Character/Capture Morph Gallery")]
        public static void CaptureGallery()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Capture outside Play Mode.");
            Scene previous = SceneManager.GetActiveScene();
            Scene gallery = OpenOwnedScene(GalleryPath, out bool openedHere);
            var changedLights = new List<Light>();
            RenderTexture previousTarget = null, previousActive = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D image = null;
            Camera camera = null;
            bool previousAsyncCompilation = ShaderUtil.allowAsyncCompilation;
            try
            {
                // A newly authored transparent variant must be ready for this
                // one-shot offscreen capture, not skipped while compiling asynchronously.
                ShaderUtil.allowAsyncCompilation = false;
                SceneManager.SetActiveScene(gallery);
                camera = gallery.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Camera>(true)).First();
                var diagnostics = new System.Text.StringBuilder();
                foreach (PinoMorphController controller in gallery.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<PinoMorphController>(true)))
                {
                    // Property blocks are transient and must be reapplied after loading a scene.
                    controller.Tick(0);
                    foreach (Renderer renderer in controller.GetComponentsInChildren<Renderer>(true))
                    {
                        var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                        diagnostics.AppendLine(controller.name + " / " + renderer.name + " enabled=" + renderer.enabled +
                            " layer=" + renderer.gameObject.layer + " bounds=" + renderer.bounds +
                            " material=" + (renderer.sharedMaterial ? renderer.sharedMaterial.name : "missing") +
                            " blockColor=" + block.GetColor("_BaseColor"));
                    }
                }
                foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (light.gameObject.scene != gallery && light.enabled) { changedLights.Add(light); light.enabled = false; }
                renderTexture = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
                previousTarget = camera.targetTexture; camera.targetTexture = renderTexture;
                camera.Render(); RenderTexture.active = renderTexture;
                image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(GalleryImagePath));
                File.WriteAllBytes(GalleryImagePath, image.EncodeToPNG());
                File.WriteAllText("Art/Pino/Morphing/Unity_GalleryDiagnostics.txt", diagnostics.ToString());
                Debug.Log("PINO_GALLERY_CAPTURE_OK " + Path.GetFullPath(GalleryImagePath));
            }
            finally
            {
                if (camera) camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                foreach (Light light in changedLights) if (light) light.enabled = true;
                if (image) Object.DestroyImmediate(image);
                if (renderTexture) { renderTexture.Release(); Object.DestroyImmediate(renderTexture); }
                ShaderUtil.allowAsyncCompilation = previousAsyncCompilation;
                RestoreActiveScene(previous, gallery, openedHere);
            }
        }
    }
}
#endif
