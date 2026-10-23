#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace WobbleWorks.Characters.Editor
{
    /// <summary>Explicit file requests let a local authoring tool use the already-open editor.</summary>
    [InitializeOnLoad]
    public static class PinoCharacterAutomation
    {
        [Serializable] class Request { public string command; public string requestId; }
        [Serializable] class Result
        {
            public bool success;
            public string command;
            public string requestId;
            public string completedUtc;
            public string detail;
        }
        const string RequestPath = "Temp/PinoCharacter.request.json";
        const string ResultPath = "Temp/PinoCharacter.result.json";
        static double nextCheck;
        static bool busy;

        static PinoCharacterAutomation() { EditorApplication.update += Check; }

        static void Check()
        {
            if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(RequestPath)) return;
            busy = true;
            var result = new Result { completedUtc = DateTime.UtcNow.ToString("O") };
            string processing = RequestPath + ".processing";
            try
            {
                if (File.Exists(processing)) File.Delete(processing);
                File.Move(RequestPath, processing);
                Request request = JsonUtility.FromJson<Request>(File.ReadAllText(processing));
                if (request == null) throw new InvalidDataException("Invalid Pino character request JSON.");
                result.command = request.command; result.requestId = request.requestId;
                switch (request.command)
                {
                    case "build": PinoCharacterBuilder.BuildAll(); break;
                    case "capturegallery": PinoCharacterBuilder.CaptureGallery(); break;
                    case "locomotion": PinoLocomotionValidation.BuildValidateAndPlaytest(); break;
                    case "validate":
                        Type type = typeof(PinoCharacterAutomation).Assembly.GetType("WobbleWorks.Characters.Editor.PinoCharacterValidation");
                        MethodInfo method = type?.GetMethod("RunAll", BindingFlags.Public | BindingFlags.Static);
                        if (method == null) throw new MissingMethodException("PinoCharacterValidation.RunAll is not ready.");
                        object detail = method.Invoke(null, null); result.detail = detail?.ToString();
                        break;
                    case "playtest":
                        Type playType = typeof(PinoCharacterAutomation).Assembly.GetType("WobbleWorks.Characters.Editor.PinoPlaymodeValidation");
                        MethodInfo start = playType?.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                        if (start == null) throw new MissingMethodException("PinoPlaymodeValidation.Start is not ready.");
                        start.Invoke(null, null); result.detail = "Pino Play Mode validation started; inspect the playtest evidence report for completion.";
                        break;
                    default: throw new InvalidOperationException("Supported Pino commands: build, validate, capturegallery, playtest, locomotion.");
                }
                result.success = true;
                if (string.IsNullOrEmpty(result.detail)) result.detail = "Pino " + request.command + " completed.";
            }
            catch (Exception error)
            {
                if (error is TargetInvocationException wrapper && wrapper.InnerException != null) error = wrapper.InnerException;
                result.success = false; result.detail = error.ToString(); Debug.LogException(error);
            }
            finally
            {
                result.completedUtc = DateTime.UtcNow.ToString("O");
                File.WriteAllText(ResultPath, JsonUtility.ToJson(result, true));
                if (File.Exists(processing)) File.Delete(processing);
                busy = false;
            }
        }
    }
}
#endif
