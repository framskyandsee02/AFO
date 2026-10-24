#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BorrowedTime.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace BorrowedTime.Editor
{
    [InitializeOnLoad]
    public static class MenuAudioValidation
    {
        const string Active = "BorrowedTime.AudioValidation";
        const string ReportPath = "Art/BorrowedTime/Audio/Validation.json";
        static readonly string[] Prefs = { MenuAudioSettings.Key(MenuAudioChannel.Master), MenuAudioSettings.Key(MenuAudioChannel.Music), MenuAudioSettings.Key(MenuAudioChannel.Effects), "BorrowedTime.Audio.Muted" };
        [Serializable] sealed class Check { public string name; public bool passed; }
        [Serializable] sealed class Report { public bool completed, success; public List<Check> checks = new List<Check>(); }
        static Report report;
        static MenuAudioFeedback audio;
        static int stage;
        static double started, next;
        static float peak, musicTime;
        static string gameplayScene;
        static readonly float[] Samples = new float[1024];

        static MenuAudioValidation() { EditorApplication.update += Tick; }
        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Start audio validation outside Play Mode.");
            Directory.CreateDirectory("Art/BorrowedTime/Audio");
            report = new Report(); Save(); stage = 0; started = next = 0;
            for (int i = 0; i < Prefs.Length; i++)
            {
                SessionState.SetBool(Active + ".Exists" + i, PlayerPrefs.HasKey(Prefs[i]));
                SessionState.SetInt(Active + ".Value" + i, PlayerPrefs.GetInt(Prefs[i], i == 2 ? 100 : i == 3 ? 0 : 80));
            }
            SessionState.SetBool(Active + ".Background", Application.runInBackground);
            SessionState.SetBool(Active, true); Application.runInBackground = true;
            EditorSceneManager.OpenScene(BorrowedMenuBuilder.ScenePath); EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (report == null) report = JsonUtility.FromJson<Report>(File.ReadAllText(ReportPath));
            if (started == 0) started = EditorApplication.timeSinceStartup;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 60) throw new Exception("Audio validation timed out.");
                if (!audio) audio = Object.FindFirstObjectByType<MenuAudioFeedback>();
                if (!audio) return;
                if (stage == 0 && !audio.music.isPlaying) return;
                if (stage == 1) Observe(audio.music);
                else if (stage == 2 || stage == 5 || stage == 6 || stage == 10) Observe(audio.hover);
                else if (stage == 3 || stage == 11) Observe(audio.click);
                if (EditorApplication.timeSinceStartup < next) return;
                if (stage == 0)
                {
                    gameplayScene = Object.FindFirstObjectByType<BorrowedMainMenu>().playSceneName;
                    CheckResult(audio.music.clip.name == "sokoniai wo hitohira" && audio.music.clip.length > 30 && audio.music.loop && audio.music.isPlaying, "The requested soundtrack is decoded and playing on loop");
                    CheckResult(audio.hover.clip == audio.click.clip && audio.click.clip.name == "oxidvideos-soft-pop-sfx-433612", "Hover and click use the requested pop clip");
                    CheckResult(audio.music.spatialBlend == 0 && audio.hover.spatialBlend == 0 && audio.click.spatialBlend == 0, "All menu audio is 2D");
                    MenuAudioSettings.SetMuted(false); MenuAudioSettings.Set(MenuAudioChannel.Master, 75);
                    MenuAudioSettings.Set(MenuAudioChannel.Music, 50); MenuAudioSettings.Set(MenuAudioChannel.Effects, 20);
                    CheckResult(Mathf.Abs(audio.music.volume - .25f) < .001f && Mathf.Abs(audio.hover.volume - .2f) < .001f && Mathf.Abs(audio.click.volume - .2f) < .001f && Mathf.Abs(AudioListener.volume - .75f) < .001f, "Music, Effects and Master independently control the real audio sources");
                }
                else if (stage == 1) { CheckResult(peak > .00001f, "Soundtrack produces nonzero audio samples"); Hover("play-button"); }
                else if (stage == 2) { CheckResult(peak > .00001f, "Hovering Play produces real pop audio"); Click("setting-button"); }
                else if (stage == 3)
                {
                    CheckResult(peak > .00001f && Object.FindFirstObjectByType<BorrowedMainMenu>().SettingsVisible, "Mouse clicking Settings plays the click pop and opens Settings");
                    Submit("back-button");
                }
                else if (stage == 4)
                {
                    CheckResult(!Object.FindFirstObjectByType<BorrowedMainMenu>().SettingsVisible, "Back still closes Settings");
                    Hover("setting-button");
                }
                else if (stage == 5) { CheckResult(peak > .00001f, "Hovering Settings produces real pop audio"); Hover("quit-button"); }
                else if (stage == 6)
                {
                    CheckResult(peak > .00001f, "Hovering Exit produces real pop audio");
                    MenuAudioSettings.SetMuted(true);
                    CheckResult(AudioListener.volume == 0 && audio.music.isPlaying, "Mute silences output while the soundtrack keeps its position");
                    MenuAudioSettings.SetMuted(false);
                    CheckResult(Mathf.Abs(AudioListener.volume - .75f) < .001f, "Unmute restores the previous master volume");
                    musicTime = audio.music.time; Submit("play-button");
                }
                else if (stage == 7)
                {
                    CheckResult(SceneManager.GetActiveScene().name == gameplayScene && audio.music.isPlaying && audio.music.time >= musicTime, "Soundtrack continues into the configured gameplay scene without restarting");
                    SceneManager.LoadScene("BorrowedTimeMainMenu");
                }
                else if (stage == 8)
                {
                    CheckResult(Object.FindObjectsByType<MenuAudioFeedback>(FindObjectsSortMode.None).Length == 1 && audio.music.time >= musicTime, "Returning to the menu does not duplicate or restart the soundtrack");
                }
                else if (stage == 9) Hover("play-button");
                else if (stage == 10)
                {
                    CheckResult(peak > .00001f, "Button audio reconnects when returning to the menu");
                    Click("game-title");
                }
                else if (stage == 11)
                {
                    CheckResult(peak > .00001f, "Clicking the game title produces the same real pop audio as buttons");
                    Finish(); return;
                }
                stage++; peak = 0; next = EditorApplication.timeSinceStartup + (stage == 1 ? 1.5 : .20);
            }
            catch (Exception e) { CheckResult(false, e.ToString()); Finish(); Debug.LogException(e); }
        }

        static void Observe(AudioSource source)
        {
            source.GetOutputData(Samples, 0);
            foreach (float sample in Samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
        }
        static void Hover(string name)
        {
            var button = audio.menuDocument.rootVisualElement.Q<Button>(name);
            using (var e = PointerEnterEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
        }
        static void Submit(string name)
        {
            var button = audio.menuDocument.rootVisualElement.Q<Button>(name);
            using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
        }
        static void Click(string name)
        {
            var button = audio.menuDocument.rootVisualElement.Q<VisualElement>(name);
            var position = button.worldBound.center;
            using (var e = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = position }))
            { e.target = button; button.SendEvent(e); }
            using (var e = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = position }))
            { e.target = button; button.SendEvent(e); }
        }
        static void CheckResult(bool passed, string name) { report.checks.Add(new Check { passed = passed, name = name }); Save(); }
        static void Save() => File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        static void Finish()
        {
            for (int i = 0; i < 3; i++) MenuAudioSettings.Set((MenuAudioChannel)i, SessionState.GetInt(Active + ".Value" + i, i == 2 ? 100 : 80));
            MenuAudioSettings.SetMuted(SessionState.GetInt(Active + ".Value3", 0) != 0);
            for (int i = 0; i < Prefs.Length; i++) if (!SessionState.GetBool(Active + ".Exists" + i, false)) PlayerPrefs.DeleteKey(Prefs[i]);
            PlayerPrefs.Save(); Application.runInBackground = SessionState.GetBool(Active + ".Background", false);
            report.completed = true; report.success = report.checks.All(c => c.passed); Save();
            SessionState.SetBool(Active, false); EditorApplication.isPlaying = false;
            Debug.Log("[Borrowed Time] Menu audio validation: " + report.success);
        }
    }
}
#endif
