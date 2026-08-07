using System;
using System.IO;
using System.Linq;
using System.Net;
using Quaver.Server.Client.Helpers;
using Quaver.Server.Client.Objects.Twitch;
using Quaver.Shared.Audio;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Database.Playlists;
using Quaver.Shared.Graphics.Notifications;
using Quaver.Shared.Graphics.Overlays.Hub;
using Quaver.Shared.Online;
using Quaver.Shared.Online.API.Maps;
using Quaver.Shared.Online.API.Mapsets;
using Quaver.Shared.Screens;
using Quaver.Shared.Screens.Download;
using Quaver.Shared.Screens.Edit;
using Quaver.Shared.Screens.Importing;
using Wobble;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Logging;

namespace Quaver.Shared.IPC
{
    public static class QuaverIpcHandler
    {
        private const string protocolUriStarter = "quaver://";
        private static readonly string[] importableFileExtensions =
        {
            ".qp",
            ".osz",
            ".sm",
            ".mcz",
            ".mc",
            ".qr",
            ".qs",
            ".mp3",
            ".ogg",
            ".db",
            ".zip",
            ".qpl"
        };

        /// <summary>
        ///     Handles messages from IPC
        /// </summary>
        /// <param name="message"></param>
        public static void HandleMessage(string message)
        {
            Logger.Important($"Received IPC Message: {message}", LogType.Runtime);

            if (message.StartsWith(protocolUriStarter))
                HandleProtocolMessage(message.Substring(protocolUriStarter.Length));
            else if (IsImportableFileMessage(message))
            {
                // Quaver was launched with a file path, try to import it.
                MapsetImporter.ImportFile(message);
            }
            else
                Logger.Important($"Ignoring unsupported IPC Message: {message}", LogType.Runtime);
        }

        /// <summary>
        ///     Checks if a raw IPC message is an actual file Quaver knows how to import.
        /// </summary>
        /// <param name="message"></param>
        private static bool IsImportableFileMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            if (!File.Exists(message))
                return false;

            return importableFileExtensions.Contains(Path.GetExtension(message), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Handles a quaver:// message.
        ///     <param name="message">the IPC message with quaver:// stripped</param>
        /// </summary>
        public static void HandleProtocolMessage(string message)
        {
            if (message.StartsWith("editor/"))
                HandleEditorNoteHighlighting(message);
            else if (message.StartsWith("map/"))
                HandleMapSelection(message);
            else if (message.StartsWith("mapset/"))
                HandleMapsetSelection(message);
            else if (message.StartsWith("playlist/"))
                HandlePlaylistImport(message);
#if DEBUG
            else if (message.StartsWith("debug/screen/", StringComparison.OrdinalIgnoreCase))
                HandleDebugScreenSwitch(message.Substring("debug/screen/".Length));
            else if (message.StartsWith("debug/input/", StringComparison.OrdinalIgnoreCase))
                QuaverDebugInputController.Handle(message.Substring("debug/input/".Length));
#endif
        }

#if DEBUG
        private static readonly object DebugScreenSwitchLock = new();
        private static string PendingDebugScreenSwitch { get; set; }

        /// <summary>
        ///     Switches to a context-free screen for local UI testing.
        ///     This command is intentionally compiled only into DEBUG builds.
        /// </summary>
        /// <param name="screenName">The screen name, for example <c>menu</c> or <c>selection</c>.</param>
        private static void HandleDebugScreenSwitch(string screenName)
        {
            if (GameBase.Game is not QuaverGame game)
            {
                QueueDebugScreenSwitch(screenName,
                    "Game is not initialized yet; DEBUG IPC screen switch queued.");
                return;
            }

            if (game.CurrentScreen == null ||
                game.CurrentScreen.Type == QuaverScreenType.Initialization ||
                game.CurrentScreen.Exiting)
            {
                QueueDebugScreenSwitch(screenName,
                    "The initial screen transition is still in progress; DEBUG IPC screen switch queued.");
                return;
            }

            ScheduleDebugScreenSwitch(game, screenName);
        }

        /// <summary>
        ///     Tries to apply a DEBUG screen switch that arrived before the game finished its initial screen
        ///     transition. This is called by the screen manager after a screen has been installed on the game loop.
        /// </summary>
        public static void TryFlushPendingDebugScreenSwitch()
        {
            if (GameBase.Game is not QuaverGame game || game.CurrentScreen == null ||
                game.CurrentScreen.Type == QuaverScreenType.Initialization || game.CurrentScreen.Exiting)
                return;

            string screenName;
            lock (DebugScreenSwitchLock)
            {
                screenName = PendingDebugScreenSwitch;
                PendingDebugScreenSwitch = null;
            }

            if (screenName != null)
                ScheduleDebugScreenSwitch(game, screenName);
        }

        private static void QueueDebugScreenSwitch(string screenName, string logMessage)
        {
            lock (DebugScreenSwitchLock)
                PendingDebugScreenSwitch = screenName;

            Logger.Important(logMessage, LogType.Runtime);
        }

        private static void ScheduleDebugScreenSwitch(QuaverGame game, string screenName)
        {
            var previousScreen = game.CurrentScreen?.Type ?? QuaverScreenType.Menu;

            if (!TryCreateDebugScreen(screenName, previousScreen, out var screenFactory))
            {
                Logger.Warning(
                    $"Unknown or unsupported DEBUG screen `{screenName}`. Supported screens: " +
                    "menu, selection, downloading, lobby, music, theater, importing, multiplayer.",
                    LogType.Runtime);
                return;
            }

            // IPC requests are received away from the game loop. Queue the transition so screen
            // lifecycle and UI/GPU work run on the game thread through the normal Exit path.
            game.ScheduleRenderTargetDraw(() =>
            {
                if (game.CurrentScreen == null)
                {
                    Logger.Warning("Cannot switch screens through DEBUG IPC because there is no current screen.",
                        LogType.Runtime);
                    return;
                }

                if (game.CurrentScreen.Exiting)
                {
                    Logger.Warning("Ignoring DEBUG IPC screen switch because the current screen is already exiting.",
                        LogType.Runtime);
                    return;
                }

                game.CurrentScreen.Exit(screenFactory);
            });
        }

        private static bool TryCreateDebugScreen(string screenName, QuaverScreenType previousScreen,
            out Func<QuaverScreen> screenFactory)
        {
            switch (screenName.Trim().Trim('/').ToLowerInvariant())
            {
                case "menu":
                case "main":
                case "main-menu":
                    screenFactory = QuaverScreenFactory.CreateMainMenu;
                    return true;
                case "select":
                case "selection":
                    screenFactory = () => QuaverScreenFactory.CreateSelection();
                    return true;
                case "download":
                case "downloading":
                    screenFactory = () => QuaverScreenFactory.CreateDownloading(previousScreen);
                    return true;
                case "lobby":
                case "multiplayer-lobby":
                    screenFactory = QuaverScreenFactory.CreateMultiplayerLobby;
                    return true;
                case "music":
                case "music-player":
                    screenFactory = QuaverScreenFactory.CreateMusicPlayer;
                    return true;
                case "theater":
                case "theatre":
                    screenFactory = QuaverScreenFactory.CreateTheater;
                    return true;
                case "import":
                case "importing":
                    screenFactory = () => QuaverScreenFactory.CreateImporting();
                    return true;
                case "multiplayer":
                    if (OnlineManager.CurrentGame == null)
                    {
                        screenFactory = null!;
                        return false;
                    }

                    screenFactory = QuaverScreenFactory.CreateMultiplayerGame;
                    return true;
                default:
                    screenFactory = null!;
                    return false;
            }
        }
#endif

        /// <summary>
        ///     Flushes a DEBUG screen switch once the initial screen is ready. This is a no-op in non-debug builds.
        /// </summary>
        public static void TryFlushPendingDebugScreenSwitchForBuild()
        {
#if DEBUG
            TryFlushPendingDebugScreenSwitch();
#endif
        }

        /// <summary>
        ///     Highlights notes within the editor
        /// </summary>
        /// <param name="message"></param>
        private static void HandleEditorNoteHighlighting(string message)
        {
            message = message.Replace("editor/", "");
            message = message.Replace("%7C", "|");

            var game = GameBase.Game as QuaverGame;

            if (game?.CurrentScreen is EditScreen screen)
                screen.GoToObjects(message);
            else
                NotificationManager.Show(NotificationLevel.Warning, "You must be in the editor to use this function!");
        }

        /// <summary>
        ///     Selects a map if already imported or downloads it from the server.
        /// </summary>
        /// <param name="message"></param>
        private static void HandleMapSelection(string message)
        {
            message = message.Replace("map/", "");

            if (!int.TryParse(message, out var id))
            {
                NotificationManager.Show(NotificationLevel.Error, $"The provided map id was not a valid number.");
                return;
            }

            var map = MapManager.FindMapFromOnlineId(id);

            if (SelectMapIfImported(map))
                return;

            if (!IsConnected())
                return;

            try
            {
                // Find mapset id & song name.
                var response = new APIRequestMapInformation(id).ExecuteRequest();

                if (response.Status == (int)HttpStatusCode.NotFound)
                {
                    NotificationManager.Show(NotificationLevel.Error, $"That map does not exist on the server.");
                    return;
                }

                if (response.Status != (int)HttpStatusCode.OK)
                    throw new Exception($"Failed map information `{id}` fetch with response: {response.Status}");

                DownloadMapAndImport(response.Map.MapsetId, response.Map.Artist, response.Map.Title, true);
            }
            catch (Exception e)
            {
                NotificationManager.Show(NotificationLevel.Error, $"An error occurred while fetching map information.");
                Logger.Error(e, LogType.Network);
            }
        }

        /// <summary>
        ///     Selects a mapset if already imported or downloads it from the server.
        /// </summary>
        /// <param name="message"></param>
        private static void HandleMapsetSelection(string message)
        {
            message = message.Replace("mapset/", "");

            if (!int.TryParse(message, out var id))
            {
                NotificationManager.Show(NotificationLevel.Error, $"The provided mapset id was not a valid number.");
                return;
            }

            var mapset = MapManager.Mapsets.Find(x => x.Maps.First().MapSetId == id);

            if (SelectMapIfImported(mapset?.Maps.First()))
                return;

            if (!IsConnected())
                return;

            try
            {
                // Find mapset id & song name.
                var response = new APIRequestMapsetInformation(id).ExecuteRequest();

                if (response.Status == (int)HttpStatusCode.NotFound)
                {
                    NotificationManager.Show(NotificationLevel.Error, $"That mapset does not exist on the server.");
                    return;
                }

                if (response.Status != (int)HttpStatusCode.OK)
                    throw new Exception($"Failed mapset information `{id}` fetch with response: {response.Status}");

                DownloadMapAndImport(id, response.Mapset.Artist, response.Mapset.Title, false);
            }
            catch (Exception e)
            {
                NotificationManager.Show(NotificationLevel.Error, $"An error occurred while fetching mapset information.");
                Logger.Error(e, LogType.Network);
            }
        }

        /// <summary>
        ///     Imports an online playlist
        /// </summary>
        /// <param name="message"></param>
        private static void HandlePlaylistImport(string message)
        {
            message = message.Replace("playlist/", "");

            if (!int.TryParse(message, out var id))
            {
                NotificationManager.Show(NotificationLevel.Error, $"The provided playlist id was not a valid number.");
                return;
            }

            PlaylistManager.ImportPlaylist(id);
        }

        /// <summary>
        ///     Checks if the user is connected to the server & alerts them if they're not.
        /// </summary>
        /// <returns></returns>
        private static bool IsConnected()
        {
            if (!OnlineManager.Connected)
            {
                NotificationManager.Show(NotificationLevel.Warning, $"You must be logged in to download maps!");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Returns if the user is allowed to select a map/import on specific screens
        /// </summary>
        /// <returns></returns>
        private static bool IsSelectionAllowedOnScreen()
        {
            var game = (QuaverGame)GameBase.Game;

            switch (game.CurrentScreen.Type)
            {
                case QuaverScreenType.Select:
                case QuaverScreenType.Menu:
                case QuaverScreenType.Lobby:
                case QuaverScreenType.Download:
                case QuaverScreenType.Music:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Selects a map if it is imported. Returns true if it was successfully selected.
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        private static bool SelectMapIfImported(Map map)
        {
            if (map == null)
                return false;

            var game = (QuaverGame)GameBase.Game;

            if (!IsSelectionAllowedOnScreen())
            {
                NotificationManager.Show(NotificationLevel.Warning, $"Please finish what you're doing before selecting this map!");
                return false;
            }

            if (game.CurrentScreen.Type == QuaverScreenType.Select)
                MapManager.PlaySongRequest(new SongRequest(), map);
            else
            {
                MapManager.Selected.Value = map;
                AudioEngine.LoadCurrentTrack();
            }

            return true;
        }

        /// <summary>
        ///     Downloads a map from the server.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="artist"></param>
        /// <param name="title"></param>
        /// <param name="isMap"></param>
        private static void DownloadMapAndImport(int id, string artist, string title, bool isMap)
        {
            var game = (QuaverGame)GameBase.Game;

            var dl = MapsetDownloadManager.Download(id, artist, title);
            MapsetDownloadManager.OpenOnlineHub();

            // Automatically import if the user is still in song select after completion.
            dl.Status.ValueChanged += (o, e) =>
            {
                if (!IsSelectionAllowedOnScreen() || e.Value.Status != FileDownloaderStatus.Complete)
                    return;

                var dialog = DialogManager.Dialogs.Find(x => x is OnlineHubDialog) as OnlineHubDialog;
                dialog?.Close();

                if (isMap)
                    game.CurrentScreen.Exit(() => QuaverScreenFactory.CreateImporting(null, true, false, id));
                else
                    game.CurrentScreen.Exit(() => QuaverScreenFactory.CreateImporting(null, true, false));
            };
        }
    }
}
