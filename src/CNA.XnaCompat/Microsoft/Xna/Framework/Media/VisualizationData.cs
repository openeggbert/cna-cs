namespace Microsoft.Xna.Framework.Media;

/// <summary>XNA 4.0-compatible <c>VisualizationData</c>. Extends <c>CNA.Media.VisualizationData</c>
/// directly, fully inherited-unchanged -- <c>Frequencies</c>/<c>Samples</c> are plain
/// <c>float[]</c> with no compat-type-crossing concern at all (no other <c>CNA</c> type is
/// referenced anywhere in this type), so there's nothing here that needs overriding, the same
/// "trivial subclass" shape <c>ModelMeshPart</c>'s own compat mirror already established.</summary>
public sealed class VisualizationData : CNA.Media.VisualizationData
{
}
