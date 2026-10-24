#if UNITY_EDITOR
using System;
using System.IO;
using BorrowedTime.Menu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BorrowedTime.Editor
{
    [InitializeOnLoad]
    public static class PauseContinueSetup
    {
        static PauseContinueSetup() { EditorApplication.update += Tick; }
        static void Tick()
        {
            const string path="Temp/PauseContinue.request.txt";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(path))return;
            string command=File.ReadAllText(path).Trim();File.Delete(path);
            try
            {
                if(command=="install")Install();
                else if(command=="validate")PauseContinueValidation.Begin();
                else throw new Exception("Unknown pause-menu request");
                File.WriteAllText("Temp/PauseContinue.dispatch.txt","OK "+command);
            }
            catch(Exception e){File.WriteAllText("Temp/PauseContinue.dispatch.txt",e.ToString());Debug.LogException(e);}
        }

        [MenuItem("Tools/Borrowed Time/Install Pause Menu Configuration")]
        public static void Install()
        {
            const string folder="Assets/BorrowedTime/Resources";
            const string asset=folder+"/BorrowedTimeMenuUi.asset";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/BorrowedTime","Resources");
            var configuration=AssetDatabase.LoadAssetAtPath<GameMenuConfiguration>(asset);
            if(!configuration){configuration=ScriptableObject.CreateInstance<GameMenuConfiguration>();AssetDatabase.CreateAsset(configuration,asset);}
            configuration.visualTree=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BorrowedMenuBuilder.UiPath+"BorrowedMainMenu.uxml");
            configuration.panelSettings=AssetDatabase.LoadAssetAtPath<PanelSettings>(BorrowedMenuBuilder.UiPath+"BorrowedMenuPanelSettings.asset");
            if(!configuration.visualTree||!configuration.panelSettings)throw new Exception("Menu UI assets are missing.");
            EditorUtility.SetDirty(configuration);AssetDatabase.SaveAssets();
            Debug.Log("[Borrowed Time] Pause menu configuration installed for all BTPlayer scenes.");
        }
    }
}
#endif
