namespace Microsoft.Xna.Framework.Graphics;

/// <summary>XNA 4.0-compatible effect facade backed by one CNA effect instance.</summary>
public class Effect : GraphicsResource
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CNA.Graphics.Effect, Effect>
        FrameworkFacades = new();

    private EffectParameterCollection? _parameters;
    private EffectTechniqueCollection? _techniques;

    private protected Effect(GraphicsDevice graphicsDevice, CNA.Graphics.Effect inner)
        : base(graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(inner);
        Inner = inner;
        FrameworkFacades.Add(inner, this);
    }

    public Effect(GraphicsDevice graphicsDevice, byte[] effectCode)
        : this(graphicsDevice, new CNA.Graphics.Effect(graphicsDevice, effectCode))
    {
    }

    protected Effect(Effect cloneSource)
        : this(
            (cloneSource ?? throw new ArgumentNullException(nameof(cloneSource))).GraphicsDevice,
            cloneSource.Inner.Clone())
    {
    }

    internal static Effect Adopt(GraphicsDevice graphicsDevice, CNA.Graphics.Effect inner) =>
        new(graphicsDevice, inner);

    internal static Effect? FromFramework(CNA.Graphics.Effect? inner)
    {
        if (inner is null)
        {
            return null;
        }

        return FrameworkFacades.TryGetValue(inner, out Effect? facade)
            ? facade
            : throw new InvalidOperationException(
                "A CNA effect cannot be exposed through the strict XNA facade.");
    }

    internal CNA.Graphics.Effect Inner { get; }

    public EffectParameterCollection Parameters =>
        _parameters ??= new EffectParameterCollection(Inner.Parameters);

    public EffectTechniqueCollection Techniques =>
        _techniques ??= new EffectTechniqueCollection(Inner.Techniques);

    public EffectTechnique CurrentTechnique
    {
        get => new(Inner.CurrentTechnique);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            Inner.CurrentTechnique = value.Framework;
        }
    }

    public virtual Effect Clone() => new(this);

    protected internal virtual void OnApply() => Inner.Apply();

    protected override void Dispose(bool arg0)
    {
        if (IsDisposed)
        {
            return;
        }

        Inner.Dispose();
        base.Dispose(arg0);
    }
}
