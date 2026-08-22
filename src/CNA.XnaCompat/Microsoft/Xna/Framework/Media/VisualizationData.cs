using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Media;

/// <summary>Holds frequency and sample data for the currently playing song.</summary>
public class VisualizationData
{
    private readonly CNA.Media.VisualizationData _data = new();

    public VisualizationData()
    {
        Frequencies = Array.AsReadOnly(_data.Frequencies);
        Samples = Array.AsReadOnly(_data.Samples);
    }

    internal CNA.Media.VisualizationData Framework => _data;

    public ReadOnlyCollection<float> Frequencies { get; }

    public ReadOnlyCollection<float> Samples { get; }
}
