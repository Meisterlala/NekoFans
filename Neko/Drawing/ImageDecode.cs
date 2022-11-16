using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Neko.Drawing;

public static class ImageDecode
{
    public struct DecodeInfo
    {
        public int Width;
        public int Height;
        public List<NekoImage.Frame> Frames;
    }

    public static DecodeInfo DecodeImageFrames(byte[] imagedata)
    {
        var img = Image.Load<Rgba32>(imagedata);
        var frames = new List<NekoImage.Frame>();

        for (var i = 0; i < img.Frames.Count; i++)
        {
            var frame = img.Frames[i];
            var frameData = new byte[frame.Width * frame.Height * 4];
            frame.CopyPixelDataTo(frameData);
            frames.Add(new NekoImage.Frame(frameData, img.Frames[i].Metadata.GetGifMetadata().FrameDelay));
        }

        return new DecodeInfo
        {
            Width = img.Width,
            Height = img.Height,
            Frames = frames
        };
    }
}
