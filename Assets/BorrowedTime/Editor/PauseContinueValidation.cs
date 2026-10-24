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
using Object=UnityEngine.Object;
using Cursor=UnityEngine.Cursor;

namespace BorrowedTime.Editor
{
    [InitializeOnLoad]
    public static class PauseContinueValidation
    {
        const string Active="BorrowedTime.PauseContinue.Validation";
        const string Folder="Art/BorrowedTime/PauseContinue";
        const string ReportPath=Folder+"/Validation.json";
        [Serializable]sealed class Check{public string name;public bool passed;}
        [Serializable]sealed class Report{public bool completed,success;public List<Check> checks=new List<Check>();}
        static Report report;
        static int stage;
        static double next,started;
        static float frozenTime;
        static Vector3 checkpoint,pausedPosition;
        static GamePauseMenu pause;
        static UIDocument preview;
        static PanelSettings previewPanel;
        static Camera camera;
        static RenderTexture texture,previousTexture;
        static string SavePath=>Path.GetFullPath("Temp/PauseContinueValidation/save.json");
        static PauseContinueValidation(){EditorApplication.update+=Tick;}

        public static void Begin()
        {
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            if(File.Exists(SavePath))File.Delete(SavePath);
            report=new Report();Save();stage=0;next=started=0;
            SessionState.SetBool(Active,true);
            SessionState.SetBool(Active+".Background",Application.runInBackground);
            foreach(MenuAudioChannel channel in Enum.GetValues(typeof(MenuAudioChannel)))
            {
                string key=MenuAudioSettings.Key(channel);
                SessionState.SetBool(Active+key+".Exists",PlayerPrefs.HasKey(key));
                SessionState.SetInt(Active+key,PlayerPrefs.GetInt(key,channel==MenuAudioChannel.Effects?100:80));
            }
            Application.runInBackground=true;
            EditorSceneManager.OpenScene(BorrowedMenuBuilder.ScenePath);EditorApplication.isPlaying=true;
        }

        static void Tick()
        {
            if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            EditorApplication.QueuePlayerLoopUpdate();
            if(report==null)report=JsonUtility.FromJson<Report>(File.ReadAllText(ReportPath));
            if(started==0)started=EditorApplication.timeSinceStartup;
            BTSaveGame.TestPathOverride=SavePath;
            try
            {
                if(EditorApplication.timeSinceStartup-started>180)throw new Exception("Pause/Continue validation timed out");
                if(EditorApplication.timeSinceStartup<next)return;
                var main=Object.FindFirstObjectByType<BorrowedMainMenu>();
                if(stage==0)
                {
                    if(!main||Time.frameCount<5||main.GetComponent<UIDocument>().rootVisualElement.layout.width<10)return;
                    if(File.Exists(SavePath))File.Delete(SavePath);
                    main.RefreshContinueState();
                    CheckResult(!main.GetComponent<UIDocument>().rootVisualElement.Q<Button>("continue-button").enabledSelf,"Continue is disabled when no saved game exists");
                    File.WriteAllText(SavePath,"{invalid save");main.RefreshContinueState();
                    CheckResult(!BTSaveGame.CanContinue&&!BTSaveGame.Continue(),"Malformed saves cannot start a Continue load");
                    Submit(main,"play-button");
                }
                else if(stage==1)
                {
                    var game=Object.FindFirstObjectByType<BTTrainingGame>();if(!game||!game.player)return;
                    game.GetComponent<BTPresentation>().briefingComplete=true;game.Begin();
                    game.player.energy=61;checkpoint=game.player.checkpoint;
                    BTSaveGame.Save(game);
                    CheckResult(BTSaveGame.CanContinue,"The existing game writes a valid Continue save");
                    pause=game.player.GetComponent<GamePauseMenu>();pause.HandleEscape();
                    frozenTime=Time.time;pausedPosition=game.player.transform.position;
                    CheckResult(pause.IsOpen&&game.player.paused&&Time.timeScale==0&&Cursor.visible&&Cursor.lockState==CursorLockMode.None,"Escape opens a real pause with frozen time and an unlocked cursor");
                    PreparePreview(pause.Menu);
                }
                else if(stage==2)
                {
                    CheckResult(Mathf.Approximately(Time.time,frozenTime)&&Vector3.Distance(pause.transform.position,pausedPosition)<.001f,"Game time and player position remain frozen while the pause UI runs");
                    CheckResult(pause.Menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>("play-button").text=="Resume","The pause menu shows Resume");
                    Capture("Pause_1920x1080");
                    Submit(pause.Menu,"setting-button");Submit(preview.GetComponent<BorrowedMainMenu>(),"setting-button");
                }
                else if(stage==3)
                {
                    CheckResult(pause.IsOpen&&pause.Menu.SettingsVisible&&Time.timeScale==0,"Settings opens over the frozen game");
                    var root=pause.Menu.GetComponent<UIDocument>().rootVisualElement;
                    root.Q<SliderInt>("master-volume").value=37;
                    CheckResult(Mathf.Abs(AudioListener.volume-.37f)<.001f,"Pause Settings changes the actual master audio volume");
                    root.Q<SliderInt>("effects-volume").value=25;
                    CheckResult(Mathf.Abs(pause.GetComponent<BTPlayer>().game.GetComponent<AudioSource>().volume-.25f)<.001f,"Pause Settings controls the gameplay effects audio source");
                    Capture("Pause_Settings");CleanupPreview();
                    pause.HandleEscape();
                    CheckResult(pause.IsOpen&&!pause.Menu.SettingsVisible&&Time.timeScale==0,"Escape from Settings returns to the pause menu");
                }
                else if(stage==4)
                {
                    Submit(pause.Menu,"play-button");
                    CheckResult(!pause.IsOpen&&!pause.GetComponent<BTPlayer>().paused&&Time.timeScale==1&&Cursor.lockState==CursorLockMode.Locked,"Resume restores gameplay, time and cursor lock");
                    Time.timeScale=.5f;pause.Pause();pause.Resume();
                    CheckResult(Time.timeScale==.5f,"Resume restores the time scale that existed before pausing");Time.timeScale=1;
                    pause.Pause();Submit(pause.Menu,"quit-button");
                }
                else if(stage==5)
                {
                    if(!main||main.pauseOwner)return;
                    CheckResult(SceneManager.GetActiveScene().name=="BorrowedTimeMainMenu"&&Time.timeScale==1,"Quit returns to the main menu without leaving time frozen");
                    main.RefreshContinueState();
                    CheckResult(main.GetComponent<UIDocument>().rootVisualElement.Q<Button>("continue-button").enabledSelf,"Continue is enabled after Quit saves progress");
                    PreparePreview(main);
                }
                else if(stage==6)
                {
                    Capture("MainMenu_Continue");CleanupPreview();Submit(main,"continue-button");
                }
                else if(stage==7)
                {
                    var game=Object.FindFirstObjectByType<BTTrainingGame>();if(!game||game.stage==0)return;
                    CheckResult(game.gameObject.scene.path==BTJourney.TrainingPath&&Mathf.Abs(game.player.energy-61)<.01f&&Vector3.Distance(game.player.checkpoint,checkpoint)<.01f,"Continue restores the saved gameplay scene, energy and checkpoint");
                    pause=game.player.GetComponent<GamePauseMenu>();pause.Pause();SceneManager.LoadScene(BTJourney.CityPath);
                }
                else if(stage==8)
                {
                    var game=Object.FindFirstObjectByType<BTCityGame>();if(!game)return;
                    CheckResult(Time.timeScale==1,"Unloading a paused scene restores time for the next scene");
                    pause=game.player.GetComponent<GamePauseMenu>();pause.Pause();
                    CheckResult(pause.IsOpen&&Time.timeScale==0,"ForgottenCity uses the same working pause menu");pause.Resume();SceneManager.LoadScene(BTJourney.FactoryPath);
                }
                else if(stage==9)
                {
                    var game=Object.FindFirstObjectByType<BTFactoryGame>();if(!game)return;
                    pause=game.player.GetComponent<GamePauseMenu>();pause.Pause();
                    CheckResult(pause.IsOpen&&Time.timeScale==0,"FactoryInterior uses the same working pause menu");pause.Resume();SceneManager.LoadScene("BorrowedTimeMainMenu");
                }
                else if(stage==10){Finish();return;}
                stage++;next=EditorApplication.timeSinceStartup+.65;
            }
            catch(Exception e){CheckResult(false,e.ToString());CleanupPreview();Finish();Debug.LogException(e);}
        }

        static void Submit(BorrowedMainMenu menu,string name)
        {
            var button=menu.GetComponent<UIDocument>().rootVisualElement.Q<Button>(name);
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}
        }
        static void PreparePreview(BorrowedMainMenu live)
        {
            camera=Camera.main;previousTexture=camera.targetTexture;
            texture=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);texture.Create();camera.targetTexture=texture;
            if(camera.TryGetComponent<MeadowMenuView>(out var meadow))meadow.RefreshFraming();
            previewPanel=Object.Instantiate(live.GetComponent<UIDocument>().panelSettings);previewPanel.targetTexture=texture;previewPanel.clearColor=false;
            var host=new GameObject("Temporary Pause Continue Preview"){hideFlags=HideFlags.HideAndDontSave};host.SetActive(false);
            preview=host.AddComponent<UIDocument>();preview.panelSettings=previewPanel;preview.visualTreeAsset=live.GetComponent<UIDocument>().visualTreeAsset;
            var controller=host.AddComponent<BorrowedMainMenu>();controller.pauseOwner=live.pauseOwner;controller.showForegroundFoliage=false;
            host.SetActive(true);
        }
        static void Capture(string name)
        {
            var previous=RenderTexture.active;var image=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
            try{RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());}
            finally{RenderTexture.active=previous;Object.DestroyImmediate(image);}
        }
        static void CleanupPreview()
        {
            if(camera){camera.targetTexture=previousTexture;if(camera.TryGetComponent<MeadowMenuView>(out var meadow))meadow.RefreshFraming();}
            if(preview)Object.DestroyImmediate(preview.gameObject);if(previewPanel)Object.DestroyImmediate(previewPanel);
            if(texture){texture.Release();Object.DestroyImmediate(texture);}preview=null;previewPanel=null;texture=null;
        }
        static void CheckResult(bool passed,string name){report.checks.Add(new Check{passed=passed,name=name});Save();}
        static void Save()=>File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true));
        static void Finish()
        {
            if(pause&&pause.IsOpen)pause.Resume();Time.timeScale=1;BTSaveGame.TestPathOverride=null;
            foreach(MenuAudioChannel channel in Enum.GetValues(typeof(MenuAudioChannel)))
            {
                string key=MenuAudioSettings.Key(channel);MenuAudioSettings.Set(channel,SessionState.GetInt(Active+key,80));
                if(!SessionState.GetBool(Active+key+".Exists",false))PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();Application.runInBackground=SessionState.GetBool(Active+".Background",false);
            report.completed=true;report.success=report.checks.All(c=>c.passed);Save();SessionState.SetBool(Active,false);EditorApplication.isPlaying=false;
            Debug.Log("[Borrowed Time] Pause/Continue validation: "+report.success);
        }
    }
}
#endif
