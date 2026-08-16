namespace Microsoft.Xna.Framework;

/// <summary>
/// XNA 4.0-compatible <c>GameTime</c>. Real XNA's <c>GameTime</c> is a mutable class with settable
/// properties (unlike CNA's immutable <c>GameTime</c> struct); this matches XNA's shape
/// exactly for source compatibility.
/// </summary>
public class GameTime
{
    public GameTime()
    {
    }

    public GameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime)
    {
        TotalGameTime = totalGameTime;
        ElapsedGameTime = elapsedGameTime;
    }

    public GameTime(TimeSpan totalRealTime, TimeSpan elapsedRealTime, bool isRunningSlowly)
    {
        TotalGameTime = totalRealTime;
        ElapsedGameTime = elapsedRealTime;
        IsRunningSlowly = isRunningSlowly;
    }

    public TimeSpan ElapsedGameTime { get; set; }
    public bool IsRunningSlowly { get; set; }
    public TimeSpan TotalGameTime { get; set; }

    internal static GameTime FromFramework(CNA.GameTime framework) =>
        new(framework.TotalGameTime, framework.ElapsedGameTime, framework.IsRunningSlowly);
}
