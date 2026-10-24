#if UNITY_EDITOR
using System;
using System.IO;
using BorrowedTime.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace BorrowedTime.Editor
{
    [InitializeOnLoad]
    public static class MenuAudioSetup
    {
        const string Root = "Assets/BorrowedTime/Audio/";
        static MenuAudioSetup() { EditorApplication.update += Tick; }
        static void Tick()
        {
            const string request = "Temp/MenuAudio.request.txt";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string command = File.ReadAllText(request).Trim(); File.Delete(request);
            try
            {
                if (command == "install") Install();
                else if (command == "validate") MenuAudioValidation.Begin();
                else throw new Exception("Unknown menu audio request");
                File.WriteAllText("Temp/MenuAudio.dispatch.txt", "OK " + command);
            }
            catch (Exception e) { File.WriteAllText("Temp/MenuAudio.dispatch.txt", e.ToString()); Debug.LogException(e); }
        }

        static AudioClip Import(string file, bool streaming)
        {
            string path = Root + file;
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (!importer) throw new Exception("Audio clip has not been imported: " + path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = streaming ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = streaming ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = 1;
            settings.preloadAudioData = !streaming;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = !streaming;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static AudioSource Source(Transform parent, string name, AudioClip clip, MenuAudioChannel channel, float volume, float pitch = 1)
        {
            var host = new GameObject(name); host.SetActive(false); host.transform.SetParent(parent, false);
            var source = host.AddComponent<AudioSource>(); source.clip = clip; source.playOnAwake = false;
            source.spatialBlend = 0; source.volume = volume; source.pitch = pitch; source.loop = channel == MenuAudioChannel.Music;
            host.AddComponent<MenuAudioBus>().channel = channel; host.SetActive(true);
            return source;
        }

        [MenuItem("Tools/Borrowed Time/Install Soundtrack and Button Sounds")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
            if (EditorSceneManager.GetActiveScene().path != BorrowedMenuBuilder.ScenePath) EditorSceneManager.OpenScene(BorrowedMenuBuilder.ScenePath);
            var music = Import("sokoniai wo hitohira.mp3", true);
            var pop = Import("oxidvideos-soft-pop-sfx-433612.mp3", false);
            if (!music || !pop || pop.length <= 0) throw new Exception("One of the audio clips could not be decoded.");
            var existing = Object.FindFirstObjectByType<MenuAudioFeedback>();
            if (existing) Object.DestroyImmediate(existing.gameObject);
            var host = new GameObject("Menu Soundtrack and Button Audio");
            var feedback = host.AddComponent<MenuAudioFeedback>();
            feedback.menuDocument = Object.FindFirstObjectByType<BorrowedMainMenu>().GetComponent<UIDocument>();
            feedback.music = Source(host.transform, "Soundtrack - sokoniai wo hitohira", music, MenuAudioChannel.Music, .5f);
            feedback.hover = Source(host.transform, "Button hover pop", pop, MenuAudioChannel.Effects, 1, 1.08f);
            feedback.click = Source(host.transform, "Button click pop", pop, MenuAudioChannel.Effects, 1);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            File.WriteAllText("Art/BorrowedTime/Audio/Import.txt", "Soundtrack: " + music.name + " (" + music.length + " seconds, streaming loop)\nSFX: " + pop.name + " (" + pop.length + " seconds, preloaded mono PCM)\nMusic and Effects use the existing independent audio settings.\n");
            Debug.Log("[Borrowed Time] Soundtrack and hover/click pop installed.");
        }
    }
}
#endif
