/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Wobble;
using Wobble.Graphics.Sprites;

namespace Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Playfield
{
    internal enum PerspectivePlayfieldOrientation
    {
        Down,
        Up
    }

    /// <summary>
    ///     Draws its children to a render target, then projects that texture onto a subdivided
    ///     trapezoid to approximate perspective while keeping the gameplay renderer entirely 2D.
    /// </summary>
    internal sealed class PerspectivePlayfieldContainer : RenderTargetContainer
    {
        private BasicEffect Effect { get; }

        private VertexPositionColorTexture[] Vertices { get; set; } =
            Array.Empty<VertexPositionColorTexture>();

        private PerspectivePlayfieldOrientation Orientation { get; }

        private float FarEdgePosition { get; }

        private float FarWidthScale { get; }

        /// <summary>
        ///     Horizontal convergence point as a ratio of the current viewport width.
        /// </summary>
        public float ConvergencePointX { get; set; } = 0.5f;

        public PerspectivePlayfieldContainer(PerspectivePlayfieldOrientation orientation,
            float farEdgePosition, float farWidthScale)
        {
            Orientation = orientation;
            FarEdgePosition = MathHelper.Clamp(farEdgePosition, 0, 1);
            FarWidthScale = MathHelper.Clamp(farWidthScale, 0, 1);

            Effect = new BasicEffect(GameBase.Game.GraphicsDevice)
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
                LightingEnabled = false,
                World = Matrix.Identity,
                View = Matrix.Identity
            };
        }

        /// <inheritdoc />
        public override void DrawToSpriteBatch()
        {
            if (!Visible)
                return;

            _ = GameBase.Game.TryEndBatch();

            var graphicsDevice = GameBase.Game.GraphicsDevice;
            var previousBlendState = graphicsDevice.BlendState;
            var previousDepthStencilState = graphicsDevice.DepthStencilState;
            var previousRasterizerState = graphicsDevice.RasterizerState;
            var previousSamplerState = graphicsDevice.SamplerStates[0];

            try
            {
                graphicsDevice.BlendState = BlendState.NonPremultiplied;
                graphicsDevice.DepthStencilState = DepthStencilState.None;
                graphicsDevice.RasterizerState = RasterizerState.CullNone;
                graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

                var viewport = graphicsDevice.Viewport;
                var meshBandCount = Math.Max(1,
                    (int)Math.Ceiling(viewport.Height * (1 - FarEdgePosition)));
                EnsureVertexCapacity(meshBandCount);
                BuildVertices(viewport.Width, viewport.Height, meshBandCount);

                Effect.Texture = Image;
                Effect.Projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width,
                    viewport.Height, 0, -1, 1);

                foreach (var pass in Effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, Vertices, 0,
                        Vertices.Length - 2);
                }
            }
            finally
            {
                graphicsDevice.BlendState = previousBlendState;
                graphicsDevice.DepthStencilState = previousDepthStencilState;
                graphicsDevice.RasterizerState = previousRasterizerState;
                graphicsDevice.SamplerStates[0] = previousSamplerState;
            }
        }

        private void EnsureVertexCapacity(int meshBandCount)
        {
            var vertexCount = (meshBandCount + 1) * 2;

            if (Vertices.Length != vertexCount)
                Vertices = new VertexPositionColorTexture[vertexCount];
        }

        private void BuildVertices(float viewportWidth, float viewportHeight, int meshBandCount)
        {
            var convergencePoint = MathHelper.Clamp(ConvergencePointX, 0, 1) * viewportWidth;

            for (var i = 0; i <= meshBandCount; i++)
            {
                var textureY = (float)i / meshBandCount;
                float screenY;
                float widthScale;

                if (Orientation == PerspectivePlayfieldOrientation.Down)
                {
                    screenY = MathHelper.Lerp(FarEdgePosition, 1, textureY) * viewportHeight;
                    widthScale = MathHelper.Lerp(FarWidthScale, 1, textureY);
                }
                else
                {
                    screenY = MathHelper.Lerp(0, 1 - FarEdgePosition, textureY) * viewportHeight;
                    widthScale = MathHelper.Lerp(1, FarWidthScale, textureY);
                }

                var left = convergencePoint * (1 - widthScale);
                var right = convergencePoint + (viewportWidth - convergencePoint) * widthScale;
                var vertexIndex = i * 2;

                Vertices[vertexIndex] = new VertexPositionColorTexture(
                    new Vector3(left, screenY, 0), Color.White, new Vector2(0, textureY));
                Vertices[vertexIndex + 1] = new VertexPositionColorTexture(
                    new Vector3(right, screenY, 0), Color.White, new Vector2(1, textureY));
            }
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            if (!Effect.IsDisposed)
                Effect.Dispose();

            base.Destroy();
        }
    }
}
