using System;

namespace ImGui;

public static partial class ImGui
{
    private static ImGuiContext? _currentContext;
    private static readonly ImDrawData _drawData = new();

    public static ImGuiContext CreateContext(ImFontAtlas? sharedFontAtlas = null)
    {
        var ctx = new ImGuiContext();
        SetCurrentContext(ctx);
        if (sharedFontAtlas != null)
            ctx.IO.Fonts = sharedFontAtlas;
        return ctx;
    }

    public static void DestroyContext(ImGuiContext? context = null)
    {
        context ??= _currentContext;
        if (context == null)
            return;
        if (ReferenceEquals(_currentContext, context))
            _currentContext = null;
    }

    public static void SetCurrentContext(ImGuiContext? context)
    {
        _currentContext = context;
    }

    public static ImGuiContext? GetCurrentContext() => _currentContext;

    public static ref ImGuiIO GetIO()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ref ctx.IO;
    }

    public static ref ImGuiStyle GetStyle()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ref ctx.Style;
    }

    public static void NewFrame()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.FrameCount++;
        if (ctx.IO.DeltaTime <= 0f)
            ctx.IO.DeltaTime = 1f / 60f;
        ctx.ProcessInputEvents();
        _drawData.Reset();
    }

    public static void EndFrame()
    {
        if (_currentContext == null)
            throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
    }

    public static void Render()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        _drawData.Valid = true;
        _drawData.DisplayPos = new ImVec2(0, 0);
        _drawData.DisplaySize = ctx.IO.DisplaySize;
        _drawData.FramebufferScale = ctx.IO.DisplayFramebufferScale;
        _drawData.CmdLists.Clear();
        _drawData.CmdLists.Add(ctx.ForegroundDrawList);
        _drawData.TotalVtxCount = ctx.ForegroundDrawList.VtxBuffer.Count;
        _drawData.TotalIdxCount = ctx.ForegroundDrawList.IdxBuffer.Count;
    }

    public static ImDrawData GetDrawData()
    {
        return _drawData;
    }

    public static ImDrawList GetForegroundDrawList()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ctx.ForegroundDrawList;
    }

    public static void AddInputCharacter(uint c)
    {
        ref var io = ref GetIO();
        io.AddInputCharacter(c);
    }

    public static void AddInputCharacterUTF16(ushort c)
    {
        ref var io = ref GetIO();
        io.AddInputCharacterUTF16(c);
    }

    public static void AddInputCharactersUTF8(string utf8)
    {
        ref var io = ref GetIO();
        io.AddInputCharactersUTF8(System.Text.Encoding.UTF8.GetBytes(utf8));
    }

    public static void AddKeyEvent(ImGuiKey key, bool down) => GetIO().AddKeyEvent(key, down);
    public static void AddMousePosEvent(float x, float y) => GetIO().AddMousePosEvent(x, y);
    public static void AddMouseButtonEvent(int button, bool down) => GetIO().AddMouseButtonEvent(button, down);
    public static void AddMouseWheelEvent(float wheelX, float wheelY) => GetIO().AddMouseWheelEvent(wheelX, wheelY);
    public static void AddMouseSourceEvent(ImGuiMouseSource source) => GetIO().AddMouseSourceEvent(source);
    public static void AddFocusEvent(bool focused) => GetIO().AddFocusEvent(focused);

    public static void StyleColorsDark()
    {
        ref var style = ref GetStyle();
        ImGuiStyle.StyleColorsDark(ref style);
    }
}
