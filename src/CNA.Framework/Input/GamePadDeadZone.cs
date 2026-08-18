namespace CNA.Input;

/// <summary>Matches real XNA's <c>GamePadDeadZone</c> values exactly -- also confirmed against the
/// real, shipped openeggbert/cna C API's own <c>CNA_GAMEPAD_DEAD_ZONE_*</c> constants
/// (<c>input.h:392-399</c>).</summary>
public enum GamePadDeadZone
{
    None = 0,
    IndependentAxes = 1,
    Circular = 2,
}
