using System;
using System.Collections.Generic;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.Server.Client.Objects;
using Quaver.Shared.Audio;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Screens.Main;
using Wobble;
using Wobble.Audio.Tracks;

namespace Quaver.Shared.Screens.Setup
{
    public sealed class SetupScreen : QuaverScreen
    {
        internal static IReadOnlyList<SetupStepDefinition> Steps { get; } = new List<SetupStepDefinition>
        {
            new SetupStepDefinition(SetupStep.Preferences, "Screen_Setup_Preferences"),
            new SetupStepDefinition(SetupStep.ScrollSpeed, "Screen_Setup_ScrollSpeed"),
            new SetupStepDefinition(SetupStep.Keybinds, "Screen_Setup_Keybinds")
        };

        internal int CurrentStepIndex { get; private set; }

        internal SetupStepDefinition CurrentStep => Steps[CurrentStepIndex];

        private Map OriginalSelectedMap { get; set; }

        private bool HasPreviewMapSelection { get; set; }

        public override QuaverScreenType Type { get; } = QuaverScreenType.Setup;

        public SetupScreen(int currentStepIndex = 0)
        {
            CurrentStepIndex = Math.Clamp(currentStepIndex, 0, Steps.Count - 1);
            View = new SetupScreenView(this);
        }

        public override void OnFirstUpdate()
        {
            GameBase.Game.GlobalUserInterface.Cursor.Show(1);
            GameBase.Game.GlobalUserInterface.Cursor.Alpha = 1;

            base.OnFirstUpdate();
        }

        internal void GoBack()
        {
            if (CurrentStepIndex == 0)
                return;

            CurrentStepIndex--;
            ((SetupScreenView)View).ShowCurrentStep();
        }

        internal void GoNext(bool applyScrollSpeedToAllModes)
        {
            if (CurrentStep.Step == SetupStep.ScrollSpeed && applyScrollSpeedToAllModes)
            {
                var scrollSpeed = ConfigManager.ScrollSpeeds[GameMode.Keys4].Value;

                for (var keyCount = 1; keyCount <= ModeHelper.MaxKeyCount; keyCount++)
                    ConfigManager.ScrollSpeeds[ModeHelper.FromKeyCount(keyCount)].Value = scrollSpeed;
            }

            if (CurrentStepIndex + 1 < Steps.Count)
            {
                CurrentStepIndex++;
                ((SetupScreenView)View).ShowCurrentStep();
                return;
            }

            Finish();
        }

        internal void Skip() => Finish();

        internal void SelectPreviewMap(Map map)
        {
            if (!HasPreviewMapSelection)
            {
                OriginalSelectedMap = MapManager.Selected.Value;
                HasPreviewMapSelection = true;
            }

            if (MapManager.Selected.Value == map)
                return;

            MapManager.Selected.Value = map;

            if (map is VisualTestMap)
            {
                AudioEngine.Track?.Dispose();
                AudioEngine.Track = new AudioTrack(GameBase.Game.Resources.Get("Quaver.Resources/Maps/Offset/offset.mp3"), false, false);
                AudioEngine.Map = map;
            }
            else
                AudioEngine.LoadCurrentTrack();
        }

        private void RestoreSelectedMap()
        {
            if (!HasPreviewMapSelection)
                return;

            HasPreviewMapSelection = false;

            if (MapManager.Selected.Value != OriginalSelectedMap)
            {
                MapManager.Selected.Value = OriginalSelectedMap;

                if (OriginalSelectedMap != null)
                    AudioEngine.LoadCurrentTrack();
            }
        }

        private void Finish()
        {
            if (Exiting)
                return;

            RestoreSelectedMap();
            ConfigManager.SetupFinished.Value = true;
            ConfigManager.WriteConfigFileAsync().Wait();
            Exit(() => new MainMenuScreen());
        }

        public override void Destroy()
        {
            RestoreSelectedMap();
            base.Destroy();
        }

        public override UserClientStatus GetClientStatus() => null;
    }

    internal enum SetupStep
    {
        Preferences,
        ScrollSpeed,
        Keybinds
    }

    internal sealed class SetupStepDefinition
    {
        internal SetupStep Step { get; }

        internal string TitleKey { get; }

        internal SetupStepDefinition(SetupStep step, string titleKey)
        {
            Step = step;
            TitleKey = titleKey;
        }
    }
}
