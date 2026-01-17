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

    public static int GetFrameCount()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ctx.FrameCount;
    }

    public static double GetTime()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ctx.Time;
    }

    public static void SetNextWindowPos(ImVec2 pos)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.NextWindowData.Pos = pos;
        ctx.NextWindowData.HasPos = true;
    }

    public static void SetNextWindowSize(ImVec2 size)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.NextWindowData.Size = size;
        ctx.NextWindowData.HasSize = true;
    }

    public static void NewFrame()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.FrameCount++;
        if (ctx.IO.DeltaTime <= 0f)
            ctx.IO.DeltaTime = 1f / 60f;
        ctx.Time += ctx.IO.DeltaTime;
        ctx.HoveredId = 0;
        ctx.ActiveIdJustActivated = false;
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
        if (ctx.CurrentWindow != null)
            ctx.WindowStack.Push(ctx.CurrentWindow);
        var window = ctx.Windows.Find(w => w.Name == name);
        if (window == null)
        {
            window = new ImGuiWindow(name);
            ctx.Windows.Add(window);
        }
        ctx.CurrentWindow = window;
        window.ID = ImHash.Hash(name);
        ctx.IDStack.Push(ImHash.Hash(name, ctx.IDStack.Peek()));
        window.DrawList.Clear();
        if (window.Size.x <= 0 && window.Size.y <= 0)
            window.Size = new ImVec2(400, 400);
        if (ctx.NextWindowData.HasPos)
            window.Pos = ctx.NextWindowData.Pos;
        if (ctx.NextWindowData.HasSize)
            window.Size = ctx.NextWindowData.Size;
        ctx.NextWindowData.Clear();
        window.DC.CursorStartPos = ctx.Style.WindowPadding;
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
        ctx.CurrentWindow = ctx.WindowStack.Count > 0 ? ctx.WindowStack.Pop() : null;
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

    public static ImGuiID GetItemID()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ctx.LastItemID;
    }

    public static ImDrawList GetWindowDrawList()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        return window.DrawList;
    }

    public static ImVec2 GetWindowPos()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        return window.Pos;
    }

    public static ImVec2 GetWindowSize()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        return window.Size;
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
    }

    public static ImVec2 GetCursorScreenPos()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        return new ImVec2(window.Pos.x + window.DC.CursorPos.x, window.Pos.y + window.DC.CursorPos.y);
    }

    public static void SetCursorScreenPos(ImVec2 screenPos)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        window.DC.CursorPos = new ImVec2(screenPos.x - window.Pos.x, screenPos.y - window.Pos.y);
    }

    public static ImVec2 GetContentRegionAvail()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var style = ctx.Style;
        var contentSize = new ImVec2(
            Math.Max(0, window.Size.x - style.WindowPadding.x * 2),
            Math.Max(0, window.Size.y - style.WindowPadding.y * 2));
        var offset = window.DC.CursorPos - window.DC.CursorStartPos;
        return new ImVec2(
            Math.Max(0, contentSize.x - offset.x),
            Math.Max(0, contentSize.y - offset.y));
    }

    public static ImVec2 GetMousePos()
    {
        ref var io = ref GetIO();
        return io.MousePos;
    }

    public static bool IsMouseDown(int button)
    {
        ref var io = ref GetIO();
        return (uint)button < io.MouseDown.Length && io.MouseDown[button];
    }

    public static bool IsMouseClicked(int button)
    {
        ref var io = ref GetIO();
        return (uint)button < io.MouseClicked.Length && io.MouseClicked[button];
    }

    public static bool IsMouseReleased(int button)
    {
        ref var io = ref GetIO();
        return (uint)button < io.MouseReleased.Length && io.MouseReleased[button];
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

    public static void Spacing()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        float y = window.DC.CursorPos.y + ctx.Style.ItemSpacing.y;
        window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, y);
        window.DC.LastItemRect = new ImRect(window.DC.CursorPos, window.DC.CursorPos);
    }

    public static void Dummy(ImVec2 size)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var bb = new ImRect(window.DC.CursorPos, new ImVec2(window.DC.CursorPos.x + size.x, window.DC.CursorPos.y + size.y));
        AdvanceCursorForItem(ctx, window, bb);
        window.DC.LastItemId = 0;
    }

    public static void Separator()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var start = new ImVec2(window.DC.CursorStartPos.x, window.DC.CursorPos.y);
        var end = new ImVec2(window.Size.x - ctx.Style.WindowPadding.x, window.DC.CursorPos.y + 1.0f);
        var startScreen = ToScreen(window, start);
        var endScreen = ToScreen(window, end);
        window.DrawList.AddRectFilled(startScreen, endScreen, GetColorU32(ImGuiCol_.ImGuiCol_Separator));
        AdvanceCursorForItem(ctx, window, new ImRect(start, new ImVec2(end.x, end.y)));
    }

    public static void Text(string text) => TextUnformatted(text);

    public static void TextUnformatted(string text)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var size = CalcTextSize(text, ctx);
        var pos = window.DC.CursorPos;
        var posScreen = ToScreen(window, pos);
        uint col = GetColorU32(ImGuiCol_.ImGuiCol_Text);
        window.DrawList.AddText(posScreen, col, text);
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
        var style = ctx.Style;
        var labelSize = CalcTextSize(label, ctx);
        var size = new ImVec2(labelSize.x + style.FramePadding.x * 2, labelSize.y + style.FramePadding.y * 2);
        var min = pos;
        var max = new ImVec2(pos.x + size.x, pos.y + size.y);
        var bb = new ImRect(min, max);
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));

        ref var io = ref ctx.IO;
        bool hovered = bbScreen.Contains(io.MousePos);
        bool pressed = hovered && io.MouseClicked[0];
        if (hovered)
            ctx.HoveredId = id;
        if (pressed)
        {
            ctx.ActiveId = id;
            ctx.ActiveIdMouseButton = 0;
            ctx.ActiveIdJustActivated = true;
        }
        if (ctx.ActiveId == id && io.MouseReleased[0])
        {
            ctx.ActiveId = 0;
            ctx.ActiveIdMouseButton = -1;
        }
        uint col = GetColorU32(ImGuiCol_.ImGuiCol_Button);
        if (hovered && io.MouseDown[0])
            col = GetColorU32(ImGuiCol_.ImGuiCol_ButtonActive);
        else if (hovered)
            col = GetColorU32(ImGuiCol_.ImGuiCol_ButtonHovered);

        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, col);
        var textPos = new ImVec2(bbScreen.Min.x + style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y);
        window.DrawList.AddText(textPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), label);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        return pressed;
    }

    public static bool Checkbox(string label, ref bool v)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        float squareSz = ctx.Style.FontSizeBase + style.FramePadding.y * 2;
        var pos = window.DC.CursorPos;
        var boxMin = pos;
        var boxMax = new ImVec2(pos.x + squareSz, pos.y + squareSz);
        var labelSize = CalcTextSize(label, ctx);
        var textPos = new ImVec2(boxMax.x + style.ItemSpacing.x, pos.y + style.FramePadding.y);
        var bb = new ImRect(pos, new ImVec2(textPos.x + labelSize.x, boxMax.y));
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));

        ref var io = ref ctx.IO;
        bool hovered = bbScreen.Contains(io.MousePos);
        bool pressed = hovered && io.MouseClicked[0];
        if (hovered)
            ctx.HoveredId = id;
        if (pressed)
        {
            ctx.ActiveId = id;
            ctx.ActiveIdMouseButton = 0;
            ctx.ActiveIdJustActivated = true;
            v = !v;
        }
        if (ctx.ActiveId == id && io.MouseReleased[0])
        {
            ctx.ActiveId = 0;
            ctx.ActiveIdMouseButton = -1;
        }

        uint boxCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBg);
        if (hovered && io.MouseDown[0]) boxCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgActive);
        else if (hovered) boxCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgHovered);

        window.DrawList.AddRectFilled(ToScreen(window, boxMin), ToScreen(window, boxMax), boxCol);
        if (v)
        {
            var pad = 3.0f;
            window.DrawList.AddRectFilled(
                ToScreen(window, new ImVec2(boxMin.x + pad, boxMin.y + pad)),
                ToScreen(window, new ImVec2(boxMax.x - pad, boxMax.y - pad)),
                GetColorU32(ImGuiCol_.ImGuiCol_CheckMark));
        }
        window.DrawList.AddText(textPos + window.Pos, GetColorU32(ImGuiCol_.ImGuiCol_Text), label);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        return pressed;
    }

    public static bool IsItemHovered()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var bb = new ImRect(ToScreen(window, window.DC.LastItemRect.Min), ToScreen(window, window.DC.LastItemRect.Max));
        return bb.Contains(ctx.IO.MousePos);
    }

    public static bool IsItemActive()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ctx.ActiveId != 0 && ctx.ActiveId == ctx.LastItemID;
    }

    public static ImVec2 GetItemRectMin()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        return ToScreen(window, window.DC.LastItemRect.Min);
    }

    public static ImVec2 GetItemRectMax()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        return ToScreen(window, window.DC.LastItemRect.Max);
    }

    public static ImVec2 GetItemRectSize()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        return window.DC.LastItemRect.Size;
    }

    public static uint GetColorU32(ImGuiCol_ idx)
    {
        ref var style = ref GetStyle();
        return ColorConvertFloat4ToU32(style.Colors[(int)idx]);
    }

    public static float GetTextLineHeight()
    {
        ref var style = ref GetStyle();
        return style.FontSizeBase + style.FramePadding.y * 2.0f;
    }

    public static float GetTextLineHeightWithSpacing()
    {
        ref var style = ref GetStyle();
        return style.FontSizeBase + style.FramePadding.y * 2.0f + style.ItemSpacing.y;
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

    private static ImVec2 ToScreen(ImGuiWindow window, ImVec2 local)
    {
        return new ImVec2(window.Pos.x + local.x, window.Pos.y + local.y);
    }
}
