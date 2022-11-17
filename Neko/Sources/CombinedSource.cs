using System;
using System.Collections.Generic;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources;

/// <summary>
/// Combines multible <see cref="ImageSource"/> to one.
/// A random Image source is choosen, when <see cref="Next"/> is called
/// </summary>
public class CombinedSource : ImageSource
{
    public override string Name => "Combined Source";

    private readonly List<ImageSource> sources = new();
    private readonly Random random = new();

    public CombinedSource(params ImageSource[] source)
    {
        foreach (var s in source)
        {
            AddSource(s);
        }
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
        var nonFaulted = sources.FindAll(s => !s.Faulted);

        if (nonFaulted.Count == 0)
        {
            Faulted = true;
            if (sources.Count > 0)
                throw new Exception("All Image sources of one Type are faulted");
            else
                throw new Exception("No Image sources are available");
        }

        var i = random.Next(0, nonFaulted.Count);
        return nonFaulted[i].Next(ct);
    }

    public void AddSource(ImageSource? source)
    {
        if (source == null)
            return;

        if (source is CombinedSource comb && comb.sources.Count == 0)
            return;

        var combined = GetAll<CombinedSource>();
        CombinedSource? exisiting = null;

        if (source is CombinedSource cs)
        {
            exisiting = combined.Find(c => c.sources.Exists(
                s => cs.Contains((a) => s.GetType() == a.GetType())));
            if (exisiting != null)
            {
                exisiting.sources.AddRange(cs.sources);
                exisiting.Faulted = false;
            }
            else
            {
                sources.Add(cs);
            }
        }
        else
        {
            exisiting = combined.Find(c => c.sources.Exists(
                s => s.GetType() == source.GetType()));
            // Chek if there is a source, which contains the same type
            if (exisiting != null)
            {
                exisiting.sources.Add(source);
                exisiting.Faulted = false;
            }
            else
            {
                sources.Add(source);
            }
        }
    }

    public bool RemoveSource(ImageSource source)
    {
        bool HasBeenRemoved()
        {
            if (sources.Remove(source))
                return true;
            foreach (var s in sources)
            {
                if (s is CombinedSource cs && cs.RemoveSource(source))
                    return true;
            }
            return false;
        }
        if (HasBeenRemoved())
        {
            sources.RemoveAll((e) =>
            e is CombinedSource cs && cs.Count() == 0);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void RemoveAll(Type type)
    {
        // Remove type from List
        sources.RemoveAll((e)
            => e.GetType() == type);
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

    public void RemoveAll(Predicate<ImageSource> match)
    {
        sources.RemoveAll(match);
        sources.ForEach((e) =>
        {
            if (e is CombinedSource cs)
                cs.RemoveAll(match);
        });
        sources.RemoveAll((e) =>
            e is CombinedSource cs && cs.Count() == 0);
    }

    public bool Contains(Type source)
    {
        return sources.Exists((e) => e.GetType() == source)
        || sources.Exists((e) => e.GetType() == typeof(CombinedSource)
        && ((CombinedSource)e).Contains(source));
    }

    public bool Contains(Predicate<ImageSource> predicate)
    {
        return sources.Exists(predicate)
        || sources.Exists((e) => e is CombinedSource cs && cs.Contains(predicate));
    }

    public bool Contains(ImageSource source) => Contains((e) => e.SameAs(source));

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
        }
        return list;
    }

    public List<ImageSource> GetAll(Predicate<ImageSource> predicate)
    {
        var list = new List<ImageSource>();
        foreach (var s in sources)
        {
            if (predicate(s))
                list.Add(s);
            else if (s is CombinedSource c)
                list.AddRange(c.GetAll(predicate));
        }
        return list;
    }

    public List<ImageSource> GetAll(Type t) => GetAll((e) => e.GetType() == t);

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
        // Remove all sources that are not in other
        RemoveAll((e) =>
        {
            if (e.GetType() != typeof(CombinedSource) && !other.Contains(e))
            {
                Dalamud.Logging.PluginLog.LogDebug($"Removing {e.Name} from ImageSource");
                return true;
            }
            return false;
        });

        // Add source and print log
        void AddIfDoesntContain(ImageSource source, bool wrapInCS = false)
        {
            if (!Contains(source))
            {
                Dalamud.Logging.PluginLog.LogDebug($"Added {source.Name} as ImageSource");
                if (wrapInCS)
                    AddSource(new CombinedSource(source));
                else
                    AddSource(source);
            }
        }
        // Add all sources that are not in this
        foreach (var s in other.sources)
        {
            if (s is CombinedSource cs)
            {
                foreach (var child in cs.sources)
                    AddIfDoesntContain(child, true);
            }
            else
            {
                AddIfDoesntContain(s);
            }
        }
    }

    public override string ToString()
    {
        var res = $"Loaded image sources: {Count()} {(Faulted ? " (Faulted)" : "")}\n";
        foreach (var s in sources)
        {
            if (s.GetType() == typeof(CombinedSource))
            {
                var c = s.ToStringWithFaulted() ?? "UNKNOWN"; // toString
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

    public ImageSource? LoadConfig(object _) => throw new NotImplementedException();

    public override bool SameAs(ImageSource other) =>
        other is CombinedSource cs
        && cs.sources.TrueForAll(Contains)
        && sources.TrueForAll(cs.Contains);

    public void ResetFaultySources()
    {
        sources.ForEach((e) =>
        {
            if (e is CombinedSource cs)
            {
                cs.Faulted = false;
                cs.ResetFaultySources();
            }
            else
            {
                e.FaultedReset();
            }
        });
    }
}
