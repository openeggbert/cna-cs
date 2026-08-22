namespace Microsoft.Xna.Framework.Input;

public readonly struct GamePadState
{
    private readonly Buttons _rawButtons;

    public bool IsConnected { get; }
    public GamePadButtons Buttons { get; }
    public GamePadDPad DPad { get; }
    public GamePadThumbSticks ThumbSticks { get; }
    public GamePadTriggers Triggers { get; }
    public int PacketNumber { get; }

    public GamePadState(
        GamePadThumbSticks thumbSticks,
        GamePadTriggers triggers,
        GamePadButtons buttons,
        GamePadDPad dPad)
    {
        IsConnected = true;
        PacketNumber = 0;
        ThumbSticks = thumbSticks;
        Triggers = triggers;
        Buttons = buttons;
        DPad = dPad;
        _rawButtons = BuildRawButtons(thumbSticks, triggers, buttons, dPad);
    }

    public GamePadState(
        Vector2 leftThumbStick,
        Vector2 rightThumbStick,
        float leftTrigger,
        float rightTrigger,
        params Buttons[] buttons)
    {
        Buttons rawButtons = 0;
        if (buttons is not null)
        {
            foreach (Buttons button in buttons)
            {
                rawButtons |= button;
            }
        }

        IsConnected = true;
        PacketNumber = 0;
        ThumbSticks = new GamePadThumbSticks(leftThumbStick, rightThumbStick);
        Triggers = new GamePadTriggers(leftTrigger, rightTrigger);
        Buttons = new GamePadButtons(rawButtons);
        DPad = new GamePadDPad(
            ToState(rawButtons, global::Microsoft.Xna.Framework.Input.Buttons.DPadUp),
            ToState(rawButtons, global::Microsoft.Xna.Framework.Input.Buttons.DPadDown),
            ToState(rawButtons, global::Microsoft.Xna.Framework.Input.Buttons.DPadLeft),
            ToState(rawButtons, global::Microsoft.Xna.Framework.Input.Buttons.DPadRight));
        // XNA's constructor stores only the physical buttons represented by its internal
        // XInput state. Virtual thumbstick/trigger flags supplied in the params array, and
        // undefined bits, do not become pressed unless the analog values themselves cross the
        // corresponding dead zone.
        _rawButtons = BuildRawButtons(ThumbSticks, Triggers, Buttons, DPad);
    }

    internal GamePadState(CNA.Input.GamePadState framework)
    {
        IsConnected = framework.IsConnected;
        PacketNumber = framework.PacketNumber;
        Buttons = new GamePadButtons(framework.Buttons);
        DPad = new GamePadDPad(framework.DPad);
        ThumbSticks = new GamePadThumbSticks(framework.ThumbSticks);
        Triggers = new GamePadTriggers(framework.Triggers);
        _rawButtons = BuildRawButtons(ThumbSticks, Triggers, Buttons, DPad);
    }

    public bool IsButtonDown(Buttons button) => (_rawButtons & button) == button;

    public bool IsButtonUp(Buttons button) => !IsButtonDown(button);

    public override bool Equals(object? obj) => obj is GamePadState other &&
        IsConnected == other.IsConnected && PacketNumber == other.PacketNumber &&
        ThumbSticks == other.ThumbSticks && Triggers == other.Triggers &&
        Buttons == other.Buttons && DPad == other.DPad;

    public override int GetHashCode() =>
        ThumbSticks.GetHashCode() ^ Triggers.GetHashCode() ^ Buttons.GetHashCode() ^
        IsConnected.GetHashCode() ^ DPad.GetHashCode() ^ PacketNumber.GetHashCode();

    public override string ToString() => $"{{IsConnected:{IsConnected}}}";

    public static bool operator ==(GamePadState left, GamePadState right) => left.Equals(right);

    public static bool operator !=(GamePadState left, GamePadState right) => !left.Equals(right);

    private static Buttons BuildRawButtons(
        GamePadThumbSticks thumbSticks,
        GamePadTriggers triggers,
        GamePadButtons buttons,
        GamePadDPad dPad)
    {
        Buttons result = BuildAnalogButtons(thumbSticks, triggers);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.A, buttons.A);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.B, buttons.B);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.X, buttons.X);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.Y, buttons.Y);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.Back, buttons.Back);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.Start, buttons.Start);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.BigButton, buttons.BigButton);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.LeftShoulder, buttons.LeftShoulder);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.RightShoulder, buttons.RightShoulder);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.LeftStick, buttons.LeftStick);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.RightStick, buttons.RightStick);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.DPadUp, dPad.Up);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.DPadDown, dPad.Down);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.DPadLeft, dPad.Left);
        Add(ref result, global::Microsoft.Xna.Framework.Input.Buttons.DPadRight, dPad.Right);
        return result;
    }

    private static Buttons BuildAnalogButtons(GamePadThumbSticks thumbSticks, GamePadTriggers triggers)
    {
        Buttons result = 0;
        short leftX = (short)(thumbSticks.Left.X * 32767f);
        short leftY = (short)(thumbSticks.Left.Y * 32767f);
        short rightX = (short)(thumbSticks.Right.X * 32767f);
        short rightY = (short)(thumbSticks.Right.Y * 32767f);
        byte leftTrigger = (byte)(triggers.Left * 255f);
        byte rightTrigger = (byte)(triggers.Right * 255f);

        if (leftX < -7849) result |= global::Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickLeft;
        if (leftX > 7849) result |= global::Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickRight;
        if (leftY < -7849) result |= global::Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickDown;
        if (leftY > 7849) result |= global::Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickUp;
        if (rightX < -8689) result |= global::Microsoft.Xna.Framework.Input.Buttons.RightThumbstickLeft;
        if (rightX > 8689) result |= global::Microsoft.Xna.Framework.Input.Buttons.RightThumbstickRight;
        if (rightY < -8689) result |= global::Microsoft.Xna.Framework.Input.Buttons.RightThumbstickDown;
        if (rightY > 8689) result |= global::Microsoft.Xna.Framework.Input.Buttons.RightThumbstickUp;
        if (leftTrigger > 30) result |= global::Microsoft.Xna.Framework.Input.Buttons.LeftTrigger;
        if (rightTrigger > 30) result |= global::Microsoft.Xna.Framework.Input.Buttons.RightTrigger;
        return result;
    }

    private static void Add(ref Buttons result, Buttons flag, ButtonState state)
    {
        if (state == ButtonState.Pressed)
        {
            result |= flag;
        }
    }

    private static ButtonState ToState(Buttons value, Buttons flag) =>
        (value & flag) != 0 ? ButtonState.Pressed : ButtonState.Released;
}
