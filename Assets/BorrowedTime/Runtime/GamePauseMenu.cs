using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace BorrowedTime.Menu
{
    [DefaultExecutionOrder(-150)]
    public sealed class GamePauseMenu : MonoBehaviour
    {
        BTPlayer player;
        GameObject host;
        BorrowedMainMenu menu;
        float previousTimeScale;
        bool previousPaused, previousCursorVisible;
        CursorLockMode previousCursorLock;
        int escapeHandledFrame=-1;
        public bool IsOpen { get; private set; }
        public BorrowedMainMenu Menu => menu;

        public static void Attach(BTPlayer player)
        {
            var pause = player.GetComponent<GamePauseMenu>();
            if (!pause) pause = player.gameObject.AddComponent<GamePauseMenu>();
            pause.player = player;
        }

        public static bool IsPausedFor(BTPlayer player)
        {
            if (!player) return false;
            var pause = player.GetComponent<GamePauseMenu>();
            return pause && pause.IsOpen;
        }

        public static bool BlocksInputFor(BTPlayer player)
        {
            if(!player)return false;
            var pause=player.GetComponent<GamePauseMenu>();
            return pause&&(pause.IsOpen||pause.escapeHandledFrame==Time.frameCount);
        }

        void Update()
        {
            if (!player || player.inputOverride != null || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) HandleEscape();
        }

        public void HandleEscape()
        {
            escapeHandledFrame=Time.frameCount;
            if (!IsOpen) Pause();
            else if (menu.SettingsVisible) menu.CloseSettings();
            else Resume();
        }

        public void Pause()
        {
            if (IsOpen || !player) return;
            if (!host)
            {
                var configuration = Resources.Load<GameMenuConfiguration>("BorrowedTimeMenuUi");
                if (!configuration || !configuration.visualTree || !configuration.panelSettings)
                { Debug.LogError("Pause menu configuration is missing. Run Tools/Borrowed Time/Install Pause Menu Configuration."); return; }
                host = new GameObject("Pause Menu"); host.SetActive(false); host.transform.SetParent(transform, false);
                var document = host.AddComponent<UIDocument>();
                document.panelSettings = configuration.panelSettings;
                document.visualTreeAsset = configuration.visualTree;
                document.sortingOrder = 100;
                menu = host.AddComponent<BorrowedMainMenu>();
                menu.pauseOwner = this; menu.showForegroundFoliage = false;
            }
            previousTimeScale = Time.timeScale; previousPaused = player.paused;
            previousCursorLock = Cursor.lockState; previousCursorVisible = Cursor.visible;
            IsOpen = true; player.paused = true; Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            host.SetActive(true);
            MenuAudioFeedback.UseDocument(host.GetComponent<UIDocument>());
        }

        public void Resume()
        {
            if (!IsOpen) return;
            escapeHandledFrame=Time.frameCount;
            IsOpen = false;
            Time.timeScale = previousTimeScale;
            if (player) player.paused = previousPaused;
            Cursor.lockState = previousCursorLock; Cursor.visible = previousCursorVisible;
            if (host) host.SetActive(false);
            MenuAudioFeedback.UseDocument(null);
        }

        public void QuitToMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded("BorrowedTimeMainMenu")) return;
            BTSaveGame.CancelPendingContinue();BTSaveGame.Save(player.game);
            Resume();
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            SceneManager.LoadScene("BorrowedTimeMainMenu");
        }

        void OnDisable() => Resume();
    }
}
