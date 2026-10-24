using UnityEngine;

namespace BorrowedTime.Menu
{
    [RequireComponent(typeof(AudioSource)), DisallowMultipleComponent]
    public sealed class MenuAudioBus : MonoBehaviour
    {
        public MenuAudioChannel channel = MenuAudioChannel.Effects;
        AudioSource source; float baseVolume, applied;
        void OnEnable() { source = GetComponent<AudioSource>(); baseVolume = source.volume; MenuAudioSettings.Changed += Apply; Apply(); }
        void LateUpdate() { if (source && !Mathf.Approximately(applied, source.volume)) { baseVolume = source.volume; Apply(); } }
        void Apply() { if (source) { applied = baseVolume * (channel == MenuAudioChannel.Master ? 1 : MenuAudioSettings.Get(channel) / 100f); source.volume = applied; } }
        void OnDisable() { MenuAudioSettings.Changed -= Apply; if (source) source.volume = baseVolume; }
    }
}
