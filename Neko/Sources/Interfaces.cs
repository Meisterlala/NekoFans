using System;
using System.Collections.Generic;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources;

#pragma warning disable CA1716 // I dont care about InterOp

/// <summary>
/// A source for new Images from an API
/// </summary>
public interface IImageSource : IEquatable<IImageSource>
{
    /// <summary>
    /// Load the next image form the web to ram, not to vram yet
    /// </summary>
    public NekoImage Next(CancellationToken ct = default);

    /// <summary>
    /// Indicates if the source is faulted and should not be used anymore
    /// </summary>
    public bool Faulted { get; set; }

    /// <summary>
    /// A string representation of the source
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// A string representation of the source
    /// </summary>
    public string ToString();
}

/// <summary>
/// Describes how to load a config to generate a class
/// </summary>
public interface IImageConfig
{
    public IImageSource? LoadConfig();
}

/// <summary>
/// Convert self to List<T>
/// </summary>
public interface IJsonToList<T>
{
    public List<T> ToList();
}
