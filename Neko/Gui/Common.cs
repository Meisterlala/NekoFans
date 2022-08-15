using System.Numerics;
using ImGuiNET;

namespace Neko.Gui;

public static class Common
{
    public static void HelpMarker(string desc)
    {
        if (desc == "")
            return;

        ImGui.TextDisabled("(?)");
        ToolTip(desc);
    }

    public static void ToolTip(string desc)
    {
        if (desc == "")
            return;

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.Text(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    /// <summary>
    /// Aligns an image in a rectange. imageSize doesnt have to fit in rectange
    /// Returns starting position and end posotion of aligned image
    /// </summary>
    public static (Vector2, Vector2) AlignImage(Vector2 imgSize, Vector2 rectangle, Configuration.ImageAlignment alignment)
    {
        var imageRatio = imgSize.X / imgSize.Y;
        var rectangeRatio = rectangle.Y / rectangle.X;
        var scaled = new Vector2(rectangle.Y / imageRatio, rectangle.X * imageRatio);
        bool widthReduced = rectangeRatio > imageRatio; // True when width of image is bigger than rectangle

        Vector2 start = alignment switch
        {
            Configuration.ImageAlignment.TopLeft => Vector2.Zero,
            Configuration.ImageAlignment.Top when widthReduced => Vector2.Zero,
            Configuration.ImageAlignment.Top => new Vector2((rectangle.X - scaled.X) / 2f, 0f),
            Configuration.ImageAlignment.TopRight when widthReduced => Vector2.Zero,
            Configuration.ImageAlignment.TopRight => new Vector2(rectangle.X - scaled.X, 0f),
            Configuration.ImageAlignment.Left when widthReduced => new Vector2(0f, (rectangle.Y - scaled.Y) / 2f),
            Configuration.ImageAlignment.Left => Vector2.Zero,
            Configuration.ImageAlignment.Center when widthReduced => new Vector2(0f, (rectangle.Y - scaled.Y) / 2f),
            Configuration.ImageAlignment.Center => new Vector2((rectangle.X - scaled.X) / 2f, 0f),
            Configuration.ImageAlignment.Right when widthReduced => new Vector2(0f, (rectangle.Y - scaled.Y) / 2f),
            Configuration.ImageAlignment.Right => new Vector2(rectangle.X - scaled.X, 0f),
            Configuration.ImageAlignment.BottomLeft when widthReduced => new Vector2(0f, rectangle.Y - scaled.Y),
            Configuration.ImageAlignment.BottomLeft => Vector2.Zero,
            Configuration.ImageAlignment.Bottom when widthReduced => new Vector2(0f, rectangle.Y - scaled.Y),
            Configuration.ImageAlignment.Bottom => new Vector2((rectangle.X - scaled.X) / 2f, 0f),
            Configuration.ImageAlignment.BottomRight when widthReduced => new Vector2(0f, rectangle.Y - scaled.Y),
            Configuration.ImageAlignment.BottomRight => new Vector2(rectangle.X - scaled.X, 0f),
            _ => Vector2.Zero
        };

        Vector2 end = alignment switch
        {
            Configuration.ImageAlignment.TopLeft when widthReduced => new Vector2(rectangle.X, scaled.Y),
            Configuration.ImageAlignment.TopLeft => new Vector2(scaled.X, rectangle.Y),
            Configuration.ImageAlignment.Top when widthReduced => new Vector2(rectangle.X, scaled.Y),
            Configuration.ImageAlignment.Top => new Vector2(rectangle.X - start.X, rectangle.Y),
            Configuration.ImageAlignment.TopRight when widthReduced => new Vector2(rectangle.X, scaled.Y),
            Configuration.ImageAlignment.TopRight => rectangle,
            Configuration.ImageAlignment.Left when widthReduced => new Vector2(rectangle.X, rectangle.Y - start.Y),
            Configuration.ImageAlignment.Left => new Vector2(scaled.X, rectangle.Y),
            Configuration.ImageAlignment.Center when widthReduced => new Vector2(rectangle.X, rectangle.Y - start.Y),
            Configuration.ImageAlignment.Center => new Vector2(rectangle.X - start.X, rectangle.Y),
            Configuration.ImageAlignment.Right when widthReduced => new Vector2(rectangle.X, rectangle.Y - start.Y),
            Configuration.ImageAlignment.Right => rectangle,
            Configuration.ImageAlignment.BottomLeft when widthReduced => rectangle,
            Configuration.ImageAlignment.BottomLeft => new Vector2(scaled.X, rectangle.Y),
            Configuration.ImageAlignment.Bottom when widthReduced => rectangle,
            Configuration.ImageAlignment.Bottom => new Vector2(rectangle.X - start.X, rectangle.Y),
            Configuration.ImageAlignment.BottomRight when widthReduced => rectangle,
            Configuration.ImageAlignment.BottomRight => rectangle,
            _ => rectangle
        };

        return (start, end);
    }
}
