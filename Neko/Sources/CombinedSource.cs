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
    public bool Faulted { get; set; }
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
        var nonFaulted = sources.FindAll(s => !s.Faulted);

        if (nonFaulted.Count == 0)
        {
            Faulted = true;
            if (sources.Count > 0)
                throw new Exception("All Image sources are faulted");
            else
                throw new Exception("No Image sources are available");
        }

        var i = random.Next(0, nonFaulted.Count);
        return nonFaulted[i].Next(ct);
    }

    public void AddSource(IImageSource? source)
    {
        if (source == null)
            return;

        if (source is CombinedSource combinedSource)
            sources.Add(combinedSource);
        else if (source is FaultCheck faultCheck)
            sources.Add(faultCheck);
        else
            sources.Add(FaultCheck.Wrap(source));
    }

    public bool RemoveSource(IImageSource source)
    {
        if (sources.Remove(source))
            return true;
        foreach (var s in sources)
        {
            if (s is CombinedSource cs && cs.RemoveSource(source))
                return true;
            if (s is FaultCheck fc && fc.UnWrap() == source)
                return true;
        }
        return false;
    }

    public void RemoveAll(Type type)
    {
        if (type == typeof(FaultCheck))
            throw new Exception("Cant remove FaultCheck type");
        // Remove type from List
        sources.RemoveAll((e) =>
            e.GetType() == type
            || (e is FaultCheck fc && fc.UnWrap().GetType() == type));
        // Remove Combindes Sources children recursivly
        sources.ForEach((e) =>
        {
            if (e is CombinedSource cs)
                cs.RemoveAll(type);
        });
        // Remove empty Combined Sources
        sources.RemoveAll((e) =>
            e is CombinedSource cs && cs.Count() == 0);
    }

    public bool Contains(Type source)
    {
        return sources.Exists((e) => e.GetType() == source)
        || sources.Exists((e) => e.GetType() == typeof(CombinedSource)
        && ((CombinedSource)e).Contains(source));
    }

    public bool Contains(Predicate<IImageSource> predicate)
    {
        return sources.Exists(predicate)
        || sources.Exists((e) => e is CombinedSource cs && cs.Contains(predicate));
    }

    public bool Contains(IImageSource source) => Contains((e) => e.Equals(source));

    public bool ContainsNonFaulted() => Contains((e) => !e.Faulted);

    public List<T> GetAll<T>()
    {
        var list = new List<T>();
        foreach (var s in sources)
        {
            if (s is T t)
                list.Add(t);
            else if (s is CombinedSource c)
                list.AddRange(c.GetAll<T>());
            else if (s is FaultCheck f && f.UnWrap() is T unwrapped)
                list.Add(unwrapped);
        }
        return list;
    }

    public List<IImageSource> GetAll(Predicate<IImageSource> predicate)
    {
        var list = new List<IImageSource>();
        foreach (var s in sources)
        {
            if (predicate(s))
                list.Add(s);
            else if (s is CombinedSource c)
                list.AddRange(c.GetAll(predicate));
            else if (s is FaultCheck f && predicate(f.UnWrap()))
                list.Add(s);
        }
        return list;
    }

    public List<IImageSource> GetAll(Type t) => GetAll((e) => e.GetType() == t);

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

    public void UpdateFrom(CombinedSource other)
    {
        // Remove all sources that are not in source
        sources.RemoveAll((e) => !other.Contains(e));
        // Add all sources that are not in this
        foreach (var s in other.sources)
        {
            if (!Contains(s))
                AddSource(s);
        }
    }

    public override string ToString()
    {
        var res = $"Loaded image sources: {Count()}\n";
        foreach (var s in sources)
        {
            if (s.GetType() == typeof(CombinedSource))
            {
                var c = s.ToString() ?? ""; // toString
                var lines = c.Split('\n'); // Split by lines
                for (var i = 1; i < lines.Length; i++) // Ignore first line
                {
                    // Draw box in front of lines
                    if (i == 1)
                        res += "┌─" + lines[i] + "\n";
                    else if (i < lines.Length - 1)
                        res += "├─" + lines[i] + "\n";
                    else
                        res += "└─" + lines[i] + "\n";
                }
            }
            else
            {
                res += s.ToString() + "\n";
            }
        }
        // Remove last newline
        return res[..^1];
    }

    public IImageSource? LoadConfig(object _) => throw new NotImplementedException();

    public bool Equals(IImageSource? other) =>
        other != null
        && other is CombinedSource cs
        && cs.sources.TrueForAll((e) => Contains(e))
        && sources.TrueForAll((e) => cs.Contains(e));
}
