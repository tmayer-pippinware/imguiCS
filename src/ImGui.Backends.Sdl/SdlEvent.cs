namespace ImGui.Backends.Sdl;

/// <summary>
/// Minimal placeholder SDL event used to mirror the backend API shape without SDL dependencies.
/// </summary>
public struct SdlEvent
{
    public int Type;
    public int Data1;
    public int Data2;
}
