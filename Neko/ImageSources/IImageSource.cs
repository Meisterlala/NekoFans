using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources
{
    public interface IImageSource
    {
        /// <summary>
        /// Load the next image form the web to ram, not to vram yet
        /// </summary>
        public Task<NekoImage> Next(CancellationToken ct = default);
    }
}