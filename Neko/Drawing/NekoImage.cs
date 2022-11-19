using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using ImGuiScene;

namespace Neko.Drawing;

public class NekoImage
{
    public class Frame : IDisposable
    {
        public byte[]? Data { get; private set; }
        public int FrameDelay { get; }
        public TextureWrap? Texture { get; set; }

        public Frame(byte[] data, int frameDelay)
        {
            Data = data;
            FrameDelay = frameDelay; // In ms
        }

        public void ClearData()
            => Data = null;

        public void Dispose()
        {
            Texture?.Dispose();
            Texture = null;
            Data = null;
            GC.SuppressFinalize(this);
        }
    }

    public enum State
    {
        Error,
        Downloading,
        Downloaded,
        Decoded,
        /// <summary>
        /// Decoded and loaded into GPU VRAM
        /// </summary>
        LoadedGPU,
    }

    public State CurrentState { get; private set; } = State.Error;
    public string? DebugInfo { get; set; }
    public string? Description { get; set; }
    public string? URLDownloadWebsite { get; set; }
    public string? URLOpenOnClick { get; set; }
    public Sources.ImageSource ImageSource { get; }

    public byte[]? EncodedData { get; private set; }
    public List<Frame>? Frames { get; private set; }
    /// <summary>
    /// in ms: When the animation should loop
    /// </summary>
    public int CycleTime { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }

    /// <summary>
    /// 0 when the image is not currently loading
    /// 1 when the image is currently loading or finished loading
    /// </summary>
    private int DecodingAndLoading;
    public bool IsDecodingAndLoading => DecodingAndLoading == 1;

    private int CurrentFrameIndex;
    private double LastFrameChange;

    public long RAMUsage =>
        CurrentState == State.Downloading || EncodedData == null
        ? 0
        : EncodedData.LongLength;

    public long VRAMUsage
    {
        get
        {
            if (Frames == null
            || Frames.Count == 0
            || CurrentState != State.LoadedGPU)
            {
                return 0;
            }
            DebugHelper.Assert(Frames.Count > 0, "No frames in image");
            DebugHelper.Assert(Width.HasValue && Height.HasValue, "Image has no width or height");
            return Width!.Value * Height!.Value * 4L * Frames.Count!;
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
                DebugHelper.RandomThrow(DebugHelper.ThrowChance.DownloadImage);
                await DebugHelper.RandomDelay(DebugHelper.Delay.DownloadImage).ConfigureAwait(false);

                var task = downloadTask(this);
                var response = await task.ConfigureAwait(false);

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

        if (Frames != null)
        {
            foreach (var frame in Frames)
            {
                frame.Dispose();
            }
            Frames?.Clear();
            Frames = null;
        }
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
        DebugHelper.RandomThrow(DebugHelper.ThrowChance.DecodeImage);
        DebugHelper.RandomDelay(DebugHelper.Delay.DecodeImage).Wait();

        DebugHelper.Assert(CurrentState == State.Downloaded, "Image is not downloaded. Current state: " + CurrentState);

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

        // Clear downloaded data
        EncodedData = Array.Empty<byte>();

        CurrentState = State.Decoded;
    }

    private void LoadGPU()
    {
        DebugHelper.RandomThrow(DebugHelper.ThrowChance.LoadGPU);
        DebugHelper.RandomDelay(DebugHelper.Delay.LoadGPU).Wait();

        DebugHelper.Assert(CurrentState == State.Decoded, "Image is not decoded. Current state: " + CurrentState);
        DebugHelper.Assert(Frames != null, "Image has no frames");

        var textures = ImageLoad.LoadFrames(this);
        for (var i = 0; i < Frames!.Count; i++)
        {
            Frames[i].Texture = textures[i];
        }
        CurrentState = State.LoadedGPU;
    }

    private void ClearData(){
        DebugHelper.Assert(CurrentState == State.LoadedGPU, "Image is not loaded into GPU. Current state: " + CurrentState);
        DebugHelper.Assert(Frames != null, "Image has no frames");

        foreach (var frame in Frames!)
        {
            frame.ClearData();
        }
    }

    private Task DecodeAndLoadGPUAsync(CancellationToken ct = default)
        => Task.Run(() =>
        {
            try
            {
                Decode();
                LoadGPU();
                ClearData();
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }, ct);

    public Task Await(State state, CancellationToken ct = default)
        => Await((s) => s == state, ct);

    public async Task Await(Predicate<State> predicate, CancellationToken ct = default)
    {
        while (!predicate(CurrentState))
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(10, ct).ConfigureAwait(false);
        }
    }

    public void RequestLoadGPU(CancellationToken ct = default)
    {
        // If the image is already loaded, do nothing
        if (CurrentState is State.LoadedGPU or State.Error)
            return;

        // If it is not currently decoding/loading, start decoding/loading
        if (Interlocked.CompareExchange(ref DecodingAndLoading, 1, 0) == 0)
        {
            Task.Run(async () =>
            {
                // Wait for the image to be downloaded
                if (CurrentState == State.Downloading)
                    await Await(State.Downloaded, ct).ConfigureAwait(false);
                await DecodeAndLoadGPUAsync(ct).ConfigureAwait(false);
            }, ct);
        }
    }

    public TextureWrap GetTexture(double time)
    {
        DebugHelper.Assert(CurrentState == State.LoadedGPU, "Image not loaded into GPU VRAM yet. Current state: " + CurrentState);
        DebugHelper.Assert(Width.HasValue && Height.HasValue, "Image has no width or height");
        DebugHelper.Assert(Frames != null || Frames!.Count != 0, "Image has no frames");

        if (Frames!.Count == 1)
            return Frames[0].Texture!;

        var delay = Frames[CurrentFrameIndex].FrameDelay / (Plugin.Config.GIFSpeed / 100f);
        if (Math.Abs(time - LastFrameChange) >= delay)
        {
            LastFrameChange = time;
            CurrentFrameIndex = (CurrentFrameIndex + 1) % Frames.Count;
        }

        var frame = Frames[CurrentFrameIndex];
        DebugHelper.Assert(frame.Texture != null, "Frame has no texture");
        return frame.Texture!;
    }

    private void OnError(Exception ex)
    {
        CurrentState = State.Error;
        ImageSource.FaultedIncrement();
        PluginLog.LogWarning(ex, "Error while loading image");
    }
}
