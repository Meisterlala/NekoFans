using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiScene;

namespace Neko.Drawing;

public class NekoImage
{
    public class Frame
    {
        public byte[]? Data { get; private set; }
        public int FrameDelay { get; }
        public TextureWrap? Texture { get; set; }

        public Frame(byte[] data, int frameDelay)
        {
            Data = data;
            FrameDelay = frameDelay;
        }

        ~Frame()
        {
            Texture?.Dispose();
            Texture = null;
            Data = null;
        }

        public void ClearData()
            => Data = null;
    }

    public enum State
    {
        Error,
        Downloading,
        Downloaded,
        Decoded,
        LoadedGPU, // Decoded and loaded into GPU VRAM
    }

    public State CurrentState { get; private set; } = State.Error;
    public string? DebugInfo { get; set; }
    public string? Description { get; set; }
    public string? URLDownloadWebsite { get; set; }
    public string? URLOpenOnClick { get; set; }
    public Sources.ImageSource ImageSource { get; }

    public byte[]? EncodedData { get; private set; }
    public List<Frame>? Frames { get; private set; }
    public int CycleTime { get; private set; } // in ms: When the animation should loop
    public int? Width { get; private set; }
    public int? Height { get; private set; }

    public long RAMUsage =>
        CurrentState == State.Downloading || EncodedData == null
        ? 0
        : EncodedData.LongLength;

    public long VRAMUsage
    {
        get
        {
            if (Frames == null || Frames.Count == 0)
                return 0;

            long res = 0;
            foreach (var frame in Frames)
            {
                res += frame.Data?.Length ?? 0;
            }
            return res;
        }
    }

    public NekoImage(Func<NekoImage, Task<Sources.Download.Response>> downloadTask, Sources.ImageSource source)
    {
        CurrentState = State.Downloading;
        ImageSource = source;
        Task.Run(async () =>
        {
            try
            {
                var task = downloadTask(this);
                var response = await task;

                EncodedData = response.Data;
                URLDownloadWebsite = response.Url;
                CurrentState = State.Downloaded;
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        });
    }

    public NekoImage(byte[] data, Sources.ImageSource source)
    {
        ImageSource = source;
        LoadData(data);
    }

    ~NekoImage()
    {
        CurrentState = State.Error;
        EncodedData = null;

        Frames?.Clear();
        Frames = null;
    }

    public void LoadData(byte[] data)
    {
        CurrentState = State.Downloaded;
        EncodedData = data;
    }

    public override string ToString()
    {
        var res = CurrentState switch
        {
            State.Error => "[Error]",
            State.Downloading => "[Downloading]",
            State.Downloaded => "[Downloaded]",
            State.Decoded => "[Decoded]",
            State.LoadedGPU => "[LoadedGPU]",
            _ => "[Unknown]",
        };

        if (RAMUsage != 0)
            res += $" Data: {Helper.SizeSuffix(RAMUsage)}";
        if (VRAMUsage != 0)
            res += $" Texture: {Helper.SizeSuffix(VRAMUsage)}";
        if (Frames?.Count > 1)
            res += $"\nFrames: {Frames.Count}";
        if (!string.IsNullOrEmpty(URLDownloadWebsite))
            res += $"\nURL: {Helper.EndWithEllipsis(URLDownloadWebsite, 75)}";
        if (DebugInfo?.Length > 0)
            res += $"\nDebugInfo: {DebugInfo}";
        res += $"\nSource: {ImageSource}";

        return res;
    }

    private void Decode()
    {
        DebugHelper.Assert(CurrentState == State.Downloaded, "Image is not downloaded");
        var decoded = ImageDecode.DecodeImageFrames(EncodedData!);
        Frames = decoded.Frames;
        Width = decoded.Width;
        Height = decoded.Height;

        // Sum all the frame delays to get the cycle time
        // You could add a delay here to make the animation pause for a bit
        foreach (var f in Frames)
        {
            CycleTime += f.FrameDelay;
        }

        CurrentState = State.Decoded;
    }

    private void LoadGPU()
    {
        DebugHelper.Assert(CurrentState == State.Decoded, "Image is not decoded");
        DebugHelper.Assert(Frames != null, "Image has no frames");

        var textures = ImageLoad.LoadFrames(this);
        for (var i = 0; i < Frames!.Count; i++)
        {
            Frames[i].Texture = textures[i];
        }
        CurrentState = State.LoadedGPU;
    }

    public Task DecodeAndLoadGPUAsync(CancellationToken ct = default)
        => Task.Run(() => { Decode(); LoadGPU(); }, ct);

    public Task Await(State state, CancellationToken ct = default)
        => Task.Run(() =>
        {
            while (CurrentState != state)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(10);
            }
        }, ct);

    public Task Await(Predicate<State> predicate, CancellationToken ct = default)
    => Task.Run(() =>
    {
        while (!predicate(CurrentState))
        {
            ct.ThrowIfCancellationRequested();
            Thread.Sleep(10);
        }
    }, ct);

    public Task RequestLoadGPU(CancellationToken ct = default)
    {
        return CurrentState == State.LoadedGPU
            ? Task.CompletedTask
            : Task.Run(async () =>
            {
                if (CurrentState == State.Downloading)
                    await Await(State.Downloaded, ct);
                await DecodeAndLoadGPUAsync(ct);
            }, ct);
    }

    public TextureWrap GetTexture(double time)
    {
        DebugHelper.Assert(CurrentState >= State.LoadedGPU, "Image not loaded into GPU VRAM yet");
        DebugHelper.Assert(Width.HasValue && Height.HasValue, "Image has no width or height");
        DebugHelper.Assert(Frames != null, "Image has no frames");
        DebugHelper.Assert(Frames!.Count == 1 || CycleTime > 0, "Image has multible Frames but no cycle time");

        var frame = Frames[0];
        if (Frames.Count > 1)
        {
            var t = time % CycleTime;
            var timeTotal = 0;
            foreach (var f in Frames)
            {
                timeTotal += f.FrameDelay;
                if (timeTotal > t)
                {
                    frame = f;
                    break;
                }
            }
        }

        DebugHelper.Assert(frame.Texture != null, "Frame has no texture");
        return frame.Texture!;
    }

    private void OnError(Exception ex)
    {
        CurrentState = State.Error;
        ImageSource.FaultedIncrement();
        PluginLog.LogError(ex, "Error while loading image");
    }
}
