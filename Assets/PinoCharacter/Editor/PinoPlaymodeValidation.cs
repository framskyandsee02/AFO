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
using Object = UnityEngine.Object;

namespace WobbleWorks.Characters.Editor
{
    /// <summary>
    /// Explicit editor-only route validation. Runs the team's unchanged OopsV2Test
    /// in the generated integration scene, then restores the original scene setup
    /// and original team evidence bytes. Session state survives Play Mode reloads.
    /// </summary>
    [InitializeOnLoad]
    public static class PinoPlaymodeValidation
    {
        const string ActiveKey = "Pino.Character.PlaymodeValidation.Active";
        const string StatePath = "Temp/PinoPlaymode.state.json";
        const string ResultPath = "Temp/PinoPlaymode.result.json";
        const string SourceReportPath = "Assets/OopsPlanet/Evidence/v2-test.json";
        const string RouteCopyPath = "Assets/PinoCharacter/Evidence/GameplayRoute.json";
        const string IntegrationReportPath = "Assets/PinoCharacter/Evidence/PlaymodeIntegration.json";
        const double TimeoutSeconds = 600;

        [Serializable] sealed class SceneEntry
        {
            public string path;
            public bool isLoaded;
            public bool isActive;
            public bool emptyDefaultObjects;
        }

        [Serializable] public sealed class IntegrationReport
        {
            public string requestId;
            public string startedUtc;
            public string completedUtc;
            public string phase;
            public bool completed;
            public bool passed;
            public bool success;
            public string detail;
            public double elapsedRealtimeSeconds;
            public int routePassed;
            public int routeFailed;
            public bool completedRoute;
            public int baselineRoutePassed;
            public int baselineRouteFailed;
            public bool baselineCompletedRoute;
            public bool baselineReportAvailable;
            public int sampledFrames;
            public int alignedStateSamples;
            public int stateMismatchSamples;
            public float maximumSpringCompression;
            public float maximumSpringStretch;
            public float maximumLiquidFlow;
            public float maximumWalkingHopMetres;
            public bool sourceReportRestored;
            public bool sceneSetupRestored;
            public List<string> observedForms = new List<string>();
            public List<string> failures = new List<string>();
        }

        [Serializable] sealed class RunState
        {
            public IntegrationReport report = new IntegrationReport();
            public SceneEntry[] scenes;
            public bool originalReportExisted;
            public bool originalMetaExisted;
            public string reportBackup;
            public string metaBackup;
            public long startUtcTicks;
            public double startEditorSeconds;
            public bool routeCopied;
            public bool stopRequested;
            public bool playModeEntered;
            public bool editModeReturned;
            public int lastSampleFrame = -1;
            public int lastPropertyFrame;
            public int lastProperty = -1;
        }

        static RunState state;
        static BlobWorld runtimeWorld;
        static PinoMorphController runtimeMorph;
        static double nextPersist;
        static bool cleaning;

        static PinoPlaymodeValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Update;
            EditorApplication.quitting += OnEditorQuitting;
            if (SessionState.GetBool(ActiveKey, false))
            {
                LoadState();
                EditorApplication.delayCall += ResumeAfterReload;
            }
        }

        /// <summary>Start only on explicit request; the normal game never creates this runner.</summary>
        [MenuItem("Tools/Pino Character/Run Integrated Gameplay Playtest")]
        public static void Start()
        {
            if (SessionState.GetBool(ActiveKey, false)) throw new InvalidOperationException("A Pino Play Mode validation is already active.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start Pino validation outside Play Mode.");
            if (!File.Exists(PinoCharacterBuilder.IntegrationPath)) throw new FileNotFoundException("Build the Pino integration scene first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty) throw new InvalidOperationException("Save pending scene edits before validation: " + scene.name);
                if (string.IsNullOrEmpty(scene.path) && scene.GetRootGameObjects().Any(o => o.name != "Main Camera" && o.name != "Directional Light"))
                    throw new InvalidOperationException("Save the untitled scene before validation so its contents can be restored exactly.");
            }

            string id = Guid.NewGuid().ToString("N");
            Directory.CreateDirectory("Temp");
            Directory.CreateDirectory("Assets/PinoCharacter/Evidence");
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            state = new RunState
            {
                scenes = setup.Select(s => new SceneEntry
                {
                    path = s.path,
                    isLoaded = s.isLoaded,
                    isActive = s.isActive,
                    emptyDefaultObjects = string.IsNullOrEmpty(s.path) && SceneManager.GetSceneByPath(s.path).IsValid() &&
                        SceneManager.GetSceneByPath(s.path).GetRootGameObjects().Length > 0
                }).ToArray(),
                originalReportExisted = File.Exists(SourceReportPath),
                originalMetaExisted = File.Exists(SourceReportPath + ".meta"),
                reportBackup = "Temp/PinoPlaymode.original-playtest-" + id + ".bin",
                metaBackup = "Temp/PinoPlaymode.original-playtest-meta-" + id + ".bin",
                startUtcTicks = DateTime.UtcNow.Ticks,
                startEditorSeconds = EditorApplication.timeSinceStartup,
                report = new IntegrationReport
                {
                    requestId = id,
                    startedUtc = DateTime.UtcNow.ToString("O"),
                    phase = "starting",
                    detail = "Opening the generated Pino integration scene for the team's unchanged route test."
                }
            };
            if (state.originalReportExisted)
            {
                File.WriteAllBytes(state.reportBackup, File.ReadAllBytes(SourceReportPath));
                try
                {
                    var baseline = JsonUtility.FromJson<OopsV2Test.Report>(File.ReadAllText(SourceReportPath));
                    if (baseline != null)
                    {
                        state.report.baselineReportAvailable = true;
                        state.report.baselineRoutePassed = baseline.passed;
                        state.report.baselineRouteFailed = baseline.failed;
                        state.report.baselineCompletedRoute = (baseline.completed || baseline.checks.Count == 57);
                    }
                }
                catch (Exception) { /* Baseline bytes are still backed up even when not valid JSON. */ }
            }
            if (state.originalMetaExisted) File.WriteAllBytes(state.metaBackup, File.ReadAllBytes(SourceReportPath + ".meta"));
            SessionState.SetBool(ActiveKey, true);
            Persist();
            try
            {
                EditorSceneManager.OpenScene(PinoCharacterBuilder.IntegrationPath, OpenSceneMode.Single);
                state.report.phase = "enteringPlayMode";
                Persist();
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail("Unable to enter Play Mode: " + exception.Message);
                CompleteCleanup();
                throw;
            }
        }

        static void LoadState()
        {
            if (!File.Exists(StatePath))
            {
                SessionState.SetBool(ActiveKey, false);
                Debug.LogError("Pino validation state is missing; automatic restoration cannot proceed.");
                return;
            }
            try { state = JsonUtility.FromJson<RunState>(File.ReadAllText(StatePath)); }
            catch (Exception error)
            {
                SessionState.SetBool(ActiveKey, false);
                Debug.LogException(error);
            }
        }

        static void ResumeAfterReload()
        {
            if (!Active) return;
            if (EditorApplication.isPlaying)
            {
                if (state.stopRequested) EditorApplication.isPlaying = false;
                else if (state.report.phase == "running") FindRuntimeObjects(false);
            }
            else if (!EditorApplication.isPlayingOrWillChangePlaymode && state.stopRequested)
            {
                // On exit, a domain reload can run this callback before EnteredEditMode.
                // Wait a further editor turn rather than restoring scenes during teardown.
                EditorApplication.delayCall += () =>
                {
                    if (!Active || EditorApplication.isPlayingOrWillChangePlaymode) return;
                    state.editModeReturned = true;
                    CompleteCleanup();
                };
            }
        }

        static bool Active => SessionState.GetBool(ActiveKey, false) && state != null;

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (!Active) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    state.playModeEntered = true;
                    state.editModeReturned = false;
                    FindRuntimeObjects(true);
                    state.report.phase = "running";
                    state.report.detail = "The team's OopsV2Test is traversing the generated integration scene at normal time scale.";
                    Persist();
                }
                catch (Exception error) { BeginStop("Play Mode initialization failed: " + error.Message); }
            }
            else if (change == PlayModeStateChange.ExitingPlayMode && !state.stopRequested)
            {
                Fail("Play Mode was stopped before the route report completed.");
                state.stopRequested = true;
                try { RestoreEvidence(); }
                catch (Exception error) { Fail("Original evidence restoration failed: " + error.Message); }
                Persist();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                state.editModeReturned = true;
                CompleteCleanup();
            }
        }

        static void FindRuntimeObjects(bool attachTest)
        {
            runtimeWorld = Object.FindObjectsByType<BlobWorld>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(w => w.gameObject.scene.path == PinoCharacterBuilder.IntegrationPath);
            if (!runtimeWorld || !runtimeWorld.player) throw new InvalidOperationException("The generated scene has no ready BlobWorld/player.");
            var adapter = runtimeWorld.player.GetComponentsInChildren<PinoGameplayAdapter>(true).FirstOrDefault(a => a.Player == runtimeWorld.player);
            if (!adapter || !adapter.Morph) throw new InvalidOperationException("Pino adapter is missing or is not connected to the existing player.");
            runtimeMorph = adapter.Morph;
            if (attachTest && !runtimeWorld.GetComponent<OopsV2Test>()) runtimeWorld.gameObject.AddComponent<OopsV2Test>();
        }

        static void Update()
        {
            if (!Active || cleaning) return;
            state.report.elapsedRealtimeSeconds = EditorApplication.timeSinceStartup - state.startEditorSeconds;
            if (!state.stopRequested && state.report.elapsedRealtimeSeconds > TimeoutSeconds)
            {
                BeginStop("Timed out after 600 real-time seconds; no successful route report was received.");
                return;
            }
            if (state.stopRequested)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && (!state.playModeEntered || state.editModeReturned)) CompleteCleanup();
                return;
            }
            if (!EditorApplication.isPlaying || state.report.phase != "running") return;
            try
            {
                if (!runtimeWorld || !runtimeMorph) FindRuntimeObjects(false);
                SampleMorphState();
                if (FreshRouteReportAvailable())
                {
                    CaptureRouteAndStop();
                    return;
                }
                if (EditorApplication.timeSinceStartup >= nextPersist)
                {
                    nextPersist = EditorApplication.timeSinceStartup + .5;
                    Persist();
                }
            }
            catch (Exception error) { BeginStop("Play Mode monitoring failed: " + error.Message); }
        }

        static void SampleMorphState()
        {
            if (state.lastSampleFrame == Time.frameCount) return;
            state.lastSampleFrame = Time.frameCount;
            state.report.sampledFrames++;
            BlobPlayer source = runtimeWorld.player;
            if ((int)source.property != state.lastProperty)
            {
                state.lastProperty = (int)source.property;
                state.lastPropertyFrame = Time.frameCount;
            }
            string form = source.property.ToString();
            if (!state.report.observedForms.Contains(form)) state.report.observedForms.Add(form);
            state.report.maximumSpringCompression = Mathf.Max(state.report.maximumSpringCompression, runtimeMorph.GetMorphWeight("SpringCompress"));
            state.report.maximumSpringStretch = Mathf.Max(state.report.maximumSpringStretch, runtimeMorph.GetMorphWeight("SpringStretch"));
            state.report.maximumLiquidFlow = Mathf.Max(state.report.maximumLiquidFlow, runtimeMorph.GetMorphWeight("LiquidFlow"));
            var locomotion = runtimeMorph.GetComponent<PinoLocomotionAnimator>();
            if (locomotion && locomotion.MotionPivot)
                state.report.maximumWalkingHopMetres = Mathf.Max(state.report.maximumWalkingHopMetres,
                    locomotion.MotionPivot.parent.TransformVector(Vector3.up * locomotion.CurrentHopHeight).magnitude);
            // Editor update can sample before LateUpdate. Allow one frame and ignore
            // transitions rather than reporting a false ordering mismatch.
            if (!runtimeMorph.IsTransitioning && Time.frameCount > state.lastPropertyFrame + 1)
            {
                if (runtimeMorph.TargetForm == PinoGameplayAdapter.FormFor(source.property)) state.report.alignedStateSamples++;
                else
                {
                    state.report.stateMismatchSamples++;
                    string mismatch = "Gameplay " + source.property + " but character target " + runtimeMorph.TargetForm;
                    if (!state.report.failures.Contains(mismatch)) state.report.failures.Add(mismatch);
                }
            }
        }

        static bool FreshRouteReportAvailable()
        {
            if (!File.Exists(SourceReportPath) || File.GetLastWriteTimeUtc(SourceReportPath).Ticks <= state.startUtcTicks) return false;
            var report = JsonUtility.FromJson<OopsV2Test.Report>(File.ReadAllText(SourceReportPath));
            return report != null && DateTime.TryParse(report.utc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime written) &&
                written.ToUniversalTime().Ticks >= state.startUtcTicks;
        }

        static void CaptureRouteAndStop()
        {
            byte[] routeBytes = File.ReadAllBytes(SourceReportPath);
            var route = JsonUtility.FromJson<OopsV2Test.Report>(System.Text.Encoding.UTF8.GetString(routeBytes));
            File.WriteAllBytes(RouteCopyPath, routeBytes);
            state.routeCopied = true;
            state.report.routePassed = route.passed;
            state.report.routeFailed = route.failed;
            state.report.completedRoute = route.completed;
            foreach (var check in route.checks)
                if (!check.passed) state.report.failures.Add("Gameplay route: " + check.name + " | " + check.observed);
            if (!route.completed) state.report.failures.Add("The team's route did not complete.");
            if (state.report.maximumWalkingHopMetres < .02f || state.report.maximumWalkingHopMetres > .08f)
                state.report.failures.Add("Expected a low Normal walking hop on the integrated route: " + state.report.maximumWalkingHopMetres + " metres.");
            foreach (BlobProperty form in new[] { BlobProperty.None, BlobProperty.Magnet, BlobProperty.Bounce, BlobProperty.Pressure, BlobProperty.Water })
                if (!state.report.observedForms.Contains(form.ToString())) state.report.failures.Add("Form was not observed during the route: " + form);
            if (state.report.maximumSpringCompression < .8f) state.report.failures.Add("No sufficiently charged Spring compression was observed.");
            if (state.report.maximumSpringStretch <= .001f) state.report.failures.Add("No Spring take-off stretch was observed; the inferred release bridge needs review.");

            if (state.report.alignedStateSamples == 0) state.report.failures.Add("No settled Gameplay/character state samples were collected.");
            state.report.passed = route.failed == 0 && route.completed && state.report.stateMismatchSamples == 0 && state.report.failures.Count == 0;
            state.report.success = state.report.passed;
            state.report.detail = "Team route: passed=" + route.passed + ", failed=" + route.failed + ", completed=" + route.completed +
                ". Settled state mismatches=" + state.report.stateMismatchSamples + ".";
            BeginStop(null);
        }

        static void Fail(string detail)
        {
            state.report.passed = false;
            state.report.success = false;
            state.report.detail = detail;
            if (!state.report.failures.Contains(detail)) state.report.failures.Add(detail);
        }

        static void BeginStop(string error)
        {
            if (!Active) return;
            if (!string.IsNullOrEmpty(error)) Fail(error);
            state.stopRequested = true;
            state.report.phase = "exitingPlayMode";
            try { RestoreEvidence(); }
            catch (Exception exception) { Fail("Original evidence restoration failed: " + exception.Message); }
            Persist();
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            else CompleteCleanup();
        }

        static void RestoreEvidence()
        {
            if (state.originalReportExisted)
            {
                byte[] original = File.ReadAllBytes(state.reportBackup);
                File.WriteAllBytes(SourceReportPath, original);
                state.report.sourceReportRestored = File.ReadAllBytes(SourceReportPath).SequenceEqual(original);
            }
            else
            {
                if (File.Exists(SourceReportPath) && (state.routeCopied || FreshRouteReportAvailable())) File.Delete(SourceReportPath);
                state.report.sourceReportRestored = !File.Exists(SourceReportPath);
            }
            if (state.originalMetaExisted) File.WriteAllBytes(SourceReportPath + ".meta", File.ReadAllBytes(state.metaBackup));
            else if (!state.originalReportExisted && state.routeCopied && File.Exists(SourceReportPath + ".meta")) File.Delete(SourceReportPath + ".meta");
        }

        static void CompleteCleanup()
        {
            if (!Active || cleaning || EditorApplication.isPlayingOrWillChangePlaymode || (state.playModeEntered && !state.editModeReturned)) return;
            cleaning = true;
            try
            {
                RestoreEvidence();
                RestoreSceneSetup();
                state.report.sceneSetupRestored = true;
                AssetDatabase.ImportAsset(RouteCopyPath);
                if (state.originalReportExisted) AssetDatabase.ImportAsset(SourceReportPath);
            }
            catch (Exception error) { Fail("Validation cleanup failed: " + error.Message); }
            finally
            {
                state.report.completed = true;
                state.report.completedUtc = DateTime.UtcNow.ToString("O");
                state.report.elapsedRealtimeSeconds = EditorApplication.timeSinceStartup - state.startEditorSeconds;
                state.report.passed &= state.report.sourceReportRestored && state.report.sceneSetupRestored;
                state.report.success = state.report.passed;
                state.report.phase = state.report.passed ? "complete" : "failed";
                Persist();
                SessionState.SetBool(ActiveKey, false);
                runtimeWorld = null;
                runtimeMorph = null;
                cleaning = false;
            }
            string summary = "PINO_PLAYMODE_VALIDATION " + state.report.phase + " | " + state.report.detail + " | " + IntegrationReportPath;
            if (state.report.passed) Debug.Log(summary); else Debug.LogError(summary);
        }

        static void RestoreSceneSetup()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene current = SceneManager.GetSceneAt(i);
                if (current.isDirty && current.path != PinoCharacterBuilder.IntegrationPath)
                    throw new InvalidOperationException("A different scene has pending edits; automatic restoration stopped to preserve it: " + current.name);
            }
            SceneEntry[] saved = state.scenes ?? Array.Empty<SceneEntry>();
            if (saved.Length == 1 && string.IsNullOrEmpty(saved[0].path))
                EditorSceneManager.NewScene(saved[0].emptyDefaultObjects ? NewSceneSetup.DefaultGameObjects : NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
            {
                if (saved.Any(s => string.IsNullOrEmpty(s.path))) throw new InvalidOperationException("Mixed saved and untitled scene setup requires manual restoration; saved entries remain in " + StatePath);
                EditorSceneManager.RestoreSceneManagerSetup(saved.Select(s => new SceneSetup { path = s.path, isLoaded = s.isLoaded, isActive = s.isActive }).ToArray());
            }
            SceneSetup[] actual = EditorSceneManager.GetSceneManagerSetup();
            if (actual.Length != saved.Length || actual.Where((s, i) => s.path != saved[i].path || s.isLoaded != saved[i].isLoaded || s.isActive != saved[i].isActive).Any())
                throw new InvalidOperationException("The original scene setup did not restore exactly; inspect " + StatePath);
        }

        static void Persist()
        {
            if (state == null) return;
            File.WriteAllText(StatePath, JsonUtility.ToJson(state, true));
            string report = JsonUtility.ToJson(state.report, true);
            File.WriteAllText(ResultPath, report);
            // Frequent progress belongs in Temp; avoid repeated asset imports during physics tests.
            if (state.report.completed || state.report.phase == "starting") File.WriteAllText(IntegrationReportPath, report);
        }

        static void OnEditorQuitting()
        {
            if (!Active) return;
            try { RestoreEvidence(); }
            catch (Exception error) { Debug.LogError("Restore Pino validation evidence from the Temp backups: " + error.Message); }
        }
    }
}
#endif
