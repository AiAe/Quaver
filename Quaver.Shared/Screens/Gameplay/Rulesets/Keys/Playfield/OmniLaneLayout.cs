/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using Microsoft.Xna.Framework;

namespace Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Playfield
{
    internal readonly struct OmniLaneGeometry
    {
        public Vector2 OutwardDirection { get; }

        public float RootRotation { get; }

        public OmniLaneGeometry(Vector2 outwardDirection)
        {
            OutwardDirection = outwardDirection;

            // Lane-local negative Y points away from the playfield center.
            RootRotation = MathF.Atan2(outwardDirection.Y, outwardDirection.X) + MathF.PI / 2f;
        }
    }

    internal static class OmniLaneLayout
    {
        private const float MinimumRadiusInFootprints = 1.5f;

        public static OmniLaneGeometry[] Create(int laneCount)
        {
            if (laneCount < 2)
                throw new ArgumentOutOfRangeException(nameof(laneCount), laneCount,
                    "Omni layouts require at least two lanes.");

            var result = new OmniLaneGeometry[laneCount];

            if (laneCount == 4)
            {
                // Preserve the legacy 4K order: left, down, up, right.
                result[0] = FromDegrees(180);
                result[1] = FromDegrees(-90);
                result[2] = FromDegrees(90);
                result[3] = FromDegrees(0);
                return result;
            }

            var step = 360f / laneCount;
            for (var i = 0; i < laneCount; i++)
                result[i] = FromDegrees(180 - i * step);

            return result;
        }

        public static float CalculateRadius(int laneCount, float footprint)
        {
            if (laneCount < 2)
                throw new ArgumentOutOfRangeException(nameof(laneCount));

            var angleStep = MathF.Tau / laneCount;
            var chordRadius = footprint / (2f * MathF.Sin(angleStep / 2f));
            return MathF.Max(footprint * MinimumRadiusInFootprints, chordRadius);
        }

        private static OmniLaneGeometry FromDegrees(float degrees)
        {
            var radians = MathHelper.ToRadians(degrees);

            // Positive layout degrees point into the upper half of screen space.
            return new OmniLaneGeometry(new Vector2(MathF.Cos(radians), -MathF.Sin(radians)));
        }
    }
}
