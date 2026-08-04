/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoreLinq.Extensions;
using Quaver.API.Enums;
using Quaver.Server.Client.Objects.Multiplayer;
using Quaver.Shared.Assets;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Graphics;
using Quaver.Shared.Online;
using Quaver.Shared.Screens.Gameplay.Rulesets.Input;
using Quaver.Shared.Screens.Gameplay.Rulesets.Keys.HitObjects;
using Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Playfield.Health;
using Quaver.Shared.Screens.Gameplay.UI;
using Quaver.Shared.Screens.Gameplay.UI.Bubble;
using Quaver.Shared.Screens.Gameplay.UI.Health;
using Quaver.Shared.Screens.Gameplay.UI.Multiplayer;
using Quaver.Shared.Skinning;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Animations;
using Wobble.Graphics.Sprites;
using Wobble.Graphics.Sprites.Text;
using Wobble.Managers;
using Wobble.Window;

namespace Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Playfield
{
    public class GameplayPlayfieldKeysStage
    {
        /// <summary>
        ///     Reference to the gameplay screen itself.
        /// </summary>
        public GameplayScreen Screen { get; }

        /// <summary>
        ///     Reference to the parent playfield.
        /// </summary>
        public GameplayPlayfieldKeys Playfield { get; }

        /// <summary>
        ///     The container that holds all of the HitObjects.
        /// </summary>
        public Container[] HitObjectContainers { get; private set; }

        private Container[][] OmniHitObjectLaneRoots { get; set; }

        private Container[] OmniHitLaneRoots { get; set; }

        private Container[] OmniReceptorLaneRoots { get; set; }

        /// <summary>
        ///     Hidden parent for stage UI objects that are retained for scoring/replay callers in Omni.
        /// </summary>
        private Container OmniUiContainer { get; set; }

        private List<SpriteTextPlus> KeybindOverlays { get; } = new List<SpriteTextPlus>();

        /// <summary>
        ///     The Container that holds every Timing Line object
        /// </summary>
        public Container TimingLineContainer { get; private set; }

        /// <summary>
        ///     The container that holds all hits.
        /// </summary>
        public Container HitContainer { get; private set; }

        /// <summary>
        ///     The left side of the stage.
        /// </summary>
        public Sprite StageLeft { get; private set; }

        /// <summary>
        ///     The right side of the stage.
        /// </summary>
        public Sprite StageRight { get; private set; }

        /// <summary>
        ///     The BG Mask of the stage.
        /// </summary>
        public Sprite BgMask { get; private set; }

        /// <summary>
        ///     The HitPositionOverlay of the stage.
        /// </summary>
        public Sprite HitPositionOverlay { get; private set; }

        /// <summary>
        ///     The list of receptors
        /// </summary>
        public List<Sprite> Receptors { get; private set; }

        /// <summary>
        ///     The list of column lighting objects.
        /// </summary>
        public List<ColumnLighting> ColumnLightingObjects { get; private set; }

        /// <summary>
        ///     The distant overlay sprite.
        /// </summary>
        public Sprite DistantOverlay { get; private set; }

        /// <summary>
        ///     Container that contains the elements for lane cover.
        /// </summary>
        public Container LaneCoverContainer { get; private set; }

        /// <summary>
        ///     Sprite that displays the current combo.
        /// </summary>
        public ComboDisplay ComboDisplay { get; private set; }

        /// <summary>
        ///     The combo in the previous frame. Used to determine if we should update it.
        /// </summary>
        private int OldCombo { get; set; }

        /// <summary>
        ///     The HitError bar.
        /// </summary>
        public HitErrorBar HitError { get; private set; }

        /// <summary>
        ///     The HitError bar.
        /// </summary>
        public HitBubbles HitBubbles { get; private set; }

        /// <summary>
        ///     The JudgementHitBurst Sprite.
        /// </summary>
        public List<JudgementHitBurst> JudgementHitBursts { get; private set; }

        /// <summary>
        ///     When hitting an object, this is the sprite that will be shown at
        ///     the hitposition.
        /// </summary>
        public List<HitLighting> HitLightingObjects { get; private set; }

        /// <summary>
        ///     The health bar for the stage.
        /// </summary>
        public HealthBar HealthBar { get; private set; }

        /// <summary>
        ///     Displays the name of the song.
        /// </summary>
        private SongInformation SongInfo { get; set; }

        /// <summary>
        ///     Displays last place/elimination alerts for battle royale
        /// </summary>
        private BattleRoyaleAlert BattleRoyaleAlert { get; set; }

        /// <summary>
        ///     Make a quicker and shorter reference to the game skin
        /// </summary>
        public SkinKeys Skin => SkinManager.Skin.Keys[Screen.Map.Mode];

        /// <summary>
        /// </summary>
        private BattleRoyalePlayerEliminated BattleRoyalePlayerEliminated { get; set; }

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="playfield"></param>
        public GameplayPlayfieldKeysStage(GameplayScreen screen, GameplayPlayfieldKeys playfield)
        {
            Screen = screen;
            Playfield = playfield;

            if (Playfield.IsOmni)
            {
                CreateOmniStage();
                return;
            }

            CreateStageLeft();
            CreateStageRight();
            CreateBgMask();

            // Depending on what the skin.ini's value is, we'll want to either initialize
            // the receptors first, or the playfield first.
            if (ConfigManager.ReceptorsOverLaneCover.Value)
            {
                CreateTimingLineContainer();
                CreateHitObjectContainer();
                CreateHitContainer();
            }
            else if (Skin.ReceptorsOverHitObjects)
            {
                CreateTimingLineContainer();
                CreateHitObjectContainer();
                CreateHitContainer();

                CreateReceptorsAndLighting();
                CreateHitPositionOverlay();
            }
            else
            {
                CreateReceptorsAndLighting();
                CreateHitPositionOverlay();

                CreateTimingLineContainer();
                CreateHitObjectContainer();
                CreateHitContainer();
            }

            CreateDistantOverlay();

            // Depending on what the config value is, we'll display ui elements over the lane cover.
            // Note: Lane cover will always be displayed over the receptors due to the creation order.
            if (ConfigManager.UIElementsOverLaneCover.Value)
            {
                CreateLaneCoverOverlay();
                if (ConfigManager.ReceptorsOverLaneCover.Value)
                {
                    CreateReceptorsAndLighting();
                    CreateHitPositionOverlay();
                }

                CreateComboDisplay();
                CreateHitError();
                CreateHitBubbles();
                CreateHitLighting();
                CreateJudgementHitBurst();

                if (OnlineManager.CurrentGame?.Ruleset == MultiplayerGameRuleset.Battle_Royale &&
                    ConfigManager.EnableBattleRoyaleAlerts.Value)
                {
                    CreateBattleRoyaleAlert();
                    CreateBattleRoyaleEliminated();
                }

                CreateSongInfo();
            }
            else
            {
                CreateComboDisplay();
                CreateHitError();
                CreateHitBubbles();
                CreateJudgementHitBurst();

                if (OnlineManager.CurrentGame?.Ruleset == MultiplayerGameRuleset.Battle_Royale &&
                    ConfigManager.EnableBattleRoyaleAlerts.Value)
                {
                    CreateBattleRoyaleAlert();
                    CreateBattleRoyaleEliminated();
                }

                CreateSongInfo();

                CreateLaneCoverOverlay();
                if (ConfigManager.ReceptorsOverLaneCover.Value)
                {
                    CreateReceptorsAndLighting();
                    CreateHitPositionOverlay();
                }
                CreateHitLighting();
            }

            CreateHealthBar();
            CreateKeybindOverlay();
        }

        private void CreateOmniStage()
        {
            ColumnLightingObjects = new List<ColumnLighting>();

            OmniUiContainer = new Container
            {
                Parent = Playfield.ForegroundContainer,
                Size = new ScalableVector2(0, 0, 1, 1),
                Visible = false
            };

            CreateTimingLineContainer();

            if (Skin.ReceptorsOverHitObjects)
            {
                CreateHitObjectContainer();
                CreateHitContainer();
                CreateReceptorsAndLighting();
            }
            else
            {
                CreateReceptorsAndLighting();
                CreateHitObjectContainer();
                CreateHitContainer();
            }

            RefreshOmniLayout(Playfield.ForegroundContainer.Width, Playfield.ForegroundContainer.Height);

            CreateComboDisplay();
            CreateHitError();
            CreateHitBubbles();
            CreateJudgementHitBurst();
            CreateHitLighting();
            CreateHealthBar();
            HideOmniStageUi();
            RefreshOmniLayout(Playfield.ForegroundContainer.Width, Playfield.ForegroundContainer.Height);
        }

        /// <summary>
        ///     Keeps required UI instances alive for existing gameplay code, but prevents their
        ///     animations from becoming visible in Omni.
        /// </summary>
        private void HideOmniStageUi()
        {
            ComboDisplay.Parent = OmniUiContainer;
            HitError.Parent = OmniUiContainer;
            HitBubbles.Parent = OmniUiContainer;
            HealthBar.Parent = OmniUiContainer;

            foreach (var judgement in JudgementHitBursts)
                judgement.Parent = OmniUiContainer;
        }

        /// <summary>
        ///     Updates the stage and keeps everything in-tact.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime) => UpdateComboDisplay(gameTime);

        /// <summary>
        ///     Creates the left side of the stage.
        /// </summary>
        private void CreateStageLeft()
        {
            // Create the left side of the stage.
            var stageLeftX = Skin.StageLeftBorder.Width * WindowManager.Height / Skin.StageLeftBorder.Height;

            StageLeft = new Sprite()
            {
                Parent = Playfield.BackgroundContainer,
                Image = Skin.StageLeftBorder,
                Size = new ScalableVector2(stageLeftX, WindowManager.Height),
                Position = new ScalableVector2(-stageLeftX + 1, 0),
                Alignment = Alignment.TopLeft
            };
        }

        /// <summary>
        ///     Creates the right side of the stage.
        /// </summary>
        private void CreateStageRight()
        {
            // Create the right side of the stage.
            var stageRightX = Skin.StageRightBorder.Width * WindowManager.Height / Skin.StageRightBorder.Height;

            StageRight = new Sprite
            {
                Image = Skin.StageRightBorder,
                Size = new ScalableVector2(stageRightX, WindowManager.Height),
                Position = new ScalableVector2(stageRightX - 1, 0),
                Parent = Playfield.BackgroundContainer,
                Alignment = Alignment.TopRight
            };
        }

        /// <summary>
        ///     Creates the BG Mask of the stage.
        /// </summary>
        private void CreateBgMask()
        {
            var imageRatio = (double)Skin.StageBgMask.Width / Skin.StageBgMask.Height;
            var columnRatio = Playfield.Width / WindowManager.Height;
            var bgMaskSize = (float)Math.Max(WindowManager.Height * columnRatio / imageRatio, WindowManager.Height);

            BgMask = new Sprite
            {
                Image = Skin.StageBgMask,
                Alpha = Skin.BgMaskAlpha,
                Size = new ScalableVector2(Playfield.Width, bgMaskSize),
                Alignment = Alignment.MidCenter,
                Parent = Playfield.BackgroundContainer
            };
        }

        /// <summary>
        ///     Creates the HitPositionOverlay for the stage.
        /// </summary>
        private void CreateHitPositionOverlay()
        {
            // Create Stage HitPosition Overlay
            var sizeY = Skin.StageHitPositionOverlay.Height * Playfield.Width / Skin.StageHitPositionOverlay.Width;
            var offsetY = Playfield.LaneSize * ((float)Skin.NoteReceptorsUp[0].Height / Skin.NoteReceptorsUp[0].Width);
            var width = Playfield.Width;

            float y;
            switch (GameplayRulesetKeys.ScrollDirection)
            {
                case ScrollDirection.Down:
                    y = Playfield.ReceptorPositionY.First() - sizeY + Skin.HitPosOffsetY;
                    break;
                case ScrollDirection.Up:
                    y = Playfield.ReceptorPositionY.First() + offsetY - Skin.HitPosOffsetY;
                    break;
                case ScrollDirection.Split:
                    y = Playfield.ReceptorPositionY.First() - sizeY + Skin.HitPosOffsetY;
                    width = Playfield.Width / 2;

                    var splitHitPositionOverlay = new Sprite
                    {
                        Parent = Playfield.ForegroundContainer,
                        Image = Skin.StageHitPositionOverlay,
                        Rotation = MathF.PI,
                        Size = new ScalableVector2(width, sizeY),
                        X = width,
                        Y = Playfield.ReceptorPositionY.Last() + offsetY - Skin.HitPosOffsetY
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            HitPositionOverlay = new Sprite
            {
                Parent = Playfield.ForegroundContainer,
                Image = Skin.StageHitPositionOverlay,
                Rotation = GameplayRulesetKeys.ScrollDirection.Equals(ScrollDirection.Up) ? MathF.PI : 0,
                Size = new ScalableVector2(width, sizeY),
                Y = y
            };
        }

        /// <summary>
        ///     Creates the receptors and column lighting
        /// </summary>
        private void CreateReceptorsAndLighting()
        {
            Receptors = new List<Sprite>();
            ColumnLightingObjects = new List<ColumnLighting>();

            if (Playfield.IsOmni)
                OmniReceptorLaneRoots = CreateOmniLaneRoots(Playfield.ForegroundContainer);

            var scratchLaneLeft = ConfigManager.ScratchLanesLeft[Screen.Map.Mode].Value;

            // Go through and create the receptors and column lighting objects.
            for (var i = 0; i < Screen.Map.GetKeyCount(Screen.Map.HasScratchKey); i++)
            {
                var scale = ConfigManager.GameplayNoteScale.Value / 100f;
                var laneSize = Playfield.LaneSize;
                var defaultLanePos = (Playfield.LaneSize + Playfield.ReceptorPadding) * i + Playfield.Padding;
                var posX = defaultLanePos;

                // Handle scratch key positioning
                if (Screen.Map.HasScratchKey)
                {
                    if (i == Screen.Map.GetKeyCount() - 1)
                        laneSize = Skin.ScratchLaneSize;

                    if (scratchLaneLeft)
                    {
                        if (i == Screen.Map.GetKeyCount() - 1)
                            posX = Playfield.Padding;
                        else
                            posX = (Playfield.LaneSize + Playfield.ReceptorPadding) * i + Playfield.Padding +
                                   Skin.ScratchLaneSize + Playfield.ReceptorPadding;
                    }
                }
                else
                {
                    posX = (Playfield.LaneSize + Playfield.ReceptorPadding) * i + Playfield.Padding;
                }

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (scale != 1)
                    posX += (Playfield.LaneSize - Playfield.LaneSize * scale) / 2f;

                var receptorHeight = (Playfield.LaneSize * Skin.NoteReceptorsUp[i].Height /
                                      Skin.NoteReceptorsUp[i].Width) * scale;

                if (Playfield.IsOmni)
                    posX = -laneSize * scale / 2f;

                // Create individiaul receptor.
                Receptors.Add(new Sprite
                {
                    Parent = Playfield.IsOmni ? OmniReceptorLaneRoots[i] : Playfield.ForegroundContainer,
                    Size = new ScalableVector2(laneSize * scale, receptorHeight),
                    Position = new ScalableVector2(posX,
                        Playfield.IsOmni ? -receptorHeight / 2f : Playfield.ReceptorPositionY[i]),
                    Alignment = Alignment.TopLeft,
                    Image = Skin.NoteReceptorsUp[i],
                    SpriteEffect = Playfield.ScrollDirections[i] == ScrollDirection.Up &&
                                   Skin.FlipNoteImagesOnUpscroll
                        ? SpriteEffects.FlipVertically
                        : SpriteEffects.None,
                    Rotation = Skin.RotateReceptorsByColumn ? Skin.ReceptorRotations[i] / 180f * MathF.PI : 0,
                    IndependentRotation = Playfield.IsOmni
                });

                if (Playfield.IsOmni)
                    continue;

                // Create the column lighting sprite.
                var size = Skin.ColumnLightingScale * Playfield.LaneSize * ((float)Skin.ColumnLighting.Height / Skin.ColumnLighting.Width);
                ColumnLightingObjects.Add(new ColumnLighting
                {
                    Parent = Playfield.BackgroundContainer,
                    Image = Skin.ColumnLighting,
                    Size = new ScalableVector2(Playfield.LaneSize, size),
                    Tint = Skin.ColumnColors[i],
                    X = posX,
                    Y = Playfield.ColumnLightingPositionY[i],
                    // todo: case statement for scroll direction
                    SpriteEffect = !Playfield.ScrollDirections[i].Equals(ScrollDirection.Down) && Skin.FlipNoteImagesOnUpscroll ? SpriteEffects.FlipVertically : SpriteEffects.None,
                    Alignment = Alignment.TopLeft,
                });
            }
        }

        /// <summary>
        ///     Creates the HitObjectContainer
        /// </summary>
        private void CreateHitObjectContainer()
        {
            HitObjectContainers = new Container[Screen.Map.EditorLayers.Count + 1];
            OmniHitObjectLaneRoots = Playfield.IsOmni
                ? new Container[HitObjectContainers.Length][]
                : null;

            for (var i = 0; i <= Screen.Map.EditorLayers.Count; i++)
            {
                HitObjectContainers[i] = new Container
                {
                    Size = new ScalableVector2(Playfield.Width, 0, 0, 1),
                    Alignment = Alignment.TopCenter,
                    Parent = Playfield.ForegroundContainer
                };

                if (Playfield.IsOmni)
                    OmniHitObjectLaneRoots[i] = CreateOmniLaneRoots(HitObjectContainers[i]);
            }
        }

        /// <summary>
        ///     Creates the TimingLineContainer
        /// </summary>
        private void CreateTimingLineContainer() => TimingLineContainer = new Container
        {
            Size = new ScalableVector2(Playfield.Width, 0, 0, 1),
            Alignment = Alignment.TopCenter,
            Parent = Playfield.ForegroundContainer
        };

        /// <summary>
        ///     Creates the HitContainer
        /// </summary>
        private void CreateHitContainer()
        {
            HitContainer = new Container
            {
                Size = new ScalableVector2(Playfield.Width, 0, 0, 1),
                Alignment = Alignment.TopCenter,
                Parent = Playfield.ForegroundContainer
            };

            if (Playfield.IsOmni)
                OmniHitLaneRoots = CreateOmniLaneRoots(HitContainer);
        }

        private Container[] CreateOmniLaneRoots(Drawable parent)
        {
            var roots = new Container[Screen.Map.GetKeyCount(Screen.Map.HasScratchKey)];

            for (var i = 0; i < roots.Length; i++)
            {
                roots[i] = new Container
                {
                    Parent = parent,
                    Pivot = Vector2.Zero
                };
            }

            return roots;
        }

        internal Container GetHitObjectContainer(int editorLayer, int lane)
        {
            if (editorLayer < 0 || editorLayer >= HitObjectContainers.Length)
                editorLayer = 0;

            if (!Playfield.IsOmni)
                return HitObjectContainers[editorLayer];

            return OmniHitObjectLaneRoots[editorLayer][lane];
        }

        internal Container GetHitContainer(int lane) =>
            Playfield.IsOmni ? OmniHitLaneRoots[lane] : HitContainer;

        internal Container GetReceptorContainer(int lane) =>
            Playfield.IsOmni ? OmniReceptorLaneRoots[lane] : Playfield.ForegroundContainer;

        internal void RefreshOmniLayout(float width, float height)
        {
            if (!Playfield.IsOmni || Receptors == null || Receptors.Count == 0)
                return;

            TimingLineContainer.Size = new ScalableVector2(width, height);
            HitContainer.Size = new ScalableVector2(width, height);
            foreach (var container in HitObjectContainers)
                container.Size = new ScalableVector2(width, height);

            const float edgeMargin = 16;
            var footprint = Receptors.Max(x => MathF.Max(x.Width, x.Height));
            footprint = MathF.Max(1, footprint + MathF.Max(0, Playfield.ReceptorPadding));

            var laneCount = Receptors.Count;
            var radius = OmniLaneLayout.CalculateRadius(laneCount, footprint);
            var geometries = Playfield.OmniLaneGeometries;

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;

            foreach (var geometry in geometries)
            {
                var point = geometry.OutwardDirection * radius;
                minX = MathF.Min(minX, point.X - footprint / 2f);
                maxX = MathF.Max(maxX, point.X + footprint / 2f);
                minY = MathF.Min(minY, point.Y - footprint / 2f);
                maxY = MathF.Max(maxY, point.Y + footprint / 2f);
            }

            var requiredWidth = MathF.Max(1, maxX - minX);
            var requiredHeight = MathF.Max(1, maxY - minY);
            var availableWidth = MathF.Max(1, width - edgeMargin * 2);
            var availableHeight = MathF.Max(1, height - edgeMargin * 2);
            var layoutScale = MathF.Min(1, MathF.Min(availableWidth / requiredWidth,
                availableHeight / requiredHeight));

            var center = new Vector2(width / 2f, height / 2f);
            for (var lane = 0; lane < geometries.Length; lane++)
            {
                var geometry = geometries[lane];
                var position = center + geometry.OutwardDirection * radius * layoutScale;
                ApplyOmniLaneTransform(OmniReceptorLaneRoots?[lane], position, geometry.RootRotation,
                    layoutScale);
                ApplyOmniLaneTransform(OmniHitLaneRoots?[lane], position, geometry.RootRotation,
                    layoutScale);

                if (OmniHitObjectLaneRoots != null)
                {
                    foreach (var layer in OmniHitObjectLaneRoots)
                        ApplyOmniLaneTransform(layer?[lane], position, geometry.RootRotation, layoutScale);
                }
            }

            RefreshOmniOverlayPositions(layoutScale, footprint);
        }

        private static void ApplyOmniLaneTransform(Container root, Vector2 position, float rotation, float scale)
        {
            if (root == null)
                return;

            root.Position = new ScalableVector2(position.X, position.Y);
            root.Rotation = rotation;
            root.Scale = Vector2.One * scale;
        }

        private void RefreshOmniOverlayPositions(float layoutScale, float footprint)
        {
            if (!Playfield.IsOmni)
                return;

            for (var lane = 0; lane < KeybindOverlays.Count; lane++)
            {
                var direction = Playfield.OmniLaneGeometries[lane].OutwardDirection;
                var rootPosition = OmniReceptorLaneRoots[lane].Position;
                var offset = direction * (footprint / 2f + 20) * layoutScale;
                KeybindOverlays[lane].Position = new ScalableVector2(rootPosition.X.Value + offset.X,
                    rootPosition.Y.Value + offset.Y);
            }

            if (!Skin.DisplayJudgementsInEachColumn || JudgementHitBursts == null)
                return;

            for (var lane = 0; lane < JudgementHitBursts.Count; lane++)
            {
                var rootPosition = OmniReceptorLaneRoots[lane].Position;
                JudgementHitBursts[lane].Position = new ScalableVector2(rootPosition.X.Value,
                    rootPosition.Y.Value);
            }
        }

        /// <summary>
        ///     Creates the distant overlay sprite.
        /// </summary>
        private void CreateDistantOverlay()
        {
            var sizeY = Skin.StageDistantOverlay.Height * Playfield.Width / Skin.StageDistantOverlay.Width;
            DistantOverlay = new Sprite
            {
                Image = Skin.StageDistantOverlay,
                Size = new ScalableVector2(Playfield.Width, sizeY),
                // todo: case statement for scroll direction
                Y = GameplayRulesetKeys.ScrollDirection.Equals(ScrollDirection.Down) ? -1 : 1,
                // todo: case statement for scroll direction
                Alignment = GameplayRulesetKeys.ScrollDirection.Equals(ScrollDirection.Down) ? Alignment.TopRight : Alignment.BotRight,
                Parent = Playfield.ForegroundContainer
            };
        }

        /// <summary>
        ///     Creates the lane cover container and overlay sprites.
        /// </summary>
        private void CreateLaneCoverOverlay()
        {
            LaneCoverContainer = new Container
            {
                Size = new ScalableVector2(Playfield.ForegroundContainer.Width, Playfield.ForegroundContainer.Height, 0, 0),
                Alignment = Alignment.TopLeft,
                Parent = Playfield.ForegroundContainer
            };

            // Apply the covers.
            ApplyTopLaneCover();
            ApplyBottomLaneCover();
        }

        /// <summary>
        ///     Creates the combo display sprite.
        /// </summary>
        private void CreateComboDisplay()
        {
            var skin = SkinManager.Skin.Keys[Screen.Map.Mode];

            // Create the combo display.
            ComboDisplay = new ComboDisplay(NumberDisplayType.Combo, "0",
                new Vector2(skin.ComboDisplayScale / 100f, skin.ComboDisplayScale / 100f),
                Screen,
                Skin.ComboPosY)
            {
                Parent = Playfield.ForegroundContainer,
                Alignment = Alignment.MidCenter,
                X = Skin.ComboPosX
            };

            // Start off the map by making the display invisible.
            ComboDisplay.MakeInvisible();
        }

        /// <summary>
        ///     Updates the combo display and its animations
        /// </summary>
        /// <param name="gameTime"></param>
        private void UpdateComboDisplay(GameTime gameTime)
        {
            if (OldCombo == Screen.Ruleset.ScoreProcessor.Combo)
                return;

            // Set the new one
            ComboDisplay.UpdateValue(Screen.Ruleset.ScoreProcessor.Combo);
            ComboDisplay.StartBump();

            OldCombo = Screen.Ruleset.ScoreProcessor.Combo;
        }

        /// <summary>
        ///     Creates the HitError Sprite.
        /// </summary>
        private void CreateHitError() => HitError = new HitErrorBar(new ScalableVector2(50, Skin.HitErrorHeight))
        {
            Parent = Playfield.ForegroundContainer,
            Alignment = Alignment.MidCenter,
            Position = new ScalableVector2(Skin.HitErrorPosX, Skin.HitErrorPosY),
            Alpha = Skin.HitErrorAlpha
        };

        /// <summary>
        ///     Creates the HitError Sprite.
        /// </summary>
        private void CreateHitBubbles()
        {
            HitBubbles = new HitBubbles(Skin.HitBubblesScale, Skin.HitBubblesType, Screen)
            {
                Parent = Playfield.ForegroundContainer,
                Alignment = Alignment.MidCenter,
                Position = new ScalableVector2(Skin.HitBubblesPosX, Skin.HitBubblesPosY),
            };

            if (Playfield.IsOmni)
                return;

            switch (Skin.HitBubblesAlignment)
            {
                case HitBubblesAlignment.BelowStage:
                    HitBubbles.Alignment = Alignment.BotCenter;
                    break;
                case HitBubblesAlignment.RightStage:
                    HitBubbles.Parent = StageRight;
                    HitBubbles.Alignment = Alignment.MidLeft;
                    break;
                case HitBubblesAlignment.LeftStage:
                    HitBubbles.Parent = StageLeft;
                    HitBubbles.Alignment = Alignment.MidRight;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Creates the JudgementHitBurst sprite.
        /// </summary>
        private void CreateJudgementHitBurst()
        {
            var skin = SkinManager.Skin.Keys[Screen.Map.Mode];
            JudgementHitBursts = new List<JudgementHitBurst>();

            // Default the frames to miss.
            var frames = SkinManager.Skin.Judgements[Judgement.Miss];

            // Grab the first frame for convenience.
            var firstFrame = frames[0];

            // Set size w/ scaling.
            var size = new Vector2(firstFrame.Width, firstFrame.Height) * Skin.JudgementHitBurstScale / firstFrame.Height;

            var judgementBurstCount = skin.DisplayJudgementsInEachColumn ?
                Screen.Map.GetKeyCount(Screen.Map.HasScratchKey) : 1;

            var playfieldOffset = (Playfield.Width / 2) - (Playfield.LaneSize / 2);

            for (var lane = 0; lane < judgementBurstCount; lane++)
            {
                var judgementHitBurst = new JudgementHitBurst(Screen, frames, size, Skin.JudgementBurstPosY)
                {
                    Parent = Playfield.ForegroundContainer,
                    Alignment = Alignment.MidCenter,
                    X = skin.DisplayJudgementsInEachColumn && !Playfield.IsOmni
                        ? Receptors[lane].X - playfieldOffset
                        : 0
                };

                if (Playfield.IsOmni && skin.DisplayJudgementsInEachColumn)
                {
                    var position = OmniReceptorLaneRoots[lane].Position;
                    judgementHitBurst.Position = new ScalableVector2(position.X.Value, position.Y.Value);
                }

                if (skin.RotateJudgements && skin.DisplayJudgementsInEachColumn)
                    judgementHitBurst.Rotation = GameplayHitObjectKeys.GetObjectRotation(Screen.Map.Mode, lane);

                JudgementHitBursts.Add(judgementHitBurst);
            }
        }

        /// <summary>
        ///     Creates the hitlighting sprites.
        /// </summary>
        private void CreateHitLighting()
        {
            HitLightingObjects = new List<HitLighting>();

            for (var i = 0; i < Screen.Map.GetKeyCount(Screen.Map.HasScratchKey); i++)
            {
                var hl = new HitLighting(Playfield, i)
                {
                    Parent = GetReceptorContainer(i),
                    Visible = false,
                    Position = new ScalableVector2(Skin.HitLightingX, Skin.HitLightingY),
                    IndependentRotation = Playfield.IsOmni
                };

                var scale = Skin.HitLightingScale / 100;

                hl.Image = Skin.HitLighting.First();
                hl.Size = new ScalableVector2(hl.Image.Width * scale, hl.Image.Height * scale);

                if (Playfield.IsOmni)
                {
                    hl.X = Receptors[i].X + Receptors[i].Width / 2f - hl.Width / 2f;
                    hl.Y = Receptors[i].Y + Receptors[i].Height / 2f - hl.Height / 2f;
                }
                else
                {
                    var pos = GraphicsHelper.AlignRect(Alignment.MidCenter, hl.RelativeRectangle,
                        Receptors[i].ScreenRectangle);

                    hl.X = pos.X - Playfield.ForegroundContainer.ScreenRectangle.X;
                    hl.Y = pos.Y - Playfield.ForegroundContainer.ScreenRectangle.Y;
                }

                // Set the spritebatch options for the hitlighting in this case
                // if it's the first object.
                if (i == 0)
                    hl.SpriteBatchOptions = new SpriteBatchOptions() { BlendState = BlendState.Additive };
                // Use the previous object's spritebatch options so all of them use the same batch.
                else
                    hl.UsePreviousSpriteBatchOptions = true;

                HitLightingObjects.Add(hl);
            }
        }

        /// <summary>
        ///     Creates the health bar for the screen.
        /// </summary>
        private void CreateHealthBar()
        {
            var scale = SkinManager.Skin.Keys[MapManager.Selected.Value.Mode].HealthBarScale;
            HealthBar = new HealthBar(SkinManager.Skin.Keys[MapManager.Selected.Value.Mode].HealthBarType,
                Playfield.Ruleset.ScoreProcessor, new Vector2(scale / 100f, scale / 100f))
            {
                Parent = Playfield.ForegroundContainer,
            };

            switch (SkinManager.Skin.Keys[MapManager.Selected.Value.Mode].HealthBarKeysAlignment)
            {
                case HealthBarKeysAlignment.LeftStage:
                    if (Playfield.IsOmni)
                    {
                        HealthBar.Parent = Playfield.Container;
                        HealthBar.Alignment = Alignment.TopLeft;
                        break;
                    }

                    HealthBar.Parent = StageLeft;
                    HealthBar.X = -5;
                    HealthBar.Y = -10;
                    break;
                case HealthBarKeysAlignment.RightStage:
                    if (Playfield.IsOmni)
                    {
                        HealthBar.Parent = Playfield.Container;
                        HealthBar.Alignment = Alignment.TopRight;
                        break;
                    }

                    HealthBar.Parent = StageRight;
                    HealthBar.X = 5;
                    HealthBar.Y = -10;
                    break;
                case HealthBarKeysAlignment.TopLeft:
                    HealthBar.Parent = Playfield.Container;
                    HealthBar.Alignment = Alignment.TopLeft;
                    break;
            }

            HealthBar.X += Skin.HealthBarPosOffsetX;
            HealthBar.Y += Skin.HealthBarPosOffsetY;
        }

        /// <summary>
        ///     Creates the sprite that displays the song information.
        /// </summary>
        private void CreateSongInfo() => SongInfo = new SongInformation(Screen)
        {
            Parent = Playfield.ForegroundContainer,
            Alignment = Alignment.MidCenter,
            Y = -200
        };

        /// <summary>
        ///     Creates the sprite that displays battle royale alerts
        /// </summary>
        private void CreateBattleRoyaleAlert()
        {
            var width = SkinManager.Skin.BattleRoyaleWarning.Width;
            var height = SkinManager.Skin.BattleRoyaleWarning.Height;
            var size = new Vector2(width, height) * Skin.BattleRoyaleAlertScale / height;

            BattleRoyaleAlert = new BattleRoyaleAlert(Screen)
            {
                Parent = Playfield.ForegroundContainer,
                Alignment = Alignment.MidCenter,
                Position = new ScalableVector2(Skin.BattleRoyaleAlertPosX, Skin.BattleRoyaleAlertPosY),
                Size = new ScalableVector2(size.X, size.Y),
            };
        }

        private void CreateBattleRoyaleEliminated() => BattleRoyalePlayerEliminated = new BattleRoyalePlayerEliminated(Screen)
        {
            Parent = Playfield.ForegroundContainer,
            Alignment = Alignment.MidCenter,
            Position = new ScalableVector2(Skin.BattleRoyaleEliminatedPosX, Skin.BattleRoyaleEliminatedPosY)
        };

        /// <summary>
        ///     Updates the given receptor and column lighting activity
        ///     (Called when pressing/releasing keys.)
        /// </summary>
        public void SetReceptorAndLightingActivity(int index, bool pressed)
        {
            Receptors[index].Image = pressed ? Skin.NoteReceptorsDown[index] : Skin.NoteReceptorsUp[index];

            if (index < ColumnLightingObjects.Count)
                ColumnLightingObjects[index].Active = pressed;
        }

        /// <summary>
        ///     Applies a top lane cover if enabled in settings.
        /// </summary>
        private void ApplyTopLaneCover()
        {
            if (!ConfigManager.LaneCoverTop.Value)
                return;

            var yAxis = -LaneCoverContainer.Height + ConfigManager.LaneCoverTopHeight.Value / 100f * LaneCoverContainer.Height;

            var laneCoverTop = new Sprite
            {
                Image = Skin.LaneCoverTop,
                Y = yAxis,
                Size = new ScalableVector2(LaneCoverContainer.Width, LaneCoverContainer.Height),
                Alignment = Alignment.BotLeft,
                Parent = LaneCoverContainer,
            };
        }

        /// <summary>
        ///     Applies the bottom lane cover if enabled in settings.
        /// </summary>
        private void ApplyBottomLaneCover()
        {
            if (!ConfigManager.LaneCoverBottom.Value)
                return;

            var yAxis = LaneCoverContainer.Height - ConfigManager.LaneCoverBottomHeight.Value / 100f * LaneCoverContainer.Height;

            var laneCoverBottom = new Sprite
            {
                Image = Skin.LaneCoverBottom,
                Y = yAxis,
                Size = new ScalableVector2(LaneCoverContainer.Width, LaneCoverContainer.Height),
                Alignment = Alignment.TopLeft,
                Parent = LaneCoverContainer,
            };
        }

        /// <summary>
        /// </summary>
        private void CreateKeybindOverlay()
        {
            for (var i = 0; i < Receptors.Count; i++)
            {
                var input = (KeysInputManager)Playfield.Ruleset.InputManager;

                if (input.ReplayInputManager != null)
                    continue;

                var keybind = new SpriteTextPlus(FontManager.GetWobbleFont(Fonts.InterSemiBold),
                    input.BindingStore[i].Key.Value.GetName(), 32)
                {
                    Parent = Playfield.ForegroundContainer,
                    Alignment = Playfield.IsOmni
                        ? Alignment.MidCenter
                        : Playfield.ScrollDirections[i] == ScrollDirection.Down
                            ? Alignment.TopCenter
                            : Alignment.BotCenter,
                    Y = Playfield.IsOmni
                        ? OmniReceptorLaneRoots[i].Position.Y.Value
                        : Receptors[i].Y + (Playfield.ScrollDirections[i] == ScrollDirection.Down ? -20 : 20),
                    X = Playfield.IsOmni
                        ? OmniReceptorLaneRoots[i].Position.X.Value
                        : Receptors[i].X - Playfield.Width / 2f + Playfield.LaneSize / 2f,
                    Alpha = 1
                };

                KeybindOverlays.Add(keybind);

                if (Playfield.IsOmni)
                {
                    var footprint = MathF.Max(Receptors[i].Width, Receptors[i].Height);
                    var direction = Playfield.OmniLaneGeometries[i].OutwardDirection;
                    keybind.X += direction.X * (footprint / 2f + 20);
                    keybind.Y += direction.Y * (footprint / 2f + 20);
                }
                else if (Playfield.ScrollDirections[i] == ScrollDirection.Down)
                    keybind.Y -= keybind.Height;
                else
                    keybind.Y += keybind.Height;

                keybind.Wait(2000);
                keybind.FadeTo(0, Easing.Linear, 500);
            }
        }

        public void FadeIn()
        {
            const int time = 400;
            const Easing easing = Easing.Linear;

            if (BgMask != null)
            {
                BgMask.Alpha = 0;
                BgMask.FadeTo(1, Easing.Linear, time);
            }

            Receptors.ForEach(x =>
            {
                x.Alpha = 0;
                x.FadeTo(1, Easing.Linear, time);
            });

            ComboDisplay.Digits.ForEach(x =>
            {
                x.Alpha = 0;
                x.FadeTo(1, Easing.Linear, time);
            });

            void FadeDrawableTree(Drawable drawable)
            {
                if (drawable is Sprite sprite)
                {
                    sprite.Alpha = 0;
                    sprite.FadeTo(1, Easing.Linear, time - 200);
                }

                drawable.Children.ForEach(FadeDrawableTree);
            }

            HitObjectContainers.ForEach(FadeDrawableTree);

            HitError.Children.ForEach(x =>
            {
                if (x is Sprite sprite)
                {
                    sprite.Alpha = 0;
                    sprite.FadeTo(1, Easing.Linear, time);
                }
            });

            if (StageLeft != null)
            {
                StageLeft.Alpha = 0;
                StageLeft.FadeTo(1, Easing.Linear, time);
            }

            if (StageRight != null)
            {
                StageRight.Alpha = 0;
                StageRight.FadeTo(1, Easing.Linear, time);
            }
        }
    }
}
