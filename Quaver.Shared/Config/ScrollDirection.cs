/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Quaver.Shared.Config
{
    public enum ScrollDirection
    {
        Down,
        Up,
        Split,
        Omni
    }

    internal static class ScrollDirectionExtensions
    {
        /// <summary>
        ///     Whether positive lane-local Y points toward the receptor. Omni lanes use downscroll math before
        ///     their lane roots rotate that local axis into screen space.
        /// </summary>
        public static bool UsesDownscrollMath(this ScrollDirection direction) =>
            direction == ScrollDirection.Down || direction == ScrollDirection.Omni;
    }
}
