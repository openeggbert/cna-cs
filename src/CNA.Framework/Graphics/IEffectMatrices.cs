namespace CNA.Graphics;

/// <summary>
/// Real XNA interface for effects that expose <see cref="World"/>/<see cref="View"/>/
/// <see cref="Projection"/> transforms. Confirmed against the real openeggbert/cna C++ engine's
/// own <c>IEffectMatrices</c> (three pure-virtual get/set pairs) -- not invented. <see cref="Model.Draw"/>
/// is the reason this exists in this project: it needs to set these three matrices on whatever
/// effect each mesh part uses without knowing the effect's concrete type.
/// </summary>
public interface IEffectMatrices
{
    Matrix World { get; set; }

    Matrix View { get; set; }

    Matrix Projection { get; set; }
}
