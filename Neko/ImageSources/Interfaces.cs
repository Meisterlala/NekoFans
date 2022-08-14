using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources;


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
