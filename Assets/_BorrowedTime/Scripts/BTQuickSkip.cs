using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace BorrowedTime {
// Explicit preview shortcut. Preview state never replaces the player's saves.
public class BTQuickSkip : MonoBehaviour {
 public static bool PreviewSession {get;private set;}
 public static bool IsLoading => loading;
 static BTQuickSkip instance;static bool pendingEnding,loading;
 GUIStyle label;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
 static void ResetSession(){instance=null;PreviewSession=pendingEnding=loading=false;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){if(instance)return;instance=new GameObject("F8 / Journey preview").AddComponent<BTQuickSkip>();DontDestroyOnLoad(instance.gameObject);}
 void Awake(){SceneManager.sceneLoaded+=Loaded;}
 void OnDestroy(){SceneManager.sceneLoaded-=Loaded;if(instance==this)instance=null;}
 public static bool ShortcutPressed()=>Keyboard.current!=null&&Keyboard.current.f8Key.wasPressedThisFrame;
 void Update(){if(ShortcutPressed())Advance();}
 public static void ExitPreview(){PreviewSession=false;pendingEnding=false;}
 static void EnterPreview(){PreviewSession=true;BTSaveGame.CancelPendingContinue();Time.timeScale=1;}
 public static void Advance(){
  if(loading)return;
  var ending=Object.FindFirstObjectByType<BorrowedTime.Ending.BTEndingCutscene>();
  if(ending){if(!ending.Finished)ending.SkipToCredits();else ending.ReturnToMainMenu();return;}
  string path=SceneManager.GetActiveScene().path;
  if(path==BTJourney.TrainingPath){LoadCityPreview();return;}
  if(path==BTJourney.CityPath){LoadFactoryPreview();return;}
  var g=Object.FindFirstObjectByType<BTFactoryGame>();
  if(g&&g.completed){var film=g.GetComponent<BTWorldEndingCinematic>();if(film&&film.Playing){film.Finish();return;}LoadFactoryPreview();return;}
  LoadEndingPreview();
 }
 public static void LoadCityPreview(){
  EnterPreview();loading=true;pendingEnding=false;BTJourney.HasMiracleSeed=true;BTJourney.LoadCity(100);
 }
 public static void LoadFactoryPreview(){
  EnterPreview();loading=true;pendingEnding=false;BTJourney.HasMiracleSeed=true;BTJourney.FromTraining=true;BTJourney.LoadFactory(100,true,true);
 }
 public static void LoadEndingPreview(){
  EnterPreview();loading=true;pendingEnding=true;BTJourney.HasMiracleSeed=true;BTJourney.FromTraining=true;BTJourney.LoadFactory(100,true,true);
 }
 void Loaded(Scene scene,LoadSceneMode mode){StartCoroutine(AfterStart(scene));}
 IEnumerator AfterStart(Scene scene){
  yield return null;loading=false;
  if(pendingEnding&&scene.path==BTJourney.FactoryPath){pendingEnding=false;CompleteFactoryPreview();}
 }
 public static void PrepareFactoryInPlace(){
  EnterPreview();BTJourney.HasMiracleSeed=true;BTJourney.FromCity=true;BTJourney.CityWaterRestored=BTJourney.CityGardenRestored=true;
  var g=Object.FindFirstObjectByType<BTFactoryGame>();if(!g)return;g.PreparePreviewEntry();g.player.paused=false;if(g.stage==0)g.Begin();
 }
 static void CompleteFactoryPreview(){
  var g=Object.FindFirstObjectByType<BTFactoryGame>();if(!g)return;
  PrepareFactoryInPlace();g.entryOpen=g.transportRestored=g.conveyorOpen=g.pressureBalanced=g.powerLatched=g.pollutionStopped=g.waterFlowRestored=true;
  for(int i=0;i<g.valvesClosed.Length;i++)g.valvesClosed[i]=true;
  foreach(var t in new[]{g.bridge,g.liftControl,g.coreConsole,g.emergencyWalkway})if(t){t.age=1;t.Apply();}
  g.conveyor.age=2;g.conveyor.Apply();g.nursery.age=1;g.nursery.Apply();g.nurseryRestored=true;
  g.hasMiracleSeed=false;g.seedPlanted=true;g.miracleTree.gameObject.SetActive(true);g.miracleTree.age=2;g.miracleTree.Apply();
  BTJourney.HasMiracleSeed=false;BTJourney.SeedPlanted=BTJourney.PollutionStopped=BTJourney.FinalNurseryRestored=true;
  g.player.checkpoint=new Vector3(0,12.1f,18);g.player.Respawn();g.RefreshRoutes();
 }
 void OnGUI(){
  if(Object.FindFirstObjectByType<BorrowedTime.Ending.BTEndingCutscene>())return;
  if(label==null){var g=Object.FindFirstObjectByType<BTTrainingGame>();var sky=Object.FindFirstObjectByType<BTSkyCityGame>();label=new GUIStyle(GUI.skin.label){font=g?g.uiFont:sky&&sky.hints?sky.hints.thaiFont:null,fontSize=14};label.normal.textColor=new Color(.62f,.94f,.96f);}
  GUI.depth=-100;GUI.matrix=Matrix4x4.identity;
  GUI.Label(new Rect(24,Screen.height-73,650,24),PreviewSession?"F8 ข้ามด่าน / ข้ามคัตซีน • โหมดพรีวิว ไม่บันทึกเซฟ":"F8 ข้ามไปท้ายด่าน (พรีวิว)",label);
 }
}}
