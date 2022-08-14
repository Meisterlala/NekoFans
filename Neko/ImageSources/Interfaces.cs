using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources;

/// <summary>
/// A source for new Images form an API
/// </summary>
public interface IImageSource
{
    /// <summary>
    /// Load the next image form the web to ram, not to vram yet
    /// </summary>
    public Task<NekoImage> Next(CancellationToken ct = default);
}

/// <summary>
/// Describes how to load a config to generate a class
/// </summary>
public interface IImageConfig
{
    public IImageSource? LoadConfig();

}

/// <summary>
/// Convert self to List<string>
/// </summary>
public interface IJsonToList
{
    public List<string> ToList();
}