using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Quaver.API.Enums;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics;
using Quaver.Shared.Graphics.Buttons;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Quaver.Shared.Localization;
using Quaver.Shared.Screens.Options.Items.Custom;
using Quaver.Shared.Screens.Selection.UI;
using Quaver.Shared.Screens.Selection.UI.Preview;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Graphics.UI.Form;
using Wobble.Managers;
using Wobble.Screens;
using ColorHelper = Quaver.Shared.Helpers.ColorHelper;

namespace Quaver.Shared.Screens.Setup
{
    public sealed class SetupScreenView : ScreenView
    {
        private const int PanelWidth = 1500;
        private const int PanelHeight = 820;

        private SetupScreen SetupScreen { get; }

        private Container Panel { get; }

        private Container StepContainer { get; set; }

        private SpriteTextPlus Title { get; }

        private SpriteTextPlus Progress { get; }

        private RoundedButton BackButton { get; }

        private RoundedButton NextButton { get; }

        private RoundedButton SkipButton { get; }

        private bool ApplyScrollSpeedToAllModes { get; set; }

        private RoundedButton ScrollSpeed4KOnlyButton { get; set; }

        private RoundedButton ScrollSpeedAllModesButton { get; set; }

        public SetupScreenView(SetupScreen screen) : base(screen)
        {
            SetupScreen = screen;

            Panel = new Container
            {
                Parent = Container,
                Alignment = Alignment.MidCenter,
                Size = new ScalableVector2(PanelWidth, PanelHeight)
            };

            new Sprite
            {
                Parent = Panel,
                Size = Panel.Size,
                Tint = ColorHelper.HexToColor("#17191f")
            };

            Title = CreateText(Panel, "", 42, Alignment.TopCenter, 0, 42);
            Progress = CreateText(Panel, "", 21, Alignment.TopCenter, 0, 102, Colors.MainAccent);

            BackButton = CreateButton(Panel, "", Alignment.BotLeft, 48, -42, 180);
            BackButton.Clicked += (sender, args) => SetupScreen.GoBack();

            NextButton = CreateButton(Panel, "", Alignment.BotRight, -48, -42, 220);
            NextButton.Clicked += (sender, args) => SetupScreen.GoNext(ApplyScrollSpeedToAllModes);

            SkipButton = CreateButton(Panel, "", Alignment.BotCenter, 0, -42, 150);
            SkipButton.Clicked += (sender, args) => SetupScreen.Skip();

            ShowCurrentStep();
        }

        internal void ShowCurrentStep()
        {
            StepContainer?.Destroy();
            StepContainer = new Container
            {
                Parent = Panel,
                Alignment = Alignment.TopCenter,
                Position = new ScalableVector2(0, 150),
                Size = new ScalableVector2(PanelWidth - 160, 550)
            };

            Title.Text = Localize(SetupScreen.CurrentStep.TitleKey);
            Progress.Text = LocalizationManager.Get("Screen_Setup_Progress", SetupScreen.CurrentStepIndex + 1, SetupScreen.Steps.Count);
            BackButton.Visible = SetupScreen.CurrentStepIndex != 0;
            BackButton.SetLabel(FontManager.GetWobbleFont(Fonts.InterBold), Localize("Screen_Setup_Back"), 20);
            NextButton.SetLabel(FontManager.GetWobbleFont(Fonts.InterBold),
                Localize(SetupScreen.CurrentStepIndex + 1 == SetupScreen.Steps.Count ? "Screen_Setup_Finish" : "Screen_Setup_Next"), 22);
            SkipButton.SetLabel(FontManager.GetWobbleFont(Fonts.InterBold), Localize("Screen_Setup_Skip"), 20);

            switch (SetupScreen.CurrentStep.Step)
            {
                case SetupStep.Preferences:
                    CreatePreferencesStep();
                    break;
                case SetupStep.ScrollSpeed:
                    CreateScrollSpeedStep();
                    break;
                case SetupStep.Keybinds:
                    CreateKeybindsStep();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CreatePreferencesStep()
        {
            CreateRowLabel(Localize("Screen_Setup_Language"), 55);

            var languageOptions = QuaverLocalization.AvailableLanguages
                .Select(language => LocalizationManager.Get(language.DisplayNameKey))
                .ToList();
            var languageDropdown = new Dropdown(languageOptions, new ScalableVector2(360, 40), 22, Colors.MainAccent,
                GetSelectedLanguageIndex())
            {
                Parent = StepContainer,
                Alignment = Alignment.TopRight,
                Position = new ScalableVector2(-35, 40)
            };
            languageDropdown.ItemSelected += (sender, args) =>
            {
                ConfigManager.Language.Value = QuaverLocalization.AvailableLanguages[args.Index].CultureName;
                Container.ScheduleUpdate(ShowCurrentStep);
            };

            CreateRowLabel(Localize("Screen_Setup_FrameLimiter"), 155);
            var limiterOptions = GetFpsLimiterOptions();
            var selectedLimiter = limiterOptions.IndexOf(ConfigManager.FpsLimiterType.Value.ToString());
            var limiterDropdown = new Dropdown(limiterOptions, new ScalableVector2(360, 40), 22, Colors.MainAccent,
                Math.Max(selectedLimiter, 0))
            {
                Parent = StepContainer,
                Alignment = Alignment.TopRight,
                Position = new ScalableVector2(-35, 140)
            };
            limiterDropdown.ItemSelected += (sender, args) =>
                ConfigManager.FpsLimiterType.Value = (FpsLimitType)Enum.Parse(typeof(FpsLimitType), args.Text);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                CreatePlatformToggle(Localize("Screen_Setup_PreferWayland"), 255, ConfigManager.PreferWayland,
                    FpsLimitType.WaylandVsync);
                CreateText(StepContainer, Localize("Screen_Setup_WaylandRestart"), 18, Alignment.TopLeft, 35, 305, Color.LightGray);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                CreatePlatformToggle(Localize("Screen_Setup_PreferMacOsInput"), 255, ConfigManager.PreferCocoaEventLoop,
                    FpsLimitType.Limited);
        }

        private void CreateScrollSpeedStep()
        {
            CreateText(StepContainer, Localize("Screen_Setup_ScrollSpeedDescription"), 22, Alignment.TopCenter, 0, 15,
                Color.LightGray);

            var previewMap = GetPreviewMap();

            if (previewMap != null)
            {
                SetupScreen.SelectPreviewMap(previewMap);

                var preview = new SetupScrollSpeedPreview(previewMap, 410)
                {
                    Parent = StepContainer,
                    Alignment = Alignment.TopLeft,
                    Position = new ScalableVector2(35, 75),
                    Alpha = 0
                };
            }
            else
                CreateText(StepContainer, Localize("Screen_Setup_PreviewUnavailable"), 20, Alignment.TopLeft, 80, 240, Color.LightGray);

            var speed = ConfigManager.ScrollSpeeds[GameMode.Keys4];
            var speedValue = CreateText(StepContainer, FormatScrollSpeed(speed.Value), 34, Alignment.TopRight, -90, 180, Colors.MainAccent);
            var slider = new Slider(speed, new Vector2(460, 5), UserInterface.VolumeSliderProgressBall)
            {
                Parent = StepContainer,
                Alignment = Alignment.TopRight,
                Position = new ScalableVector2(-90, 245),
                Tint = ColorHelper.HexToColor("#5b5b5b")
            };
            slider.ActiveColor.Tint = Colors.MainAccent;
            speed.ValueChanged += (sender, args) => speedValue.Text = FormatScrollSpeed(args.Value);
            speed.TriggerChangeEvent();

            CreateText(StepContainer, Localize("Screen_Setup_ApplyScrollSpeed"), 22, Alignment.TopRight, -90, 325);
            ScrollSpeed4KOnlyButton = CreateButton(StepContainer, Localize("Screen_Setup_4KOnly"), Alignment.TopRight, -330, 370, 200);
            ScrollSpeedAllModesButton = CreateButton(StepContainer, Localize("Screen_Setup_AllModes"), Alignment.TopRight, -90, 370, 220);
            ScrollSpeed4KOnlyButton.Clicked += (sender, args) => SetScrollSpeedScope(false);
            ScrollSpeedAllModesButton.Clicked += (sender, args) => SetScrollSpeedScope(true);
            SetScrollSpeedScope(false);
        }

        private void CreateKeybindsStep()
        {
            CreateText(StepContainer, Localize("Screen_Setup_KeybindsDescription"), 22, Alignment.TopCenter, 0, 15,
                Color.LightGray);

            var bounds = new RectangleF(0, 0, 720, 54);
            var keys4 = new OptionsItemKeybindMultiple(bounds, Localize("Screen_Setup_4KKeybinds"),
                ConfigManager.KeyLayouts[GameMode.Keys4])
            {
                Parent = StepContainer,
                Alignment = Alignment.TopCenter,
                Position = new ScalableVector2(0, 160)
            };
            var keys7 = new OptionsItemKeybindMultiple(bounds, Localize("Screen_Setup_7KKeybinds"),
                ConfigManager.KeyLayouts[GameMode.Keys7])
            {
                Parent = StepContainer,
                Alignment = Alignment.TopCenter,
                Position = new ScalableVector2(0, 290)
            };
        }

        private void CreatePlatformToggle(string label, int y, Bindable<bool> value, FpsLimitType limiterWhenEnabled)
        {
            if (value.Value && ConfigManager.FpsLimiterType.Value == FpsLimitType.Unlimited)
                ConfigManager.FpsLimiterType.Value = limiterWhenEnabled;

            CreateRowLabel(label, y);
            var button = CreateButton(StepContainer, "", Alignment.TopRight, -35, y - 15, 220);

            void RefreshButton()
            {
                button.SetLabel(FontManager.GetWobbleFont(Fonts.InterBold),
                    value.Value ? Localize("Screen_Setup_Enabled") : Localize("Screen_Setup_Disabled"), 20);
                button.Tint = value.Value ? Colors.MainAccent : ColorHelper.HexToColor("#3f4654");
            }

            button.Clicked += (sender, args) =>
            {
                value.Value = !value.Value;

                if (value.Value && ConfigManager.FpsLimiterType.Value == FpsLimitType.Unlimited)
                    ConfigManager.FpsLimiterType.Value = limiterWhenEnabled;

                RefreshButton();
            };

            RefreshButton();
        }

        private void SetScrollSpeedScope(bool applyToAllModes)
        {
            ApplyScrollSpeedToAllModes = applyToAllModes;
            ScrollSpeed4KOnlyButton.Tint = !applyToAllModes ? Colors.MainAccent : ColorHelper.HexToColor("#3f4654");
            ScrollSpeedAllModesButton.Tint = applyToAllModes ? Colors.MainAccent : ColorHelper.HexToColor("#3f4654");
        }

        private void CreateRowLabel(string text, int y) =>
            CreateText(StepContainer, text, 25, Alignment.TopLeft, 35, y, Color.White);

        private static SpriteTextPlus CreateText(Container parent, string text, int size, Alignment alignment, float x, float y,
            Color? tint = null) => new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterBold), text, size)
        {
            Parent = parent,
            Alignment = alignment,
            Position = new ScalableVector2(x, y),
            Tint = tint ?? Color.White
        };

        private static RoundedButton CreateButton(Container parent, string text, Alignment alignment, float x, float y, float width) =>
            new RoundedButton
            {
                Parent = parent,
                Alignment = alignment,
                Position = new ScalableVector2(x, y),
                Size = new ScalableVector2(width, 46),
                Tint = ColorHelper.HexToColor("#3f4654"),
                CornerRadius = 8,
                PerformHoverFade = true,
                WidthMode = ButtonSizeMode.Fixed,
                HeightMode = ButtonSizeMode.Fixed
            }.WithLabel(text);

        private static List<string> GetFpsLimiterOptions() => Enum.GetValues(typeof(FpsLimitType))
            .Cast<FpsLimitType>()
            .Where(type => type != FpsLimitType.WaylandVsync || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            .Select(type => type.ToString())
            .ToList();

        private static int GetSelectedLanguageIndex()
        {
            var cultureName = QuaverLocalization.GetLanguage(ConfigManager.Language.Value).CultureName;
            return QuaverLocalization.AvailableLanguages
                .Select((language, index) => new { language, index })
                .FirstOrDefault(x => x.language.CultureName == cultureName)?.index ?? 0;
        }

        private static string FormatScrollSpeed(int speed) => $"{speed / 10f:0.0}";

        private static Map GetPreviewMap() => new VisualTestMap
        {
            Directory = "Quaver.Resources/Maps/Offset",
            Path = "offset.qua",
            Md5Checksum = "setup-scroll-speed-preview",
            Mode = GameMode.Keys4
        };

        private static string Localize(string key) => LocalizationManager.Get(key);

        public override void Update(GameTime gameTime) => Container.Update(gameTime);

        public override void Draw(GameTime gameTime)
        {
            GameBase.Game.GraphicsDevice.Clear(Color.Black);
            Container.Draw(gameTime);
        }

        public override void Destroy() => Container.Destroy();
    }

    internal static class RoundedButtonExtensions
    {
        internal static RoundedButton WithLabel(this RoundedButton button, string text)
        {
            button.SetLabel(FontManager.GetWobbleFont(Fonts.InterBold), text, 20);
            return button;
        }
    }
}
