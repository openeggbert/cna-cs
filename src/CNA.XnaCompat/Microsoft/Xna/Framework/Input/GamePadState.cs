namespace Microsoft.Xna.Framework.Input;

public readonly struct GamePadState
{
    private readonly CNA.Framework.Input.GamePadState _framework;

    public bool IsConnected { get; }
    public GamePadButtons Buttons { get; }
    public GamePadDPad DPad { get; }
    public GamePadThumbSticks ThumbSticks { get; }
    public GamePadTriggers Triggers { get; }
    public int PacketNumber => _framework.PacketNumber;

    internal GamePadState(CNA.Framework.Input.GamePadState framework)
    {
        _framework = framework;
        IsConnected = framework.IsConnected;
        Buttons = new GamePadButtons(framework.Buttons);
        DPad = new GamePadDPad(framework.DPad);
        ThumbSticks = new GamePadThumbSticks(framework.ThumbSticks);
        Triggers = new GamePadTriggers(framework.Triggers);
    }

    public bool IsButtonDown(Buttons button) => _framework.IsButtonDown((CNA.Framework.Input.Buttons)(uint)button);

    public bool IsButtonUp(Buttons button) => !IsButtonDown(button);
}
