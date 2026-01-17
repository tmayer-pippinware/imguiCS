using System;

namespace ImGui;

/// <summary>
/// Holds Dear ImGui state for the current context.
/// </summary>
public sealed class ImGuiContext
{
    public ImGuiIO IO;
    public ImGuiStyle Style;
    public int FrameCount;

    public ImGuiContext()
    {
        IO = new ImGuiIO();
        Style = new ImGuiStyle();
        FrameCount = 0;
    }
}
