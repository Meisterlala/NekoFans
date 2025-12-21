using System.Collections.Generic;
using TerraFX.Interop.DirectX;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;


namespace Neko.Drawing;

public static class ImageLoad
{
    private static IDalamudTextureWrap LoadTexture(byte[] imagedata, int width, int height)
    {
        RawImageSpecification imageSpecs = new()
        {
            Width = width,
            Height = height,
            DxgiFormat = (int)DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
            Pitch = width * 4,
        };

        return Plugin.TextureProvider.CreateFromRaw(imageSpecs, imagedata, "NekoFans Image");
    }

    public static List<IDalamudTextureWrap> LoadFrames(NekoImage image)
    {
        DebugHelper.Assert(image.CurrentState >= NekoImage.State.Decoded, "Image not decoded yet");
        DebugHelper.Assert(image.Width.HasValue && image.Height.HasValue, "Image has no width or height");
        DebugHelper.Assert(image.Frames != null, "Image has no frames");
        DebugHelper.Assert(image.CurrentState != NekoImage.State.LoadedGPU, "Image is already loaded into GPU VRAM");

        var res = new List<IDalamudTextureWrap>();
        foreach (var frame in image.Frames!)
        {
            DebugHelper.Assert(frame.Data != null, "Frame has no data");
            res.Add(LoadTexture(frame.Data!, image.Width!.Value, image.Height!.Value));
        }
        return res;
    }
}
