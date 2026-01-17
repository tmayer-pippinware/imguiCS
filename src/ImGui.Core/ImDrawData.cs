using System.Collections.Generic;

namespace ImGui;

public sealed class ImDrawData
{
    public bool Valid { get; internal set; }
    public ImVec2 DisplayPos { get; internal set; }
    public ImVec2 DisplaySize { get; internal set; }
    public ImVec2 FramebufferScale { get; internal set; }
    public int TotalVtxCount { get; internal set; }
    public int TotalIdxCount { get; internal set; }
    public List<object> CmdLists { get; } = new(); // Placeholder until draw list is ported.

    internal void Reset()
    {
        Valid = false;
        CmdLists.Clear();
        TotalVtxCount = 0;
        TotalIdxCount = 0;
    }
}
