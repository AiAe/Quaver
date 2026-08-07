using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Quaver.Shared.Screens;
using Wobble;
using Wobble.Input;
using Wobble.Logging;
using Wobble.Window;

namespace Quaver.Shared.IPC
{
    /// <summary>
    ///     Deterministic mouse automation for local DEBUG UI testing.
    ///     Commands are queued from IPC and applied before Wobble samples mouse input for a frame.
    /// </summary>
    public static class QuaverDebugInputController
    {
        private const int MaximumQueuedActions = 256;
        private const int DefaultDragDurationMilliseconds = 250;
        private const int MillisecondsPerFrame = 16;

#if DEBUG
        private static readonly object ActionLock = new();
        private static readonly Queue<DebugInputAction> Actions = new();
        private static DebugInputAction ActiveAction { get; set; }
        private static long NextActionId;

        private static bool IsInitialized { get; set; }
        private static Vector2 Position { get; set; }
        private static int ScrollWheelValue { get; set; }
        private static ButtonState LeftButton { get; set; }
        private static ButtonState RightButton { get; set; }
        private static ButtonState MiddleButton { get; set; }

        /// <summary>
        ///     Parses and queues a DEBUG input command.
        /// </summary>
        /// <param name="message">The command after <c>debug/input/</c>.</param>
        public static void Handle(string message)
        {
#if DEBUG
            if (!TryParseAction(message, out var action, out var error))
            {
                Logger.Warning($"Invalid DEBUG INPUT command `{message}`: {error}", LogType.Runtime);
                return;
            }

            lock (ActionLock)
            {
                if (Actions.Count >= MaximumQueuedActions)
                {
                    Logger.Warning("Ignoring DEBUG INPUT command because the automation queue is full.",
                        LogType.Runtime);
                    return;
                }

                Actions.Enqueue(action);
            }

            Logger.Important($"Queued DEBUG INPUT `{action.Id}`: {action.Description}", LogType.Runtime);
#endif
        }

#endif

        /// <summary>
        ///     Applies one queued automation step before Wobble updates the screen for the current frame.
        /// </summary>
        public static void UpdateForBuild()
        {
#if DEBUG
            Update();
#endif
        }

        /// <summary>
        ///     Toggles deterministic mouse input for the local DEBUG build.
        ///     Release builds intentionally do nothing.
        /// </summary>
        public static void ToggleForBuild()
        {
#if DEBUG
            Toggle();
#endif
        }

#if DEBUG
        private static void Update()
        {
            if (GameBase.Game is not QuaverGame game || game.CurrentScreen == null ||
                game.CurrentScreen.Type == QuaverScreenType.Initialization || game.CurrentScreen.Exiting)
                return;

            if (!IsInitialized && !MouseManager.IsSyntheticInputEnabled && !HasPendingActions())
                return;

            if (!IsInitialized)
                InitializeFromPlatformMouse();

            ActiveAction ??= DequeueAction();

            if (ActiveAction != null)
                ApplyAction(ActiveAction);

            if (MouseManager.IsSyntheticInputEnabled)
                MouseManager.SetSyntheticState(CreateMouseState());
        }

        private static void Toggle()
        {
            if (MouseManager.IsSyntheticInputEnabled)
            {
                DisableSyntheticInput();
                Logger.Important("DEBUG INPUT synthetic cursor control disabled.", LogType.Runtime);
                return;
            }

            if (GameBase.Game is not QuaverGame game || game.CurrentScreen == null ||
                game.CurrentScreen.Type == QuaverScreenType.Initialization || game.CurrentScreen.Exiting)
            {
                Logger.Warning("Could not enable DEBUG INPUT synthetic cursor control before the game finished loading.",
                    LogType.Runtime);
                return;
            }

            InitializeFromPlatformMouse();
            Logger.Important("DEBUG INPUT synthetic cursor control enabled.", LogType.Runtime);
        }

        private static void DisableSyntheticInput()
        {
            LeftButton = ButtonState.Released;
            RightButton = ButtonState.Released;
            MiddleButton = ButtonState.Released;
            ActiveAction = null;

            lock (ActionLock)
                Actions.Clear();

            if (IsInitialized)
                MouseManager.SetSyntheticState(CreateMouseState());

            MouseManager.DisableSyntheticInput();
            IsInitialized = false;
        }

        private static void InitializeFromPlatformMouse()
        {
            var state = new EnhancedMouseState(Mouse.GetState());

            Position = ClampPosition(state.Position);
            ScrollWheelValue = state.ScrollWheelValue;
            LeftButton = state.LeftButton;
            RightButton = state.RightButton;
            MiddleButton = state.MiddleButton;
            IsInitialized = true;

            MouseManager.EnableSyntheticInput(CreateMouseState());
        }

        private static DebugInputAction DequeueAction()
        {
            lock (ActionLock)
                return Actions.Count > 0 ? Actions.Dequeue() : null;
        }

        private static void ApplyAction(DebugInputAction action)
        {
            switch (action.Type)
            {
                case DebugInputActionType.Move:
                    MoveTo(action.Position);
                    CompleteAction(action);
                    break;
                case DebugInputActionType.Press:
                    MoveTo(ResolveActionPosition(action));
                    SetButton(action.Button, ButtonState.Pressed);
                    CompleteAction(action);
                    break;
                case DebugInputActionType.Release:
                    MoveTo(ResolveActionPosition(action));
                    SetButton(action.Button, ButtonState.Released);
                    CompleteAction(action);
                    break;
                case DebugInputActionType.Click:
                    ApplyClick(action);
                    break;
                case DebugInputActionType.Drag:
                    ApplyDrag(action);
                    break;
                case DebugInputActionType.Scroll:
                    MoveTo(ResolveActionPosition(action));
                    ScrollWheelValue += action.ScrollDelta;
                    CompleteAction(action);
                    break;
                case DebugInputActionType.Reset:
                    ResetSyntheticInput(action);
                    break;
                case DebugInputActionType.Status:
                    LogStatus(action);
                    CompleteAction(action);
                    break;
                default:
                    CompleteAction(action);
                    break;
            }
        }

        private static void ApplyClick(DebugInputAction action)
        {
            if (action.Phase == 0)
            {
                MoveTo(action.Position);
                SetButton(action.Button, ButtonState.Pressed);
                action.Phase = 1;
                return;
            }

            SetButton(action.Button, ButtonState.Released);
            CompleteAction(action);
        }

        private static void ResetSyntheticInput(DebugInputAction action)
        {
            if (action.Phase == 0)
            {
                LeftButton = ButtonState.Released;
                RightButton = ButtonState.Released;
                MiddleButton = ButtonState.Released;
                action.Phase = 1;
                return;
            }

            MouseManager.DisableSyntheticInput();
            IsInitialized = false;
            CompleteAction(action);
        }

        private static void ApplyDrag(DebugInputAction action)
        {
            if (action.Phase == 0)
            {
                action.StartPosition = Position;
                MoveTo(action.StartPosition);
                SetButton(action.Button, ButtonState.Pressed);
                action.Phase = 1;
                return;
            }

            if (action.Phase <= action.DurationFrames)
            {
                var progress = action.Phase / (float) action.DurationFrames;
                MoveTo(Vector2.Lerp(action.StartPosition, action.Position, progress));
                SetButton(action.Button, ButtonState.Pressed);
                action.Phase++;
                return;
            }

            MoveTo(action.Position);
            SetButton(action.Button, ButtonState.Released);
            CompleteAction(action);
        }

        private static void MoveTo(Vector2 position)
        {
            Position = ClampPosition(position);

            // Keep the native pointer close to the synthetic pointer for screenshots and hover previews.
            Mouse.SetPosition((int) MathF.Round(Position.X * WindowManager.ScreenScale.X),
                (int) MathF.Round(Position.Y * WindowManager.ScreenScale.Y));
        }

        private static Vector2 ResolveActionPosition(DebugInputAction action) =>
            action.HasPosition ? action.Position : Position;

        private static void SetButton(MouseButton button, ButtonState state)
        {
            switch (button)
            {
                case MouseButton.Left:
                    LeftButton = state;
                    break;
                case MouseButton.Right:
                    RightButton = state;
                    break;
                case MouseButton.Middle:
                    MiddleButton = state;
                    break;
            }
        }

        private static EnhancedMouseState CreateMouseState() =>
            new(Position, ScrollWheelValue, LeftButton, RightButton, MiddleButton,
                ButtonState.Released, ButtonState.Released);

        private static Vector2 ClampPosition(Vector2 position) => new(
            MathHelper.Clamp(position.X, 0, Math.Max(0, WindowManager.Width)),
            MathHelper.Clamp(position.Y, 0, Math.Max(0, WindowManager.Height)));

        private static void CompleteAction(DebugInputAction action)
        {
            Logger.Important(
                $"Completed DEBUG INPUT `{action.Id}`: {action.Description} at " +
                $"({Position.X:0.##}, {Position.Y:0.##}) on {((QuaverGame) GameBase.Game).CurrentScreen.Type}.",
                LogType.Runtime);
            ActiveAction = null;
        }

        private static void LogStatus(DebugInputAction action)
        {
            var queueCount = GetQueueCount();
            var screen = ((QuaverGame) GameBase.Game).CurrentScreen?.Type.ToString() ?? "None";

            Logger.Important(
                $"DEBUG INPUT `{action.Id}` status: ready={IsInitialized}, screen={screen}, " +
                $"position=({Position.X:0.##}, {Position.Y:0.##}), queue={queueCount}.", LogType.Runtime);
        }

        private static int GetQueueCount()
        {
            lock (ActionLock)
                return Actions.Count + (ActiveAction == null ? 0 : 1);
        }

        private static bool HasPendingActions()
        {
            lock (ActionLock)
                return Actions.Count > 0 || ActiveAction != null;
        }

        private static bool TryParseAction(string message, out DebugInputAction action, out string error)
        {
            action = null;
            error = null;

            var separator = message?.IndexOf('?') ?? -1;
            var actionName = (separator >= 0 ? message[..separator] : message)?.Trim().Trim('/').ToLowerInvariant();
            var parameters = ParseParameters(separator >= 0 ? message[(separator + 1)..] : string.Empty);
            var id = parameters.TryGetValue("id", out var suppliedId) && !string.IsNullOrWhiteSpace(suppliedId)
                ? suppliedId
                : $"input-{Interlocked.Increment(ref NextActionId)}";

            if (string.IsNullOrWhiteSpace(actionName))
                return Fail("an action name is required", out error);

            switch (actionName)
            {
                case "move":
                    if (!TryGetRequiredPosition(parameters, out var movePosition, out error))
                        return false;

                    action = new DebugInputAction(id, DebugInputActionType.Move, $"move to {Format(movePosition)}")
                    {
                        Position = movePosition
                    };
                    return true;
                case "click":
                    if (!TryGetRequiredPosition(parameters, out var clickPosition, out error) ||
                        !TryGetButton(parameters, out var clickButton, out error))
                        return false;

                    action = new DebugInputAction(id, DebugInputActionType.Click,
                        $"click {clickButton} at {Format(clickPosition)}")
                    {
                        Position = clickPosition,
                        Button = clickButton
                    };
                    return true;
                case "press":
                    if (!TryGetOptionalPosition(parameters, out var pressPosition, out error) ||
                        !TryGetButton(parameters, out var pressButton, out error))
                        return false;

                    var hasPressPosition = HasPosition(parameters);
                    action = new DebugInputAction(id, DebugInputActionType.Press,
                        $"press {pressButton} at {DescribePosition(pressPosition, hasPressPosition)}")
                    {
                        Position = pressPosition,
                        Button = pressButton,
                        HasPosition = hasPressPosition
                    };
                    return true;
                case "release":
                    if (!TryGetOptionalPosition(parameters, out var releasePosition, out error) ||
                        !TryGetButton(parameters, out var releaseButton, out error))
                        return false;

                    var hasReleasePosition = HasPosition(parameters);
                    action = new DebugInputAction(id, DebugInputActionType.Release,
                        $"release {releaseButton} at {DescribePosition(releasePosition, hasReleasePosition)}")
                    {
                        Position = releasePosition,
                        Button = releaseButton,
                        HasPosition = hasReleasePosition
                    };
                    return true;
                case "drag":
                    if (!TryGetRequiredPosition(parameters, out var dragPosition, out error) ||
                        !TryGetButton(parameters, out var dragButton, out error) ||
                        !TryGetDuration(parameters, out var durationFrames, out error))
                        return false;

                    action = new DebugInputAction(id, DebugInputActionType.Drag,
                        $"drag {dragButton} to {Format(dragPosition)} over {durationFrames} frames")
                    {
                        Position = dragPosition,
                        Button = dragButton,
                        DurationFrames = durationFrames
                    };
                    return true;
                case "scroll":
                    if (!TryGetOptionalPosition(parameters, out var scrollPosition, out error) ||
                        !TryGetInteger(parameters, "delta", out var scrollDelta, out error))
                        return false;

                    var hasScrollPosition = HasPosition(parameters);
                    action = new DebugInputAction(id, DebugInputActionType.Scroll,
                        $"scroll {scrollDelta} at {DescribePosition(scrollPosition, hasScrollPosition)}")
                    {
                        Position = scrollPosition,
                        ScrollDelta = scrollDelta,
                        HasPosition = hasScrollPosition
                    };
                    return true;
                case "reset":
                    action = new DebugInputAction(id, DebugInputActionType.Reset, "reset synthetic input");
                    return true;
                case "status":
                    action = new DebugInputAction(id, DebugInputActionType.Status, "report input status");
                    return true;
                default:
                    return Fail(
                        "supported actions are move, click, press, release, drag, scroll, reset, and status",
                        out error);
            }
        }

        private static Dictionary<string, string> ParseParameters(string query)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in (query ?? string.Empty).Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = part.IndexOf('=');
                var key = separator >= 0 ? part[..separator] : part;
                var value = separator >= 0 ? part[(separator + 1)..] : string.Empty;

                if (!string.IsNullOrWhiteSpace(key))
                    parameters[Uri.UnescapeDataString(key.Replace('+', ' '))] =
                        Uri.UnescapeDataString(value.Replace('+', ' '));
            }

            return parameters;
        }

        private static bool TryGetRequiredPosition(Dictionary<string, string> parameters, out Vector2 position,
            out string error)
        {
            if (!TryGetOptionalPosition(parameters, out position, out error))
                return false;

            if (!parameters.ContainsKey("x") || !parameters.ContainsKey("y"))
            {
                error = "x and y are required";
                return false;
            }

            return true;
        }

        private static bool TryGetOptionalPosition(Dictionary<string, string> parameters, out Vector2 position,
            out string error)
        {
            position = Vector2.Zero;
            error = null;

            if (!parameters.ContainsKey("x") && !parameters.ContainsKey("y"))
                return true;

            if (!parameters.TryGetValue("x", out var x) || !TryGetFiniteFloat(x, out var parsedX) ||
                !parameters.TryGetValue("y", out var y) || !TryGetFiniteFloat(y, out var parsedY))
            {
                error = "x and y must be finite numbers";
                return false;
            }

            position = new Vector2(parsedX, parsedY);
            return true;
        }

        private static bool HasPosition(Dictionary<string, string> parameters) =>
            parameters.ContainsKey("x") || parameters.ContainsKey("y");

        private static bool TryGetButton(Dictionary<string, string> parameters, out MouseButton button,
            out string error)
        {
            button = MouseButton.Left;
            error = null;

            if (!parameters.TryGetValue("button", out var value) || string.IsNullOrWhiteSpace(value))
                return true;

            if (Enum.TryParse(value, true, out button) &&
                (button == MouseButton.Left || button == MouseButton.Right || button == MouseButton.Middle))
                return true;

            error = "button must be left, right, or middle";
            return false;
        }

        private static bool TryGetDuration(Dictionary<string, string> parameters, out int frames,
            out string error)
        {
            frames = Math.Max(1, DefaultDragDurationMilliseconds / MillisecondsPerFrame);
            error = null;

            if (!parameters.TryGetValue("duration", out var duration))
                return true;

            if (!int.TryParse(duration, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds) ||
                milliseconds < 1 || milliseconds > 10000)
            {
                error = "duration must be between 1 and 10000 milliseconds";
                return false;
            }

            frames = Math.Max(1, (int) Math.Ceiling(milliseconds / (double) MillisecondsPerFrame));
            return true;
        }

        private static bool TryGetInteger(Dictionary<string, string> parameters, string name, out int value,
            out string error)
        {
            value = 0;
            error = null;

            if (!parameters.TryGetValue(name, out var raw) ||
                !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                error = $"{name} must be an integer";
                return false;
            }

            return true;
        }

        private static bool TryGetFiniteFloat(string value, out float result) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) &&
            float.IsFinite(result);

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }

        private static string Format(Vector2 position) => $"({position.X:0.##}, {position.Y:0.##})";

        private static string DescribePosition(Vector2 position, bool hasPosition) =>
            hasPosition ? Format(position) : "current position";

        private enum DebugInputActionType
        {
            Move,
            Click,
            Press,
            Release,
            Drag,
            Scroll,
            Reset,
            Status
        }

        private sealed class DebugInputAction
        {
            public DebugInputAction(string id, DebugInputActionType type, string description)
            {
                Id = id;
                Type = type;
                Description = description;
            }

            public string Id { get; }
            public DebugInputActionType Type { get; }
            public string Description { get; }
            public Vector2 Position { get; set; }
            public bool HasPosition { get; set; }
            public Vector2 StartPosition { get; set; }
            public MouseButton Button { get; set; }
            public int ScrollDelta { get; set; }
            public int DurationFrames { get; set; }
            public int Phase { get; set; }
        }
#endif
    }
}
