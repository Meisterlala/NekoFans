using System;
using System.Collections.Generic;
using ImGuiScene;
using Dalamud.Bindings.ImGui;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;


namespace Neko.Drawing;

public static class ImageLoad
{
    private static unsafe D3DTextureWrap LoadTexture(byte[] imagedata, int width, int height)
    {
        fixed (void* data_ptr = imagedata)
        {
            return LoadTexture(data_ptr, width, height);
        }
    }

    private static unsafe D3DTextureWrap LoadTexture(void* imagedata, int width, int height)
    {
        // Use DeviceHandle and wrap it with SharpDX.Device to avoid using the obsolete IUiBuilder.Device
        var device = new SharpDX.Direct3D11.Device(Plugin.PluginInterface.UiBuilder.DeviceHandle);
        
        ShaderResourceView resView;

        var texDesc = new Texture2DDescription
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Immutable,
            BindFlags = BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        };

        using (var texture = new Texture2D(device, texDesc, new DataRectangle(new IntPtr(imagedata), width * 4)))
        {
            resView = new ShaderResourceView(device, texture, new ShaderResourceViewDescription
            {
                Format = texDesc.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = { MipLevels = texDesc.MipLevels }
            });
        }

        return new D3DTextureWrap(resView, width, height);
    }

    public static List<TextureWrap> LoadFrames(NekoImage image)
    {
        DebugHelper.Assert(image.CurrentState >= NekoImage.State.Decoded, "Image not decoded yet");
        DebugHelper.Assert(image.Width.HasValue && image.Height.HasValue, "Image has no width or height");
        DebugHelper.Assert(image.Frames != null, "Image has no frames");
        DebugHelper.Assert(image.CurrentState != NekoImage.State.LoadedGPU, "Image is already loaded into GPU VRAM");

        var res = new List<TextureWrap>();
        foreach (var frame in image.Frames!)
        {
            DebugHelper.Assert(frame.Data != null, "Frame has no data");
            res.Add(LoadTexture(frame.Data!, image.Width!.Value, image.Height!.Value));
        }
        return res;
    }
}
