using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Playfield;
using Quaver.Shared.Screens.Selection.UI;
using Quaver.Shared.Screens.Selection.UI.Preview;
using Wobble;
using Wobble.Bindables;
using Wobble.Graphics;
using Wobble.Window;

namespace Quaver.Shared.Screens.Setup
{
    internal sealed class SetupScrollSpeedPreview : SelectMapPreviewContainer
    {
        private static readonly RasterizerState ScissorRasterizerState = new RasterizerState { ScissorTestEnable = true };

        internal SetupScrollSpeedPreview(Map map, int height) : base(new Bindable<bool>(false),
            new Bindable<SelectContainerPanel>(SelectContainerPanel.MapPreview), height, previewMap: map)
        {
            HasSeekBar = false;
            DelayTime = 0;
            Alpha = 1;
        }

        protected override bool ShowHitBubbles { get; } = false;

        protected override bool ShouldShowTestPlayPrompt { get; } = false;

        public override void Update(GameTime gameTime)
        {
            if (LoadedGameplayScreen != null)
                LoadedGameplayScreen.IsPaused = false;

            base.Update(gameTime);

            if (LoadedGameplayScreen?.Ruleset?.Playfield is not GameplayPlayfieldKeys playfield)
                return;

            var stage = playfield.Stage;
            playfield.Container.Y = 0;
            playfield.PlayfieldMask.Visible = false;
            stage.StageLeft.Visible = false;
            stage.StageRight.Visible = false;
            stage.BgMask.Visible = true;
            stage.HealthBar.Visible = false;
            stage.HitBubbles.Visible = false;
            stage.ComboDisplay.Visible = false;
            stage.HitError.Visible = false;
            stage.JudgementHitBursts.ForEach(x => x.Visible = false);
            stage.HitLightingObjects.ForEach(x => x.Visible = false);
        }

        public override void Draw(GameTime gameTime)
        {
            var graphicsDevice = GameBase.Game.GraphicsDevice;
            var previousScissorRectangle = graphicsDevice.ScissorRectangle;
            var previousDefaultRasterizerState = GameBase.DefaultSpriteBatchOptions.RasterizerState;
            var changedSpriteBatchOptions = new List<(Drawable Drawable, RasterizerState RasterizerState)>();

            GameBase.Game.TryEndBatch();

            try
            {
                graphicsDevice.ScissorRectangle = ToBackBufferRectangle(ScreenRectangle);
                GameBase.DefaultSpriteBatchOptions.RasterizerState = ScissorRasterizerState;
                EnableScissorOnSpriteBatchOptions(this, changedSpriteBatchOptions);

                base.Draw(gameTime);
                GameBase.Game.TryEndBatch();
            }
            finally
            {
                GameBase.Game.TryEndBatch();

                foreach (var item in changedSpriteBatchOptions)
                    item.Drawable.SpriteBatchOptions.RasterizerState = item.RasterizerState;

                GameBase.DefaultSpriteBatchOptions.RasterizerState = previousDefaultRasterizerState;
                graphicsDevice.ScissorRectangle = previousScissorRectangle;
            }
        }

        private static Rectangle ToBackBufferRectangle(RectangleF rectangle)
        {
            var graphics = GameBase.Game.Graphics;
            var widthScale = graphics.PreferredBackBufferWidth / WindowManager.Width;
            var heightScale = graphics.PreferredBackBufferHeight / WindowManager.Height;

            return new Rectangle((int)(rectangle.X * widthScale), (int)(rectangle.Y * heightScale),
                (int)(rectangle.Width * widthScale), (int)(rectangle.Height * heightScale));
        }

        private static void EnableScissorOnSpriteBatchOptions(Drawable drawable,
            ICollection<(Drawable Drawable, RasterizerState RasterizerState)> changedSpriteBatchOptions)
        {
            if (drawable.SpriteBatchOptions != null)
            {
                changedSpriteBatchOptions.Add((drawable, drawable.SpriteBatchOptions.RasterizerState));
                drawable.SpriteBatchOptions.RasterizerState = ScissorRasterizerState;
            }

            foreach (var child in drawable.Children)
                EnableScissorOnSpriteBatchOptions(child, changedSpriteBatchOptions);
        }
    }
}
