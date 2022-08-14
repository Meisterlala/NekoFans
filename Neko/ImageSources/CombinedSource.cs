using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources;

public class CombinedSource : IImageSource
{
    public List<IImageSource> sources = new();
    private readonly Random random = new();

    public CombinedSource(params IImageSource[] source)
    {
        foreach (var s in source)
        {
            AddSource(s);
        }
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
        if (source == null)
            return;
        sources.Add(source);
    }

    public bool RemoveSource(IImageSource source) => sources.Remove(source);
    public void RemoveAll(Type source)
    {
        sources.RemoveAll((e) =>
            e.GetType() == source);
        sources.ForEach((e) =>
        {
            if (e.GetType() == typeof(CombinedSource))
                ((CombinedSource)e).RemoveAll(source);
        });
        sources.RemoveAll((e) =>
            e.GetType() == typeof(CombinedSource) && ((CombinedSource)e).Count() == 0);
    }
    public bool Contains(Type source) => sources.Find((e) => e.GetType() == source) != null;
    public int Count() => sources.Count;
    public List<IImageSource> GetSources() => sources;


    public override string ToString()
    {
        var res = $"Loaded image sources: {sources.Count}\n";
        foreach (var s in sources)
        {
            if (s.GetType() == typeof(CombinedSource))
            {
                var c = s.ToString() ?? "";
                res += c[(c.IndexOf("\n") + 1)..];
            }
            else
            {
                res += s.ToString() + "\n";
            }
        }
        return res;
    }

    public IImageSource? LoadConfig(object config)
    {
        throw new NotImplementedException();
    }
}
