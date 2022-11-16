using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public State CurrentState { get; private set; } = State.Downloading;
    public string? DebugInfo { get; set; }
    public string? Description { get; set; }
    public string? URLDownloadWebsite { get; }
    public string? URLOpenOnClick { get; set; }
    public Type? Creator { get; set; }

    public byte[]? EncodedData { get; private set; }
    public List<Frame>? Frames { get; private set; }
    public Frame? FrameCurrent => Frames?[FrameIndex];
    private int FrameIndex;
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


    public NekoImage()
    {
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
        //  Debug.Assert(CurrentState == State.Downloading, "Image is not downloading");
        EncodedData = data;
        CurrentState = State.Downloaded;
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

        if (CurrentState == State.Downloading)
            res += $" {URLDownloadWebsite}";

        if (RAMUsage != 0)
            res += $" Data: {Helper.SizeSuffix(RAMUsage)}";
        if (VRAMUsage != 0)
            res += $" Texture: {Helper.SizeSuffix(VRAMUsage)}";
        if (Frames?.Count > 0)
            res += $"\n└─Frames: {Frames.Count}";
        if (Creator != null)
            res += $"\n└─Creator: {Creator.Name}";
        if (DebugInfo?.Length > 0)
            res += $"\n└─DebugInfo: {DebugInfo}";

        return res;
    }

    public void Decode()
    {
        Debug.Assert(CurrentState == State.Downloaded, "Image is not downloaded");
        var decoded = ImageDecode.DecodeImageFrames(EncodedData!);
        Frames = decoded.Frames;
        Width = decoded.Width;
        Height = decoded.Height;
        CurrentState = State.Decoded;
    }

    public void LoadGPU()
    {
        Debug.Assert(CurrentState == State.Decoded, "Image is not decoded");
        Debug.Assert(Frames != null, "Image has no frames");

        var textures = ImageLoad.LoadFrames(this);
        for (var i = 0; i < Frames.Count; i++)
        {
            Frames[i].Texture = textures[i];
        }
        CurrentState = State.LoadedGPU;
    }
}
