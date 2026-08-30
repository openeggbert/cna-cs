namespace CNA.Content.Xnb;

/// <summary>
/// An external reference read by XNA's <c>ExternalReferenceReader</c>, carrying the resolved asset
/// name rather than the loaded object.
///
/// <b>Why a type rather than a bare string.</b> The value lands in an <c>EffectMaterial</c>'s
/// parameter dictionary alongside genuine values, and a parameter can legitimately be a string.
/// Handing back a <see cref="string"/> would make "the asset named Textures\lizard_normal" and "the
/// literal text Textures\lizard_normal" the same value, and the builder would have to guess which
/// one a parameter wanted from the parameter's declared class -- a guess that is wrong for any
/// string parameter whose value happens to name a file.
///
/// XNA has no equivalent type because it resolves the reference immediately, into the object
/// itself. This layer stops at the name for the reason
/// <see cref="XnbContentReader.ReadExternalReference"/> gives.
/// </summary>
/// <param name="AssetName">The resolved content asset name -- not a file-system path.</param>
internal sealed record XnbExternalReference(string AssetName);
