#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using BorrowedTime.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BorrowedTime.Editor
{
    [InitializeOnLoad]
    public static class SleepingCompanionsSetup
    {
        public const string Root = "Assets/_BorrowedTime/Characters";
        const string RequestPath = "Temp/SleepingCompanions.request.json";
        [Serializable] sealed class Request { public string command; }
        static SleepingCompanionsSetup() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;
            var request = JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath));
            File.Delete(RequestPath);
            try
            {
                if (request.command == "inspect") Inspect();
                else if (request.command == "install") Install();
                else if (request.command == "validate") SleepingCompanionsValidation.Begin();
                else throw new Exception("Unknown sleeping companions request: " + request.command);
                File.WriteAllText("Temp/SleepingCompanions.dispatch.txt", "OK " + request.command);
            }
            catch (Exception e)
            {
                File.WriteAllText("Temp/SleepingCompanions.dispatch.txt", e.ToString());
                Debug.LogException(e);
            }
        }

        static GameObject Load(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + name + ".fbx");
            if (!asset) throw new Exception("Character model not imported: " + name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            return instance;
        }

        public static void Inspect()
        {
            var report = new StringBuilder();
            foreach (string name in new[] { "Protagonist", "Robo" })
            {
                var go = Load(name);
                try
                {
                    report.AppendLine(name);
                    foreach (var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        report.AppendLine("Mesh " + renderer.name + " bounds " + renderer.bounds + " materials " + string.Join(",", renderer.sharedMaterials.Select(m => m ? m.name : "MISSING")));
                        report.AppendLine("Expressions " + string.Join(",", Enumerable.Range(0, renderer.sharedMesh.blendShapeCount).Select(renderer.sharedMesh.GetBlendShapeName)));
                    }
                    foreach (var bone in go.GetComponentsInChildren<Transform>())
                        report.AppendLine(bone.name + " pos=" + go.transform.InverseTransformPoint(bone.position).ToString("F3") + " rotation=" + bone.localEulerAngles.ToString("F1"));
                }
                finally { Object.DestroyImmediate(go); }
            }
            File.WriteAllText("Temp/SleepingCompanions.models.txt", report.ToString());
        }

        static Transform Bone(Transform root, string name) => root.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == name);

        static void Aim(Transform model, string boneName, string tipName, Vector3 direction)
        {
            var bone = Bone(model, boneName); var tip = Bone(model, tipName);
            if (!bone || !tip) throw new Exception("Missing pose bone " + boneName);
            bone.rotation = Quaternion.FromToRotation(tip.position - bone.position, model.TransformDirection(direction)) * bone.rotation;
        }

        public static Bounds BakedBounds(GameObject go)
        {
            var bounds = new Bounds(); bool first = true;
            foreach (var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = new Mesh();
                // These FBX rigs contain a 0.01 import scale; include it when baking skinning.
                renderer.BakeMesh(mesh, true);
                foreach (var vertex in mesh.vertices)
                {
                    Vector3 point = renderer.transform.TransformPoint(vertex);
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                    else bounds.Encapsulate(point);
                }
                Object.DestroyImmediate(mesh);
                renderer.updateWhenOffscreen = true;
            }
            return bounds;
        }

        [MenuItem("Tools/Borrowed Time/Install Sleeping Companions")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
            if (EditorSceneManager.GetActiveScene().path != BorrowedMenuBuilder.ScenePath)
                EditorSceneManager.OpenScene(BorrowedMenuBuilder.ScenePath);
            var camera = Camera.main;
            var meadow = camera.GetComponent<MeadowMenuView>();
            if (!meadow || !meadow.characterPlacement) throw new Exception("The menu's meadow placement anchor is missing.");
            var anchor = meadow.characterPlacement;
            var existing = anchor.GetComponentInChildren<SleepingCompanions>();
            if (existing) Object.DestroyImmediate(existing.gameObject);
            anchor.name = "CharacterPlacement - sleeping companions";
            meadow.characterViewport = new Vector2(.30f, .44f);
            meadow.RefreshFraming();

            var pair = new GameObject("Sleeping Companions");
            pair.transform.SetParent(anchor, false);
            pair.transform.localScale = Vector3.one * 4.2f;
            var human = Load("Protagonist"); human.transform.SetParent(pair.transform, false);
            Aim(human.transform, "UpperArm.L", "LowerArm.L", new Vector3(-.65f, .25f, .06f));
            Aim(human.transform, "LowerArm.L", "Hand.L", new Vector3(.18f, .8f, .2f));
            Aim(human.transform, "UpperArm.R", "LowerArm.R", new Vector3(.25f, -.85f, .12f));
            Aim(human.transform, "LowerArm.R", "Hand.R", new Vector3(-.5f, -.15f, .65f));
            Aim(human.transform, "UpperLeg.L", "LowerLeg.L", new Vector3(-.12f, -1, .08f));
            Aim(human.transform, "UpperLeg.R", "LowerLeg.R", new Vector3(.18f, -.9f, .26f));
            Aim(human.transform, "LowerLeg.R", "Foot.R", new Vector3(-.08f, -.95f, -.18f));
            human.transform.localRotation = Quaternion.Euler(0, 156, 0) * Quaternion.Euler(-90, 0, 0);
            var humanBounds = BakedBounds(human);
            if (humanBounds.size.magnitude > 20) throw new Exception("Unexpected human skinning bounds: " + humanBounds);
            var humanCenter = pair.transform.TransformPoint(new Vector3(-.35f, 0, 0));
            human.transform.position += new Vector3(humanCenter.x - humanBounds.center.x,
                humanCenter.y + .10f - humanBounds.min.y, humanCenter.z - humanBounds.center.z);

            var robot = Load("Robo"); robot.transform.SetParent(pair.transform, false);
            robot.transform.localRotation = Quaternion.Euler(0, 192, 0) * Quaternion.Euler(-90, 0, -10);
            var robotBounds = BakedBounds(robot);
            if (robotBounds.size.magnitude > 10) throw new Exception("Unexpected robot skinning bounds: " + robotBounds);
            var robotCenter = pair.transform.TransformPoint(new Vector3(.75f, 0, .45f));
            robot.transform.position += new Vector3(robotCenter.x - robotBounds.center.x,
                robotCenter.y + .10f - robotBounds.min.y, robotCenter.z - robotBounds.center.z);

            var companions = pair.AddComponent<SleepingCompanions>();
            companions.menuCamera = camera;
            companions.human = human.transform; companions.robot = robot.transform;
            companions.humanFace = human.GetComponentInChildren<SkinnedMeshRenderer>();
            companions.robotFace = robot.GetComponentInChildren<SkinnedMeshRenderer>();
            companions.menuDocument = Object.FindFirstObjectByType<BorrowedMainMenu>().GetComponent<UnityEngine.UIElements.UIDocument>();
            if (SleepingCompanions.FindExpression(companions.humanFace, "Blink") < 0 || SleepingCompanions.FindExpression(companions.robotFace, "Sleep") < 0)
                throw new Exception("Character expression blend shapes are missing.");
            foreach (var face in new[] { companions.humanFace, companions.robotFace })
                for (int i = 0; i < face.sharedMesh.blendShapeCount; i++) face.SetBlendShapeWeight(i, 0);
            companions.InitializeRestPose();
            foreach (var renderer in pair.GetComponentsInChildren<Renderer>())
            { renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true; }
            EditorUtility.SetDirty(meadow);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = pair;
            Debug.Log("[Borrowed Time] Sleeping companions installed, with hover-driven human eyelids.");
        }
    }
}
#endif
