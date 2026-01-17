using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ImGui;
using SDL2;

namespace ImGui.Backends.Sdl;

/// <summary>
/// SDL2 platform backend mirroring ImGui_ImplSDL2 API surface.
/// Defaults to a managed-only path that avoids native SDL calls unless explicitly enabled.
/// </summary>
public sealed class ImGuiSDL2Platform
{
    private readonly Stopwatch _stopwatch = new();
    private ulong _lastPerformanceCounter;
    private long _lastTicks;
    private IntPtr _window;
    private bool _nativeAvailable;
    private bool _ownsSdlVideo;
    private ImGuiSDL2PlatformOptions _options = new();

    public bool Init(IntPtr window, ImGuiSDL2PlatformOptions? options = null)
    {
        _options = options ?? new ImGuiSDL2PlatformOptions();
        _window = window;
        _nativeAvailable = _options.UseNativeBackend && TryEnsureSdlVideo();

        ref var io = ref ImGui.GetIO();
        io.BackendPlatformName = "imgui_impl_sdl2_cs";
        io.BackendFlags |= ImGuiBackendFlags_.ImGuiBackendFlags_HasMouseCursors;
        if (_options.AllowSetMousePosition)
            io.BackendFlags |= ImGuiBackendFlags_.ImGuiBackendFlags_HasSetMousePos;

        if (_nativeAvailable)
        {
            io.GetClipboardTextFn = GetClipboardTextSdl;
            io.SetClipboardTextFn = SetClipboardTextSdl;
        }

        if (_nativeAvailable && _window != IntPtr.Zero)
            UpdateDisplaySizeFromNative();
        else if (_options.InitialDisplaySize.HasValue)
            UpdateDisplaySize((int)_options.InitialDisplaySize.Value.x, (int)_options.InitialDisplaySize.Value.y);

        _stopwatch.Restart();
        _lastTicks = _stopwatch.ElapsedTicks;
        _lastPerformanceCounter = _nativeAvailable ? SDL.SDL_GetPerformanceCounter() : 0;
        return true;
    }

    public void Shutdown()
    {
        if (_ownsSdlVideo)
            SDL.SDL_QuitSubSystem(SDL.SDL_INIT_VIDEO);
        _window = IntPtr.Zero;
        _nativeAvailable = false;
    }

    public void NewFrame()
    {
        ref var io = ref ImGui.GetIO();
        io.DeltaTime = CalculateDeltaTime();

        if (_nativeAvailable && _window != IntPtr.Zero)
        {
            UpdateDisplaySizeFromNative();
            UpdateFramebufferScaleFromNative(ref io);
        }
    }

    public void ProcessEvent(ref SDL.SDL_Event ev)
    {
        ref var io = ref ImGui.GetIO();
        switch (ev.type)
        {
            case SDL.SDL_EventType.SDL_MOUSEMOTION:
                io.AddMousePosEvent(ev.motion.x, ev.motion.y);
                break;
            case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
            case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                int button = ev.button.button switch
                {
                    (byte)SDL.SDL_BUTTON_LEFT => 0,
                    (byte)SDL.SDL_BUTTON_RIGHT => 1,
                    (byte)SDL.SDL_BUTTON_MIDDLE => 2,
                    (byte)SDL.SDL_BUTTON_X1 => 3,
                    (byte)SDL.SDL_BUTTON_X2 => 4,
                    _ => ev.button.button
                };
                bool down = ev.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN;
                io.AddMouseButtonEvent(button, down);
                break;
            case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                io.AddMouseWheelEvent(ev.wheel.x, ev.wheel.y);
                break;
            case SDL.SDL_EventType.SDL_KEYDOWN:
            case SDL.SDL_EventType.SDL_KEYUP:
                var mapped = MapKey(ev.key.keysym.sym);
                if (mapped != ImGuiKey.ImGuiKey_None)
                    io.AddKeyEvent(mapped, ev.type == SDL.SDL_EventType.SDL_KEYDOWN);
                break;
            case SDL.SDL_EventType.SDL_TEXTINPUT:
                unsafe
                {
                    fixed (byte* text = ev.text.text)
                        io.AddInputCharactersUTF8(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(text));
                }
                break;
            case SDL.SDL_EventType.SDL_WINDOWEVENT:
                if (ev.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED)
                    io.AddFocusEvent(true);
                else if (ev.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST)
                    io.AddFocusEvent(false);
                else if (ev.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED)
                    UpdateDisplaySize(ev.window.data1, ev.window.data2);
                break;
        }
    }

    public void ProcessEvent(SdlEvent ev)
    {
        // Legacy compatibility overload for callers not yet using SDL_Event.
        if (ev.Type == (int)SDL.SDL_EventType.SDL_WINDOWEVENT)
            UpdateDisplaySize(ev.Data1, ev.Data2);
    }

    public void UpdateDisplaySize(int width, int height)
    {
        ref var io = ref ImGui.GetIO();
        io.DisplaySize = new ImVec2(width, height);
    }

    private void UpdateDisplaySizeFromNative()
    {
        if (!_nativeAvailable || _window == IntPtr.Zero)
            return;
        SDL.SDL_GetWindowSize(_window, out int w, out int h);
        UpdateDisplaySize(w, h);
    }

    private void UpdateFramebufferScaleFromNative(ref ImGuiIO io)
    {
        if (_window == IntPtr.Zero)
            return;
        SDL.SDL_GL_GetDrawableSize(_window, out int drawableW, out int drawableH);
        if (io.DisplaySize.x > 0 && io.DisplaySize.y > 0 && drawableW > 0 && drawableH > 0)
        {
            io.DisplayFramebufferScale = new ImVec2(drawableW / io.DisplaySize.x, drawableH / io.DisplaySize.y);
        }
    }

    private static string GetClipboardTextSdl()
    {
        return SDL.SDL_GetClipboardText() ?? string.Empty;
    }

    private static void SetClipboardTextSdl(string text)
    {
        SDL.SDL_SetClipboardText(text ?? string.Empty);
    }

    private float CalculateDeltaTime()
    {
        if (_nativeAvailable)
        {
            ulong counter = SDL.SDL_GetPerformanceCounter();
            ulong freq = SDL.SDL_GetPerformanceFrequency();
            float dt = (float)((double)(counter - _lastPerformanceCounter) / freq);
            _lastPerformanceCounter = counter;
            if (dt <= 0f)
                dt = 1f / 60f;
            return dt;
        }

        long ticks = _stopwatch.ElapsedTicks;
        long delta = ticks - _lastTicks;
        _lastTicks = ticks;
        return delta > 0 ? (float)delta / Stopwatch.Frequency : 1f / 60f;
    }

    private bool TryEnsureSdlVideo()
    {
        try
        {
            uint initialized = SDL.SDL_WasInit(SDL.SDL_INIT_VIDEO);
            if ((initialized & SDL.SDL_INIT_VIDEO) == 0)
            {
                if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
                    return false;
                _ownsSdlVideo = true;
            }
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }

    private static ImGuiKey MapKey(SDL.SDL_Keycode key)
    {
        if (KeyMap.TryGetValue(key, out var mapped))
            return mapped;

        if (key >= SDL.SDL_Keycode.SDLK_a && key <= SDL.SDL_Keycode.SDLK_z)
            return (ImGuiKey)((int)ImGuiKey.ImGuiKey_A + (int)(key - SDL.SDL_Keycode.SDLK_a));
        if (key >= SDL.SDL_Keycode.SDLK_0 && key <= SDL.SDL_Keycode.SDLK_9)
            return (ImGuiKey)((int)ImGuiKey.ImGuiKey_0 + (int)(key - SDL.SDL_Keycode.SDLK_0));
        if (key >= SDL.SDL_Keycode.SDLK_F1 && key <= SDL.SDL_Keycode.SDLK_F12)
            return (ImGuiKey)((int)ImGuiKey.ImGuiKey_F1 + (int)(key - SDL.SDL_Keycode.SDLK_F1));

        return ImGuiKey.ImGuiKey_None;
    }

    private static readonly Dictionary<SDL.SDL_Keycode, ImGuiKey> KeyMap = new()
    {
        { SDL.SDL_Keycode.SDLK_TAB, ImGuiKey.ImGuiKey_Tab },
        { SDL.SDL_Keycode.SDLK_LEFT, ImGuiKey.ImGuiKey_LeftArrow },
        { SDL.SDL_Keycode.SDLK_RIGHT, ImGuiKey.ImGuiKey_RightArrow },
        { SDL.SDL_Keycode.SDLK_UP, ImGuiKey.ImGuiKey_UpArrow },
        { SDL.SDL_Keycode.SDLK_DOWN, ImGuiKey.ImGuiKey_DownArrow },
        { SDL.SDL_Keycode.SDLK_PAGEUP, ImGuiKey.ImGuiKey_PageUp },
        { SDL.SDL_Keycode.SDLK_PAGEDOWN, ImGuiKey.ImGuiKey_PageDown },
        { SDL.SDL_Keycode.SDLK_HOME, ImGuiKey.ImGuiKey_Home },
        { SDL.SDL_Keycode.SDLK_END, ImGuiKey.ImGuiKey_End },
        { SDL.SDL_Keycode.SDLK_INSERT, ImGuiKey.ImGuiKey_Insert },
        { SDL.SDL_Keycode.SDLK_DELETE, ImGuiKey.ImGuiKey_Delete },
        { SDL.SDL_Keycode.SDLK_BACKSPACE, ImGuiKey.ImGuiKey_Backspace },
        { SDL.SDL_Keycode.SDLK_SPACE, ImGuiKey.ImGuiKey_Space },
        { SDL.SDL_Keycode.SDLK_RETURN, ImGuiKey.ImGuiKey_Enter },
        { SDL.SDL_Keycode.SDLK_ESCAPE, ImGuiKey.ImGuiKey_Escape },
        { SDL.SDL_Keycode.SDLK_LCTRL, ImGuiKey.ImGuiKey_LeftCtrl },
        { SDL.SDL_Keycode.SDLK_LSHIFT, ImGuiKey.ImGuiKey_LeftShift },
        { SDL.SDL_Keycode.SDLK_LALT, ImGuiKey.ImGuiKey_LeftAlt },
        { SDL.SDL_Keycode.SDLK_LGUI, ImGuiKey.ImGuiKey_LeftSuper },
        { SDL.SDL_Keycode.SDLK_RCTRL, ImGuiKey.ImGuiKey_RightCtrl },
        { SDL.SDL_Keycode.SDLK_RSHIFT, ImGuiKey.ImGuiKey_RightShift },
        { SDL.SDL_Keycode.SDLK_RALT, ImGuiKey.ImGuiKey_RightAlt },
        { SDL.SDL_Keycode.SDLK_RGUI, ImGuiKey.ImGuiKey_RightSuper },
        { SDL.SDL_Keycode.SDLK_MENU, ImGuiKey.ImGuiKey_Menu },
        { SDL.SDL_Keycode.SDLK_QUOTE, ImGuiKey.ImGuiKey_Apostrophe },
        { SDL.SDL_Keycode.SDLK_COMMA, ImGuiKey.ImGuiKey_Comma },
        { SDL.SDL_Keycode.SDLK_MINUS, ImGuiKey.ImGuiKey_Minus },
        { SDL.SDL_Keycode.SDLK_PERIOD, ImGuiKey.ImGuiKey_Period },
        { SDL.SDL_Keycode.SDLK_SLASH, ImGuiKey.ImGuiKey_Slash },
        { SDL.SDL_Keycode.SDLK_SEMICOLON, ImGuiKey.ImGuiKey_Semicolon },
        { SDL.SDL_Keycode.SDLK_EQUALS, ImGuiKey.ImGuiKey_Equal },
        { SDL.SDL_Keycode.SDLK_LEFTBRACKET, ImGuiKey.ImGuiKey_LeftBracket },
        { SDL.SDL_Keycode.SDLK_BACKSLASH, ImGuiKey.ImGuiKey_Backslash },
        { SDL.SDL_Keycode.SDLK_RIGHTBRACKET, ImGuiKey.ImGuiKey_RightBracket },
        { SDL.SDL_Keycode.SDLK_BACKQUOTE, ImGuiKey.ImGuiKey_GraveAccent },
        { SDL.SDL_Keycode.SDLK_CAPSLOCK, ImGuiKey.ImGuiKey_CapsLock },
        { SDL.SDL_Keycode.SDLK_SCROLLLOCK, ImGuiKey.ImGuiKey_ScrollLock },
        { SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR, ImGuiKey.ImGuiKey_NumLock },
        { SDL.SDL_Keycode.SDLK_PRINTSCREEN, ImGuiKey.ImGuiKey_PrintScreen },
        { SDL.SDL_Keycode.SDLK_PAUSE, ImGuiKey.ImGuiKey_Pause },
        { SDL.SDL_Keycode.SDLK_KP_0, ImGuiKey.ImGuiKey_Keypad0 },
        { SDL.SDL_Keycode.SDLK_KP_1, ImGuiKey.ImGuiKey_Keypad1 },
        { SDL.SDL_Keycode.SDLK_KP_2, ImGuiKey.ImGuiKey_Keypad2 },
        { SDL.SDL_Keycode.SDLK_KP_3, ImGuiKey.ImGuiKey_Keypad3 },
        { SDL.SDL_Keycode.SDLK_KP_4, ImGuiKey.ImGuiKey_Keypad4 },
        { SDL.SDL_Keycode.SDLK_KP_5, ImGuiKey.ImGuiKey_Keypad5 },
        { SDL.SDL_Keycode.SDLK_KP_6, ImGuiKey.ImGuiKey_Keypad6 },
        { SDL.SDL_Keycode.SDLK_KP_7, ImGuiKey.ImGuiKey_Keypad7 },
        { SDL.SDL_Keycode.SDLK_KP_8, ImGuiKey.ImGuiKey_Keypad8 },
        { SDL.SDL_Keycode.SDLK_KP_9, ImGuiKey.ImGuiKey_Keypad9 },
        { SDL.SDL_Keycode.SDLK_KP_PERIOD, ImGuiKey.ImGuiKey_KeypadDecimal },
        { SDL.SDL_Keycode.SDLK_KP_DIVIDE, ImGuiKey.ImGuiKey_KeypadDivide },
        { SDL.SDL_Keycode.SDLK_KP_MULTIPLY, ImGuiKey.ImGuiKey_KeypadMultiply },
        { SDL.SDL_Keycode.SDLK_KP_MINUS, ImGuiKey.ImGuiKey_KeypadSubtract },
        { SDL.SDL_Keycode.SDLK_KP_PLUS, ImGuiKey.ImGuiKey_KeypadAdd },
        { SDL.SDL_Keycode.SDLK_KP_ENTER, ImGuiKey.ImGuiKey_KeypadEnter },
        { SDL.SDL_Keycode.SDLK_KP_EQUALS, ImGuiKey.ImGuiKey_KeypadEqual },
    };

    public bool WantCaptureMouse => ImGui.GetIO().WantCaptureMouse;
    public bool WantCaptureKeyboard => ImGui.GetIO().WantCaptureKeyboard;
}

public sealed class ImGuiSDL2PlatformOptions
{
    /// <summary>
    /// When true, calls into SDL to initialize the video subsystem and query window sizes/timers.
    /// Leave false for headless tests without native SDL binaries present.
    /// </summary>
    public bool UseNativeBackend { get; init; }

    /// <summary>
    /// Allow SetMousePos semantics advertised to Dear ImGui.
    /// </summary>
    public bool AllowSetMousePosition { get; init; } = true;

    /// <summary>
    /// Optional initial display size when no SDL window is present.
    /// </summary>
    public ImVec2? InitialDisplaySize { get; init; }
}
