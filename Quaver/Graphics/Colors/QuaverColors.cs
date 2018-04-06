﻿using Microsoft.Xna.Framework;

namespace Quaver.Graphics.Colors
{
    public struct QuaverColors
    {
        /// <summary>
        /// Swan's favorite color; #db88c2. This color is used for the developers of the game.
        /// </summary>
        public static readonly Color NameTagAdmin = new Color(219, 136, 194);

        /// <summary>
        /// Nametag color for Community Managers, Map Nominators, ect.
        /// </summary>
        public static readonly Color NameTagModerator = new Color(59, 233, 106);

        /// <summary>
        /// Nametag color for Quaver Supporters/Donators.
        /// </summary>
        public static readonly Color NameTagSupporter = new Color(76, 146, 211);

        /// <summary>
        /// Nametag color for regular users.
        /// </summary>
        public static readonly Color NameTagRegular = new Color(76, 146, 211);

        /// <summary>
        ///     Color for dead notes
        /// </summary>
        public static readonly Color DeadNote = new Color(50, 50, 50);

        /// <summary>
        ///     Colors for different grades
        /// </summary>
        public static readonly Color GradeXX = new Color(255, 255, 255);
        public static readonly Color GradeX = new Color(255, 255, 255);
        public static readonly Color GradeSS = new Color(255, 255, 125);
        public static readonly Color GradeS = new Color(255, 255, 0);
        public static readonly Color GradeA = new Color(0, 255, 0);
        public static readonly Color GradeB = new Color(0, 30, 255);
        public static readonly Color GradeC = new Color(255, 0, 255);
        public static readonly Color GradeD = new Color(255, 70, 0);
        public static readonly Color GradeF = new Color(255, 0, 0);
        public static readonly Color[] GradeColors = new Color[9] { GradeF, GradeD, GradeC, GradeB, GradeA, GradeS, GradeSS, GradeX, GradeXX };

        /// <summary>
        ///     Colors for note snaps
        /// </summary>
        public static readonly Color Snap1 = Color.Red;
        public static readonly Color Snap2 = Color.Blue;
        public static readonly Color Snap3 = Color.Purple;
        public static readonly Color Snap4 = Color.Yellow;
        public static readonly Color Snap6 = Color.Magenta;
        public static readonly Color Snap8 = Color.Orange;
        public static readonly Color Snap12 = Color.Cyan;
        public static readonly Color Snap16 = Color.Green;
        public static readonly Color Snap48 = Color.White;
        public static readonly Color[] SnapColors = new Color[9] { Snap1, Snap2, Snap3, Snap4, Snap6, Snap8, Snap12, Snap16, Snap48 };
    }
}