#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using BorrowedTime.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BorrowedTime.Editor
{
    [InitializeOnLoad]
    public static class BorrowedMeadowImporter
    {
        const string Model = "Assets/BorrowedTime/Environment/Models/BT_Meadow.fbx";
        const string Prefab = "Assets/BorrowedTime/Environment/Prefabs/BT_Meadow.prefab";
        const string Report = "Art/BorrowedTime/Meadow3D/UnityImport.json";
        [Serializable] sealed class Result
        {
            public bool success, hasVertexColors, hasNoCharacters;
            public int meshObjects, vertices, triangles;
            public Vector3 size;
            public string scene, prefab, message;
        }
        static BorrowedMeadowImporter() { EditorApplication.update += Tick; }
        static void Tick()
        {
            const string request = "Temp/BorrowedMeadow.request.json";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Import(); }
            catch (Exception e) { File.WriteAllText(Report, JsonUtility.ToJson(new Result { message = e.ToString() }, true)); Debug.LogException(e); }
        }
        [MenuItem("Tools/Borrowed Time/Import Blender 3D Meadow")]
        public static void Import()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode before importing.");
            var importer = AssetImporter.GetAtPath(Model) as ModelImporter;
            if (!importer) throw new Exception("Blender FBX has not been imported yet.");
            importer.globalScale = 1; importer.bakeAxisConversion = true;
            importer.importCameras = false; importer.importLights = false; importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importNormals = ModelImporterNormals.Calculate; importer.normalSmoothingAngle = 65;
            importer.isReadable = true; importer.SaveAndReimport();
            var shader = Shader.Find("Borrowed Time/Painterly Meadow");
            if (!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Meadow shader is unavailable or has errors.");
            const string matPath = "Assets/BorrowedTime/Environment/Materials/BT_PainterlyMeadow.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, matPath); }
            material.enableInstancing = true;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var source = (GameObject)PrefabUtility.InstantiatePrefab(model);
            source.name = "BT Meadow 3D Background";
            foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>()) renderer.sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(source, Prefab); UnityEngine.Object.DestroyImmediate(source);

            if (EditorSceneManager.GetActiveScene().path != BorrowedMenuBuilder.ScenePath) EditorSceneManager.OpenScene(BorrowedMenuBuilder.ScenePath);
            var old = GameObject.Find("BT Meadow 3D Background"); if (old) UnityEngine.Object.DestroyImmediate(old);
            var environment = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            environment.name = "BT Meadow 3D Background";
            var camera = Camera.main;
            var painted = camera.GetComponent<MeadowBackdrop>();
            if (painted) { painted.enabled = false; if (painted.background) painted.background.gameObject.SetActive(false); }
            var view = camera.GetComponent<MeadowMenuView>(); if (!view) view = camera.gameObject.AddComponent<MeadowMenuView>();
            view.characterPlacement = painted ? painted.characterPlacement : null; view.RefreshFraming();
            camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            var menu = UnityEngine.Object.FindFirstObjectByType<BorrowedMainMenu>(); menu.showForegroundFoliage = false;
            var sunlight = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            if (sunlight)
            {
                sunlight.transform.rotation = Quaternion.Euler(44, -28, 0);
                sunlight.color = new Color(1, .96f, .79f); sunlight.intensity = 1.25f;
                sunlight.shadows = LightShadows.Soft; sunlight.shadowStrength = .5f; sunlight.shadowBias = .02f; sunlight.shadowNormalBias = .08f;
            }
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.68f, .77f, .58f); RenderSettings.ambientIntensity = .75f;
            var filters = environment.GetComponentsInChildren<MeshFilter>();
            Bounds bounds = environment.GetComponentsInChildren<Renderer>()[0].bounds;
            foreach (var renderer in environment.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            bool colors = filters.All(f => f.sharedMesh.colors32.Length == f.sharedMesh.vertexCount);
            var result = new Result { meshObjects = filters.Length, vertices = filters.Sum(f => f.sharedMesh.vertexCount), triangles = filters.Sum(f => f.sharedMesh.triangles.Length/3),
                hasVertexColors = colors, hasNoCharacters = environment.GetComponentsInChildren<Animator>().Length == 0,
                size = bounds.size, scene = BorrowedMenuBuilder.ScenePath, prefab = Prefab,
                message = "Blender mesh meadow replaces the disabled painting. The menu's buttons and character anchor are preserved." };
            result.success = colors && bounds.size.x > 30 && bounds.size.z > 20 && bounds.size.y < 1 && filters.Length == 37;
            EditorUtility.SetDirty(menu); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            File.WriteAllText(Report, JsonUtility.ToJson(result, true));
            Selection.activeGameObject = environment;
            Debug.Log("[Borrowed Time] 3D meadow imported: " + result.meshObjects + " meshes, " + result.triangles + " triangles, bounds " + bounds.size);
        }
    }
}
#endif
