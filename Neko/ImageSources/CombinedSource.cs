using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources
{
    public class CombinedSource : IImageSource
    {
        public List<IImageSource> sources = new();
        private readonly Random random = new();

        public CombinedSource(IImageSource source)
        {
            AddSource(source);
        }

        public CombinedSource()
        {
        }

        public Task<NekoImage> Next(CancellationToken ct = default)
        {
            if (sources.Count <= 0)
                return NekoImage.DefaultNeko();

            var i = random.Next(0, sources.Count);
            return sources[i].Next(ct);
        }

        public void AddSource(IImageSource? source)
        {
            if (source != null)
                sources.Add(source);
        }

        public bool RemoveSource(IImageSource source) => sources.Remove(source);
        public void RemoveAll(Type source) => sources.RemoveAll((e) => e.GetType() == source);
        public bool Contains(Type source) => sources.Find((e) => e.GetType() == source) != null;
        public int Count() => sources.Count;


        public override string ToString()
        {
            var res = "";
            foreach (var s in sources)
            {
                res += s.ToString() + "\n";
            }
            return res;
        }

        public IImageSource? LoadConfig(object config)
        {
            throw new NotImplementedException();
        }
    }
}