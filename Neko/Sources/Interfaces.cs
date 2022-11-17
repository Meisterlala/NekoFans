using System.Collections.Generic;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources;

#pragma warning disable CA1716 // I dont care about InterOp

/// <summary>
/// A source for new Images from an API
/// </summary>
public abstract class ImageSource
{
    /// <summary>
    /// Load the next image form the web to ram, not to vram yet
    /// </summary>
    public abstract NekoImage Next(CancellationToken ct = default);

    /// <summary>
    /// Indicates if the source is faulted and should not be used anymore
    /// </summary>
    public bool Faulted { get; set; }
    public int FaultCountMax { get; set; } = 5;
    private int FaultedCount { get; set; }

    /// <summary>
    /// Increase the fault count and check if the source should be faulted
    /// </summary>
    public void FaultedIncrement()
    {
        FaultedCount++;
        if (FaultedCount > FaultCountMax)
            Faulted = true;
    }

    /// <summary>
    /// Reset the faulted counter
    /// </summary>
    public void FaultedReset()
    {
        FaultedCount = 0;
        Faulted = false;
    }

    /// <summary>
    /// Load the next image, unless the source is faulted
    /// </summary>
    public NekoImage? NextChecked(CancellationToken ct = default) => Faulted ? null : Next(ct);

    /// <summary>
    /// A string representation of the source
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Information about the source
    /// </summary>
    public abstract override string ToString();

    public string ToStringWithFaulted()
    {
        return Faulted
            ? $"[F]{ToString()}"
            : FaultedCount > 0
            ? $"[{FaultedCount}]{ToString()}"
            : ToString();
    }

    /// <summary>
    /// Compare two sources of the same type
    /// </summary>
    public abstract bool SameAs(ImageSource other);

    public sealed override bool Equals(object? obj)
        => obj is ImageSource other && other.GetType() == GetType() && SameAs(other);

    public sealed override int GetHashCode() => Name.GetHashCode();
}

/// <summary>
/// Describes how to load a config to generate a class
/// </summary>
public interface IImageConfig
{
    public ImageSource? LoadConfig();
}

/// <summary>
/// Convert self to List<T>
/// </summary>
public interface IJsonToList<T>
{
    public List<T> ToList();
}
