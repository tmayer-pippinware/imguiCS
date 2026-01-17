using System.Runtime.InteropServices;
using ImGui;

namespace ImGui.Backends.Sdl;

/// <summary>
/// Placeholder SDL2 platform backend mirroring ImGui_ImplSDL2 API surface.
/// This stub does not depend on SDL; it forwards basic size/input data into ImGuiIO.
/// </summary>
public sealed class ImGuiSDL2Platform
{
    private IntPtr _window;

    public bool Init(IntPtr window)
    {
        _window = window;
        return true;
    }

    public void Shutdown()
    {
        _window = IntPtr.Zero;
    }

    public void NewFrame()
    {
        // Nothing to do in the stub; real backend would sync display size/time/input here.
    }

    public void ProcessEvent(SdlEvent ev)
    {
        // In a real backend this would route SDL events into ImGuiIO.
    }

    public void UpdateDisplaySize(int width, int height)
    {
        ref var io = ref ImGui.GetIO();
        io.DisplaySize = new ImVec2(width, height);
    }

    public bool WantCaptureMouse => ImGui.GetIO().WantCaptureMouse;
    public bool WantCaptureKeyboard => ImGui.GetIO().WantCaptureKeyboard;
}
