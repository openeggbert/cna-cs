using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace XnaCompatibilityCompileProbe;

/// <summary>
/// Deterministic constructor/value-semantics observations for the engine-neutral input structs.
/// Hardware polling is intentionally excluded so the same source can run without a window or
/// controller on every implementation.
/// </summary>
public static class InputBehaviorCorpus
{
    public static IReadOnlyList<string> Capture()
    {
        var observations = new List<string>();

        var nullKeyboard = new KeyboardState((Keys[])null!);
        observations.Add($"keyboard.null.count={nullKeyboard.GetPressedKeys().Length}");

        var keyboard = new KeyboardState(Keys.Z, (Keys)7, Keys.A, Keys.A, (Keys)300);
        observations.Add("keyboard.pressed=" + string.Join(",", keyboard.GetPressedKeys().Select(key => (int)key)));
        observations.Add($"keyboard.invalid={Flag(keyboard.IsKeyDown((Keys)7))},{Flag(keyboard.IsKeyDown((Keys)300))}");
        observations.Add("keyboard.hash=" + keyboard.GetHashCode().ToString(CultureInfo.InvariantCulture));

        var mouse = new MouseState(
            12, -3, 120,
            ButtonState.Pressed, ButtonState.Released, ButtonState.Pressed,
            ButtonState.Pressed, ButtonState.Released);
        observations.Add("mouse.string=" + mouse);
        observations.Add("mouse.hash=" + mouse.GetHashCode().ToString(CultureInfo.InvariantCulture));

        var thumbSticks = new GamePadThumbSticks(new Vector2(2f, -2f), new Vector2(0.25f, -0.5f));
        observations.Add(
            $"thumbs.clamp={Bits(thumbSticks.Left.X)},{Bits(thumbSticks.Left.Y)}," +
            $"{Bits(thumbSticks.Right.X)},{Bits(thumbSticks.Right.Y)}");
        var triggers = new GamePadTriggers(-0.5f, 1.5f);
        observations.Add($"triggers.clamp={Bits(triggers.Left)},{Bits(triggers.Right)}");

        observations.Add("gamepad.null=" + ExceptionName(() => _ = new GamePadState(
            new Vector2(0.1f, -0.3f),
            new Vector2(0.3f, -0.3f),
            0.1f,
            0.2f,
            (Buttons[])null!)));
        var state = new GamePadState(
            new Vector2(0.1f, -0.3f),
            new Vector2(0.3f, -0.3f),
            0.1f,
            0.2f,
            Array.Empty<Buttons>());
        observations.Add(
            "gamepad.virtual=" + string.Join(",",
            new int[]
            {
                Flag(state.IsButtonDown(Buttons.LeftThumbstickRight)),
                Flag(state.IsButtonDown(Buttons.LeftThumbstickDown)),
                Flag(state.IsButtonDown(Buttons.RightThumbstickRight)),
                Flag(state.IsButtonDown(Buttons.RightThumbstickDown)),
                Flag(state.IsButtonDown(Buttons.LeftTrigger)),
                Flag(state.IsButtonDown(Buttons.RightTrigger)),
            }));
        var filteredButtons = new GamePadState(
            Vector2.Zero,
            Vector2.Zero,
            0f,
            0f,
            new[]
            {
                Buttons.A,
                Buttons.LeftTrigger,
                (Buttons)0x40000000,
                unchecked((Buttons)0x80000000u),
            });
        observations.Add(
            $"gamepad.filtered={Flag(filteredButtons.IsButtonDown(Buttons.A))}," +
            $"{Flag(filteredButtons.IsButtonDown(Buttons.LeftTrigger))}," +
            $"{Flag(filteredButtons.IsButtonDown((Buttons)0x40000000))}," +
            Flag(filteredButtons.IsButtonDown(unchecked((Buttons)0x80000000u))));
        observations.Add("gamepad.string=" + state);

        var buttons = new GamePadButtons(Buttons.A | Buttons.Y | Buttons.Back);
        observations.Add("buttons.string=" + buttons);
        observations.Add("buttons.hash=" + buttons.GetHashCode().ToString(CultureInfo.InvariantCulture));
        var dPad = new GamePadDPad(
            ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Pressed);
        observations.Add("dpad.string=" + dPad);
        observations.Add("dpad.hash=" + dPad.GetHashCode().ToString(CultureInfo.InvariantCulture));

        var withoutPrevious = new TouchLocation(7, TouchLocationState.Pressed, new Vector2(1f, 2f));
        bool hasPrevious = withoutPrevious.TryGetPreviousLocation(out TouchLocation previous);
        observations.Add(
            $"touch.previous.none={Flag(hasPrevious)},{previous.Id},{(int)previous.State}");

        var first = new TouchLocation(
            5, TouchLocationState.Pressed, new Vector2(1f, 2f),
            TouchLocationState.Moved, new Vector2(0.5f, 1.5f));
        var sameCoordinates = new TouchLocation(
            5, TouchLocationState.Released, new Vector2(1f, 2f),
            TouchLocationState.Released, new Vector2(0.5f, 1.5f));
        observations.Add($"touch.equals={Flag(first.Equals(sameCoordinates))},{Flag(first == sameCoordinates)}");
        observations.Add("touch.hash=" + first.GetHashCode().ToString(CultureInfo.InvariantCulture));
        observations.Add("touch.string=" + first);

        TouchLocation[] source = [first];
        var collection = new TouchCollection(source);
        source[0] = new TouchLocation(99, TouchLocationState.Released, Vector2.Zero);
        observations.Add("touch.collection.clone=" + collection[0].Id.ToString(CultureInfo.InvariantCulture));
        observations.Add("touch.collection.contains=" + Flag(collection.Contains(sameCoordinates)));
        observations.Add("touch.collection.oob=" + ExceptionName(() => _ = collection[1]));

        return observations;
    }

    private static int Flag(bool value) => value ? 1 : 0;

    private static string Bits(float value) =>
        unchecked((uint)BitConverter.ToInt32(BitConverter.GetBytes(value), 0))
            .ToString("X8", CultureInfo.InvariantCulture);

    private static string ExceptionName(Action action)
    {
        try
        {
            action();
            return "none";
        }
        catch (Exception exception)
        {
            return exception.GetType().Name;
        }
    }
}
