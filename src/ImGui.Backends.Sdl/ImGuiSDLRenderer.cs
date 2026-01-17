using ImGui;

namespace ImGui.Backends.Sdl;

/// <summary>
/// Placeholder renderer backend for SDL/OpenGL-style rendering.
/// </summary>
public sealed class ImGuiSDLRenderer
{
    public bool Init(IntPtr window)
    {
        return true;
    }

    public void NewFrame()
    {
    }

    public void RenderDrawData(ImDrawData drawData)
    {
        // Stub: real backend would translate draw lists to GPU commands.
    }

    public void Shutdown()
    {
    }
}
