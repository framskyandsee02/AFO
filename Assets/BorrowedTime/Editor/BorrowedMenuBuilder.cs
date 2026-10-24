#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using BorrowedTime.Menu;

namespace BorrowedTime.Editor
{
    [InitializeOnLoad]
    public static class BorrowedMenuBuilder
    {
        public const string ScenePath = "Assets/Scenes/BorrowedTimeMainMenu.unity";
        public const string UiPath = "Assets/BorrowedTime/UI/";
        [Serializable] sealed class Request { public string command = ""; }
        static BorrowedMenuBuilder() { EditorApplication.update += Tick; }
        static void Tick()
        {
            const string requestPath = "Temp/BorrowedTime.request.json";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(requestPath)) return;
            var request = JsonUtility.FromJson<Request>(File.ReadAllText(requestPath)); File.Delete(requestPath);
            try
            {
                if (request.command == "setup") CreateOrOpen();
                else if (request.command == "validate") BorrowedMenuValidation.Begin();
                else throw new Exception("Unknown Borrowed Time request");
                File.WriteAllText("Temp/BorrowedTime.dispatch.json", "{\"success\":true}");
            }
            catch (Exception e) { File.WriteAllText("Temp/BorrowedTime.dispatch.json", JsonUtility.ToJson(new Error { message = e.ToString() }, true)); Debug.LogException(e); }
        }
        [Serializable] sealed class Error { public bool success = false; public string message; }
        [MenuItem("Tools/Borrowed Time/Create or Open Main Menu")]
        public static void CreateOrOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            var current = EditorSceneManager.GetActiveScene();
            if (current.isDirty && !EditorSceneManager.SaveScene(current, "Assets/BorrowedTime/Editor/BeforeMenuSetup.unity", true)) throw new Exception("Could not preserve the open scene.");
            foreach (string path in Directory.GetFiles(UiPath + "Art", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
                importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false; importer.SaveAndReimport();
            }
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = .5f;
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiPath + "BorrowedMenuTheme.tss");
            AssetDatabase.CreateAsset(panel, UiPath + "BorrowedMenuPanelSettings.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var camera = Camera.main; camera.orthographic = true; camera.orthographicSize = 5.4f;
            camera.transform.SetPositionAndRotation(new Vector3(0, 3, -10), Quaternion.Euler(10, 0, 0));
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.22f, .38f, .28f);
            var background = GameObject.CreatePrimitive(PrimitiveType.Quad); background.name = "Background - painted meadow";
            UnityEngine.Object.DestroyImmediate(background.GetComponent<Collider>()); background.transform.SetParent(camera.transform, false); background.transform.localPosition = new Vector3(0, 0, 30);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(UiPath + "Art/MeadowBackground.png");
            material.SetTexture("_BaseMap", texture); AssetDatabase.CreateAsset(material, UiPath + "MeadowBackdrop.mat");
            background.GetComponent<Renderer>().sharedMaterial = material;
            var anchor = new GameObject("CharacterPlacement - empty for future sleeping model").transform;
            var backdrop = camera.gameObject.AddComponent<MeadowBackdrop>(); backdrop.menuCamera = camera; backdrop.background = background.transform;
            backdrop.characterPlacement = anchor; backdrop.imageSize = new Vector2(texture.width, texture.height);
            var ui = new GameObject("Borrowed Time Main Menu UI");
            ui.AddComponent<BorrowedMainMenu>().playSceneName = "TrainingArea";
            var document = ui.GetComponent<UIDocument>();
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPath + "BorrowedMenuPanelSettings.asset");
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + "BorrowedMainMenu.uxml");
            EditorUtility.SetDirty(document);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            PlayerSettings.productName = "BORROWED TIME"; AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/BorrowedTime.setup.json", "{\"success\":true,\"menuScene\":\"" + ScenePath + "\",\"playScene\":\"TrainingArea\"}");
            Debug.Log("[Borrowed Time] Main Menu created. Play opens TrainingArea.");
        }
    }
}
#endif
