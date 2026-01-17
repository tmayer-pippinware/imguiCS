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
    public readonly List<ImGuiInputEvent> InputEventsQueue = new();
    internal uint InputEventsNextEventId = 1;

    public ImGuiContext()
    {
        IO = new ImGuiIO();
        IO.Context = this;
        Style = new ImGuiStyle();
        FrameCount = 0;
    }

    internal void EnqueueInputEvent(in ImGuiInputEvent evt)
    {
        InputEventsQueue.Add(evt);
    }
}
