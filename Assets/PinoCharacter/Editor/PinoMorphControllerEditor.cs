#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using OopsPlanet;

namespace WobbleWorks.Characters.Editor
{
    [CustomEditor(typeof(PinoMorphController))]
    public class PinoMorphControllerEditor : UnityEditor.Editor
    {
        float compression, stretch, flow, absorption;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var controller = (PinoMorphController)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Character Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Preview only changes this character's geometry and material. It does not grant an ability or run gameplay input.", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
                foreach (BlobProperty form in new[] { BlobProperty.None, BlobProperty.Magnet, BlobProperty.Bounce, BlobProperty.Pressure, BlobProperty.Water })
                    if (GUILayout.Button(form == BlobProperty.None ? "Normal" : form.ToString()))
                        Apply(controller, () => controller.SetMorphForm(form));
            EditorGUI.BeginChangeCheck();
            compression = EditorGUILayout.Slider("Spring compression", compression, 0, 1);
            stretch = EditorGUILayout.Slider("Spring stretch", stretch, 0, 1);
            flow = EditorGUILayout.Slider("Liquid flow", flow, 0, 1);
            absorption = EditorGUILayout.Slider("Absorption reaction", absorption, 0, 1);
            if (EditorGUI.EndChangeCheck()) Apply(controller, () =>
            {
                controller.SetSpringCompression(compression);
                controller.SetSpringStretch(stretch);
                controller.SetLiquidFlow(flow);
                controller.SetAbsorptionProgress(absorption);
                controller.Tick(0);
            });
            if (GUILayout.Button("Restore Normal / clear preview")) Apply(controller, () =>
            {
                compression = stretch = flow = absorption = 0;
                controller.SetSpringCompression(0); controller.SetSpringStretch(0);
                controller.SetLiquidFlow(0); controller.SetAbsorptionProgress(0);
                controller.SetMorphForm(BlobProperty.None); controller.Tick(0);
            });
        }

        static void Apply(PinoMorphController controller, System.Action action)
        {
            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Preview Pino morph");
            action();
            EditorUtility.SetDirty(controller);
            foreach (Renderer renderer in controller.GetComponentsInChildren<Renderer>(true))
            {
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            if (controller.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            SceneView.RepaintAll();
        }
    }
}
#endif
