using System;
using ImGui;
using ImGui.Backends.Sdl;
using SDL2;
using Xunit;

namespace ImGui.Core.Tests;

public class BackendTests
{
    [Fact]
    public void Sdl_platform_updates_display_size()
    {
        ImGui.CreateContext();
        var platform = new ImGuiSDL2Platform();
        platform.Init(IntPtr.Zero);
        platform.UpdateDisplaySize(800, 600);
        var io = ImGui.GetIO();
        Assert.Equal(800, io.DisplaySize.x);
        Assert.Equal(600, io.DisplaySize.y);
    }

    [Fact]
    public unsafe void Sdl_platform_processes_events_without_native_sdl()
    {
        ImGui.CreateContext();
        var platform = new ImGuiSDL2Platform();
        platform.Init(IntPtr.Zero);

        SDL.SDL_Event ev = default;
        ev.type = SDL.SDL_EventType.SDL_MOUSEMOTION;
        ev.motion.x = 42;
        ev.motion.y = 24;
        platform.ProcessEvent(ref ev);

        ev = default;
        ev.type = SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN;
        ev.button.button = (byte)SDL.SDL_BUTTON_LEFT;
        platform.ProcessEvent(ref ev);

        ev = default;
        ev.type = SDL.SDL_EventType.SDL_MOUSEWHEEL;
        ev.wheel.y = 1;
        platform.ProcessEvent(ref ev);

        ev = default;
        ev.type = SDL.SDL_EventType.SDL_KEYDOWN;
        ev.key.keysym.sym = SDL.SDL_Keycode.SDLK_TAB;
        platform.ProcessEvent(ref ev);

        ev = default;
        ev.type = SDL.SDL_EventType.SDL_TEXTINPUT;
        ev.text.text[0] = (byte)'A';
        ev.text.text[1] = 0;
        platform.ProcessEvent(ref ev);

        ImGui.NewFrame();
        var io = ImGui.GetIO();
        Assert.Equal(42, io.MousePos.x);
        Assert.Equal(24, io.MousePos.y);
        Assert.True(io.MouseDown[0]);
        Assert.Equal(1, io.MouseWheel);
        int tabIndex = (int)ImGuiKey.ImGuiKey_Tab - (int)ImGuiKey.ImGuiKey_NamedKey_BEGIN;
        Assert.True(io.KeysData[tabIndex].Down);
        Assert.Contains((uint)'A', io.InputQueueCharacters);
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
