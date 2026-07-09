using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Quaver.Shared.Assets;
using Quaver.Shared.Graphics.Buttons;
using Quaver.Shared.Screens.Setup;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.UI.Dialogs;
using Wobble.Managers;
using ColorHelper = Quaver.Shared.Helpers.ColorHelper;

namespace Quaver.Shared.Screens.Options.Items.Custom
{
    public class OptionsItemRunSetup : OptionsItem
    {
        private RoundedButton Button { get; }

        public OptionsItemRunSetup(RectangleF containerRect, string name) : base(containerRect, name)
        {
            Button = new RoundedButton
            {
                Parent = this,
                Alignment = Alignment.MidRight,
                X = -Name.X,
                Size = new ScalableVector2(190, 36),
                Tint = ColorHelper.HexToColor("#0FBAE5"),
                CornerRadius = 8
            };
            Button.SetLabel(FontManager.GetWobbleFont(Fonts.InterBold), "START SETUP", 18, Color.White);

            Button.Clicked += (sender, args) =>
            {
                var game = GameBase.Game as QuaverGame;

                if (game?.CurrentScreen == null || game.CurrentScreen.Exiting)
                    return;

                if (DialogManager.Dialogs.Count != 0)
                    DialogManager.Dismiss(DialogManager.Dialogs[^1]);

                game.CurrentScreen.Exit(() => new SetupScreen());
            };
        }
    }
}
