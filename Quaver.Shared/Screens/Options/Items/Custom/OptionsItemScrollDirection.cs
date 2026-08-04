using System.Collections.Generic;
using MonoGame.Extended;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.Shared.Config;
using Quaver.Shared.Graphics;
using Quaver.Shared.Graphics.Form.Dropdowns;
using Wobble.Bindables;
using Wobble.Graphics;

namespace Quaver.Shared.Screens.Options.Items.Custom
{
    public class OptionsItemScrollDirection : OptionsItemDropdown
    {
        private static readonly IReadOnlyList<ScrollDirection> AllDirections = new[]
        {
            ScrollDirection.Down,
            ScrollDirection.Up,
            ScrollDirection.Split,
            ScrollDirection.Omni
        };

        /// <summary>
        /// </summary>
        /// <param name="containerRect"></param>
        /// <param name="name"></param>
        /// <param name="direction"></param>
        public OptionsItemScrollDirection(RectangleF containerRect, string name, GameMode mode,
            Bindable<ScrollDirection> direction)
            : this(containerRect, name, direction, GetDirections(mode))
        {
        }

        private OptionsItemScrollDirection(RectangleF containerRect, string name,
            Bindable<ScrollDirection> direction, IReadOnlyList<ScrollDirection> directions)
            : base(containerRect, name, new Dropdown(GetOptions(directions), new ScalableVector2(180, 35), 20,
                Colors.MainAccent, GetSelectedIndex(direction, directions)))
        {
            Tags = new List<string>()
            {
                "upscroll",
                "downscroll",
                "omni",
                "radial"
            };

            Dropdown.ItemSelected += (sender, args) =>
            {
                if (direction == null)
                    return;

                if (args.Index >= 0 && args.Index < directions.Count)
                    direction.Value = directions[args.Index];
            };
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private static IReadOnlyList<ScrollDirection> GetDirections(GameMode mode)
        {
            if (ModeHelper.ToKeyCount(mode) != 1)
                return AllDirections;

            return new[] { ScrollDirection.Down, ScrollDirection.Up, ScrollDirection.Split };
        }

        private static List<string> GetOptions(IReadOnlyList<ScrollDirection> directions)
        {
            var options = new List<string>();

            foreach (var val in directions)
                options.Add(val.ToString());

            return options;
        }

        /// <summary>
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private static int GetSelectedIndex(Bindable<ScrollDirection> direction,
            IReadOnlyList<ScrollDirection> directions)
        {
            if (direction == null)
                return 0;

            for (var i = 0; i < directions.Count; i++)
            {
                if (directions[i] == direction.Value)
                    return i;
            }

            return 0;
        }
    }
}
