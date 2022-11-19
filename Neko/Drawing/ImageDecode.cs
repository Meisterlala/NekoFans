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

            // According to the GIF spec, the frame delay should always be honored.
            // So a frame delay of 0 should imidiatly display the next frame.
            var frameDelay = img.Frames[i].Metadata.GetGifMetadata().FrameDelay * 10; // convert to ms
            // However, most programs ignore this and use a arbitrary delay instead.
            // see https://bugzilla.mozilla.org/show_bug.cgi?id=139677
            // We dont allow frame delays below 50ms.
            // Frames with 0 delay are set to 100ms
            if (frameDelay <= 0)
                frameDelay = 100;
            if (frameDelay < 50)
                frameDelay = 50;

            frame.CopyPixelDataTo(frameData);
            frames.Add(new NekoImage.Frame(frameData, frameDelay));
        }

        return new DecodeInfo
        {
            Width = img.Width,
            Height = img.Height,
            Frames = frames
        };
    }
}
