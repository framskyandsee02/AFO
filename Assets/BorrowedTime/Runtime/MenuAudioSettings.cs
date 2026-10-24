using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BorrowedTime.Menu
{
    public enum MenuAudioChannel { Master, Music, Effects }
    public static class MenuAudioSettings
    {
        public static event Action Changed;
        static readonly int[] volumes = { 80, 80, 100 };
        public static bool Muted { get; private set; }
        public static string Key(MenuAudioChannel channel) => "BorrowedTime.Audio." + channel;
        public static int Get(MenuAudioChannel channel) => volumes[(int)channel];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Changed = null; SceneManager.sceneLoaded -= RouteScene; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize() { Reload(); SceneManager.sceneLoaded += RouteScene; }
        public static void Reload()
        {
            for (int i = 0; i < 3; i++) volumes[i] = Mathf.Clamp(PlayerPrefs.GetInt(Key((MenuAudioChannel)i), i == 2 ? 100 : 80), 0, 100);
            Muted = PlayerPrefs.GetInt("BorrowedTime.Audio.Muted", 0) != 0; Apply();
        }
        public static void Set(MenuAudioChannel channel, int value)
        {
            volumes[(int)channel] = Mathf.Clamp(value, 0, 100);
            PlayerPrefs.SetInt(Key(channel), Get(channel)); Apply();
        }
        public static void SetMuted(bool value) { Muted = value; PlayerPrefs.SetInt("BorrowedTime.Audio.Muted", value ? 1 : 0); Apply(); }
        static void Apply() { AudioListener.volume = Muted ? 0 : Get(MenuAudioChannel.Master) / 100f; Changed?.Invoke(); }
        static void RouteScene(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects()) foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
                if (!source.GetComponent<MenuAudioBus>()) source.gameObject.AddComponent<MenuAudioBus>();
        }
    }
}
