namespace ImGui;

/// <summary>
/// Helper for running a block not more than once per frame.
/// </summary>
public struct ImGuiOnceUponAFrame
{
    private int _refFrame = -1;

    public ImGuiOnceUponAFrame()
    {
        _refFrame = -1;
    }

    public bool Test()
    {
        var ctx = ImGui.GetCurrentContext() ?? throw new System.InvalidOperationException("No current context.");
        if (_refFrame == ctx.FrameCount)
            return false;
        _refFrame = ctx.FrameCount;
        return true;
    }
}
