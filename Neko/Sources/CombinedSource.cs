using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Sources;

/// <summary>
/// Combines multible <see cref="IImageSource"/> to one.
/// A random Image source is choosen, when <see cref="Next"/> is called
/// </summary>
public class CombinedSource : IImageSource
{
    private readonly List<IImageSource> sources = new();
    private readonly Random random = new();

    public CombinedSource(params IImageSource[] source)
    {
        foreach (var s in source)
        {
            AddSource(s);
        }
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

    public bool RemoveSource(IImageSource source)
    {
        if (sources.Remove(source))
            return true;
        foreach (var s in sources)
        {
            if (s is CombinedSource cs)
            {
                if (cs.RemoveSource(source))
                    return true;
            }
        }
        return false;
    }

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
    public bool Contains(Type source)
    {
        return sources.Exists((e) => e.GetType() == source)
        || sources.Exists((e) => e.GetType() == typeof(CombinedSource)
        && ((CombinedSource)e).Contains(source));
    }

    public List<T> GetAll<T>()
    {
        var list = new List<T>();
        foreach (var s in sources)
        {
            if (s is T t)
                list.Add(t);
            if (s is CombinedSource c)
                list.AddRange(c.GetAll<T>());
        }
        return list;
    }

    public int Count()
    {
        var count = sources.Count;
        sources.ForEach((s) =>
        {
            if (s.GetType() == typeof(CombinedSource))
                count += ((CombinedSource)s).Count() - 1;
        });
        return count;
    }

    public override string ToString()
    {
        var res = $"Loaded image sources: {Count()}\n";
        foreach (var s in sources)
        {
            if (s.GetType() == typeof(CombinedSource))
            {
                var c = s.ToString() ?? "";
                c = c[(c.IndexOf("\n") + 1)..];
                res += "|--- " + c.Replace("\n", "\n|--- ");
                res = res[..^5];
            }
            else
            {
                res += s.ToString() + "\n";
            }
        }
        return res;
    }

    public IImageSource? LoadConfig(object _) => throw new NotImplementedException();
}
