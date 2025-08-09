using System;
using Dalamud.Bindings.ImGui;
using SharpDX.Direct3D11;

// Minimal compatibility shims for missing ImGuiScene types used by this project.
// These cover only the members accessed in the codebase.
namespace ImGuiScene
{
    public class TextureWrap : IDisposable
    {
        public ImTextureID ImGuiHandle { get; protected set; }
        public int Width { get; }
        public int Height { get; }

        public TextureWrap(ImTextureID handle, int width, int height)
        {
            ImGuiHandle = handle;
            Width = width;
            Height = height;
        }

        public virtual void Dispose()
        {
            ImGuiHandle = default;
            GC.SuppressFinalize(this);
        }
    }

    public class D3DTextureWrap : TextureWrap
    {
        private readonly ShaderResourceView? resourceView;

        public D3DTextureWrap(ShaderResourceView view, int width, int height)
            : base(new ImTextureID(view?.NativePointer ?? IntPtr.Zero), width, height)
        {
            resourceView = view;
        }

        public override void Dispose()
        {
            resourceView?.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
