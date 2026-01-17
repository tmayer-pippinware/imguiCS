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
        ctx.ForegroundDrawList.Clear();
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
        int totalVtx = 0;
        int totalIdx = 0;
        for (int i = 0; i < ctx.Windows.Count; i++)
        {
            var drawList = ctx.Windows[i].DrawList;
            if (drawList.CmdBuffer.Count == 0 && drawList.VtxBuffer.Count == 0 && drawList.IdxBuffer.Count == 0 && drawList.TextBuffer.Count == 0)
                continue;
            _drawData.CmdLists.Add(drawList);
            totalVtx += drawList.VtxBuffer.Count;
            totalIdx += drawList.IdxBuffer.Count;
        }
        _drawData.CmdLists.Add(ctx.ForegroundDrawList);
        totalVtx += ctx.ForegroundDrawList.VtxBuffer.Count;
        totalIdx += ctx.ForegroundDrawList.IdxBuffer.Count;
        _drawData.TotalVtxCount = totalVtx;
        _drawData.TotalIdxCount = totalIdx;
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

    public static void StyleColorsClassic()
    {
        ref var style = ref GetStyle();
        ImGuiStyle.StyleColorsClassic(ref style);
    }

    public static void StyleColorsLight()
    {
        ref var style = ref GetStyle();
        ImGuiStyle.StyleColorsLight(ref style);
    }

    public static bool Begin(string name)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.Windows.Find(w => w.Name == name);
        if (window == null)
        {
            window = new ImGuiWindow(name);
            ctx.Windows.Add(window);
        }
        ctx.CurrentWindow = window;
        ctx.IDStack.Push(ImHash.Hash(name, ctx.IDStack.Peek()));
        window.DrawList.Clear();
        window.DC.CursorStartPos = ImVec2.Zero;
        window.DC.CursorPos = window.DC.CursorStartPos;
        window.DC.LastItemRect = new ImRect(window.DC.CursorPos, window.DC.CursorPos);
        return true;
    }

    public static void End()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        if (ctx.CurrentWindow == null)
            return;
        if (ctx.IDStack.Count > 1)
            ctx.IDStack.Pop();
        ctx.CurrentWindow = null;
    }

    public static void PushID(string id)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.IDStack.Push(ImHash.Hash(id, ctx.IDStack.Peek()));
    }

    public static void PopID()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        if (ctx.IDStack.Count > 1)
            ctx.IDStack.Pop();
    }

    public static ImGuiID GetID(string label)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ImHash.Hash(label, ctx.IDStack.Peek());
    }

    public static ImVec2 GetCursorPos()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        return window.DC.CursorPos;
    }

    public static void SetCursorPos(ImVec2 localPos)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        window.DC.CursorPos = localPos;
        window.DC.CursorStartPos = localPos;
    }

    public static void SameLine(float offset_from_start_x = 0.0f, float spacing = -1.0f)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var style = ctx.Style;
        float spacingX = spacing >= 0.0f ? spacing : style.ItemSpacing.x;
        float newX = offset_from_start_x > 0.0f
            ? window.DC.CursorStartPos.x + offset_from_start_x
            : window.DC.LastItemRect.Max.x + spacingX;
        float newY = window.DC.LastItemRect.Min.y;
        window.DC.CursorPos = new ImVec2(newX, newY);
    }

    public static void NewLine()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        float y = window.DC.LastItemRect.Max.y + ctx.Style.ItemSpacing.y;
        window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, y);
    }

    public static void Text(string text) => TextUnformatted(text);

    public static void TextUnformatted(string text)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var size = CalcTextSize(text, ctx);
        var pos = window.DC.CursorPos;
        uint col = GetColorU32(ImGuiCol_.ImGuiCol_Text);
        window.DrawList.AddText(pos, col, text);
        AdvanceCursorForItem(ctx, window, new ImRect(pos, new ImVec2(pos.x + size.x, pos.y + size.y)));
        window.DC.LastItemId = 0;
        ctx.LastItemID = 0;
    }

    public static bool Button(string label)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        ImGuiID id = GetID(label);
        var pos = window.DC.CursorPos;
        var size = new ImVec2(100, 20);
        var min = pos;
        var max = new ImVec2(pos.x + size.x, pos.y + size.y);
        var bb = new ImRect(min, max);

        ref var io = ref ctx.IO;
        bool hovered = bb.Contains(io.MousePos);
        bool pressed = hovered && io.MouseClicked[0];
        uint col = GetColorU32(ImGuiCol_.ImGuiCol_Button);
        if (hovered && io.MouseDown[0])
            col = GetColorU32(ImGuiCol_.ImGuiCol_ButtonActive);
        else if (hovered)
            col = GetColorU32(ImGuiCol_.ImGuiCol_ButtonHovered);

        window.DrawList.AddRectFilled(min, max, col);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        return pressed;
    }

    public static uint GetColorU32(ImGuiCol_ idx)
    {
        ref var style = ref GetStyle();
        return ColorConvertFloat4ToU32(style.Colors[(int)idx]);
    }

    private static uint ColorConvertFloat4ToU32(ImVec4 col)
    {
        uint r = (uint)(col.x * 255.0f + 0.5f);
        uint g = (uint)(col.y * 255.0f + 0.5f);
        uint b = (uint)(col.z * 255.0f + 0.5f);
        uint a = (uint)(col.w * 255.0f + 0.5f);
        return (a << 24) | (b << 16) | (g << 8) | r;
    }

    private static ImVec2 CalcTextSize(string text, ImGuiContext ctx)
    {
        float fontSize = ctx.Style.FontSizeBase > 0 ? ctx.Style.FontSizeBase : 13.0f;
        float scale = ctx.IO.FontGlobalScale > 0 ? ctx.IO.FontGlobalScale : 1.0f;
        fontSize *= scale;
        float width = text.Length * fontSize * 0.55f;
        return new ImVec2(width, fontSize);
    }

    private static void AdvanceCursorForItem(ImGuiContext ctx, ImGuiWindow window, ImRect bb)
    {
        window.DC.LastItemRect = bb;
        window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, bb.Max.y + ctx.Style.ItemSpacing.y);
    }
}
