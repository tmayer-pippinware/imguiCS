using System;
using ImGui.Backends.Sdl;
using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class BackendTests
{
    [Fact]
    public void Sdl_platform_updates_display_size()
    {
        var platform = new ImGuiSDL2Platform();
        platform.Init(IntPtr.Zero);
        platform.UpdateDisplaySize(800, 600);
        var io = ImGui.GetIO();
        Assert.Equal(800, io.DisplaySize.x);
        Assert.Equal(600, io.DisplaySize.y);
    }

    [Fact]
    public void Sdl_renderer_accepts_draw_data()
    {
        ImGui.CreateContext();
        var renderer = new ImGuiSDLRenderer();
        Assert.True(renderer.Init(IntPtr.Zero));
        renderer.NewFrame();
        renderer.RenderDrawData(ImGui.GetDrawData());
        renderer.Shutdown();
    }
}
