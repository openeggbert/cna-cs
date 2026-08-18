namespace Microsoft.Xna.Framework.Audio;

/// <summary>XNA 4.0-compatible <c>AudioEngine</c>. A pure subclass -- only
/// <see cref="GetCategory"/> needs re-typing.</summary>
public class AudioEngine : CNA.Audio.AudioEngine
{
    public AudioEngine(string settingsFile)
        : base(settingsFile)
    {
    }

    public new AudioCategory GetCategory(string name) => new(base.GetCategory(name));

    /// <summary>Re-typed so <c>RendererDetails</c> answers this namespace's own
    /// <see cref="RendererDetail"/>. Without it that type would be unreachable -- the base property
    /// returns <c>CNA.Audio.RendererDetail</c>, and a compat game has no way to get from one to the
    /// other.</summary>
    public new IReadOnlyList<RendererDetail> RendererDetails
    {
        get
        {
            IReadOnlyList<CNA.Audio.RendererDetail> source = base.RendererDetails;
            var wrapped = new RendererDetail[source.Count];
            for (int i = 0; i < wrapped.Length; i++)
            {
                wrapped[i] = new RendererDetail(source[i]);
            }

            return wrapped;
        }
    }
}
