using System;

namespace ImGui;

public static partial class ImGui
{
    private static ImGuiContext? _currentContext;

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
    }

    public static void EndFrame()
    {
        if (_currentContext == null)
            throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
    }
}
