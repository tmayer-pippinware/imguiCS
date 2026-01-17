using System;
using System.Collections.Generic;

namespace ImGui;

public static partial class ImGui
{
    private static ImGuiContext? _currentContext;
    private static readonly ImDrawData _drawData = new();
    private static bool _lastItemToggledOpen;
    private static string _fallbackClipboard = string.Empty;

    public static ImGuiContext CreateContext(ImFontAtlas? sharedFontAtlas = null)
    {
        var ctx = new ImGuiContext();
        SetCurrentContext(ctx);
        ctx.IO.Fonts = sharedFontAtlas ?? new ImFontAtlas();
        if (ctx.IO.Fonts.TexPixelsRGBA32 == null)
            BuildDefaultFontAtlas(ctx.IO);
        return ctx;
    }

    private static void BuildDefaultFontAtlas(ImGuiIO io)
    {
        // Minimal baked atlas: a single white pixel, to be replaced by stb bake.
        if (io.Fonts == null)
            return;
        const int cellSize = 8;
        const int glyphs = 95; // printable ASCII 32..126
        const int cols = 16;
        int rows = (glyphs + cols - 1) / cols;
        int width = cols * cellSize;
        int height = rows * cellSize;
        byte[] pixels = new byte[width * height * 4];

        for (int i = 0; i < glyphs; i++)
        {
            int gx = i % cols;
            int gy = i / cols;
            for (int y = 1; y < cellSize - 1; y++)
            {
                for (int x = 1; x < cellSize - 1; x++)
                {
                    int px = gx * cellSize + x;
                    int py = gy * cellSize + y;
                    int idx = (py * width + px) * 4;
                    pixels[idx + 0] = 255;
                    pixels[idx + 1] = 255;
                    pixels[idx + 2] = 255;
                    pixels[idx + 3] = 255;
                }
            }
        }

        io.Fonts.TexWidth = width;
        io.Fonts.TexHeight = height;
        io.Fonts.TexPixelsRGBA32 = pixels;
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

    public static void SetNextItemWidth(float width)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.NextItemData.ItemSize = new ImVec2(width, 0);
        ctx.NextItemData.HasSize = true;
    }

    public static void PushItemWidth(float width)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.ItemWidthStack.Push(width);
    }

    public static void PopItemWidth()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        if (ctx.ItemWidthStack.Count > 0)
            ctx.ItemWidthStack.Pop();
    }

    private static bool IsClippedEx(ImGuiWindow window, ImRect bbScreen)
    {
        var clip = window.DC.ClipRect;
        return bbScreen.Max.x < clip.Min.x || bbScreen.Min.x > clip.Max.x || bbScreen.Max.y < clip.Min.y || bbScreen.Min.y > clip.Max.y;
    }

    private static bool ItemAdd(ImGuiContext ctx, ImGuiWindow window, ImRect bb, ImGuiID id)
    {
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
        if (IsClippedEx(window, bbScreen))
            return false;
        window.DC.LastItemRect = bb;
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        return true;
    }

    private static void ItemSize(ImGuiContext ctx, ImGuiWindow window, ImVec2 size)
    {
        var bb = new ImRect(window.DC.CursorPos, new ImVec2(window.DC.CursorPos.x + size.x, window.DC.CursorPos.y + size.y));
        AdvanceCursorForItem(ctx, window, bb);
    }

    public static string GetClipboardText()
    {
        ref var io = ref GetIO();
        if (io.GetClipboardTextFn != null)
            return io.GetClipboardTextFn() ?? string.Empty;
        return _fallbackClipboard;
    }

    public static void SetClipboardText(string text)
    {
        ref var io = ref GetIO();
        if (io.SetClipboardTextFn != null)
        {
            io.SetClipboardTextFn(text);
            return;
        }
        _fallbackClipboard = text ?? string.Empty;
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
        _lastItemToggledOpen = false;
        ctx.NavMoveRequest = false;
        ctx.ProcessInputEvents();
        if (ctx.NavInitRequest && ctx.NavInitResultId != 0)
        {
            ctx.NavId = ctx.NavInitResultId;
            ctx.NavInitRequest = false;
            ctx.NavInitResultId = 0;
        }
        ctx.IO.NavActive = ctx.NavId != 0 || ctx.IO.NavActive;
        ctx.IO.NavVisible = ctx.IO.NavActive;
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
        window.DC.IndentX = 0;
        window.DC.CursorStartPos = new ImVec2(ctx.Style.WindowPadding.x + window.DC.IndentX, ctx.Style.WindowPadding.y);
        window.DC.CursorPos = window.DC.CursorStartPos;
        window.DC.CursorMax = window.DC.CursorPos;
        window.DC.LastItemRect = new ImRect(window.DC.CursorPos, window.DC.CursorPos);
        window.DC.ClipRect = new ImRect(window.Pos, new ImVec2(window.Pos.x + window.Size.x, window.Pos.y + window.Size.y));
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
        ctx.NextItemData.Clear();
    }

    public static bool BeginMainMenuBar()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var io = ctx.IO;
        float width = io.DisplaySize.x > 0 ? io.DisplaySize.x : 400;
        float height = GetTextLineHeight() + ctx.Style.FramePadding.y;
        SetNextWindowPos(new ImVec2(0, 0));
        SetNextWindowSize(new ImVec2(width, height));
        return Begin("##MainMenuBar");
    }

    public static void EndMainMenuBar()
    {
        End();
    }

    public static bool BeginMenuBar()
    {
        return true;
    }

    public static void EndMenuBar()
    {
    }

    public static bool BeginMenu(string label, bool enabled = true)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        if (!enabled)
            return false;
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        float height = GetTextLineHeight() + style.FramePadding.y;
        var pos = window.DC.CursorPos;
        var bb = new ImRect(pos, new ImVec2(pos.x + labelSize.x + style.FramePadding.x * 2, pos.y + height));
        ItemAdd(ctx, window, bb, id);
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, false);
        bool open = true;
        window.StateStorage.SetBool(id, open);

        uint bg = GetColorU32(ImGuiCol_.ImGuiCol_Header);
        if (hovered && ctx.IO.MouseDown[0]) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderActive);
        else if (hovered) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, bg);
        window.DrawList.AddText(new ImVec2(bbScreen.Min.x + style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y), GetColorU32(ImGuiCol_.ImGuiCol_Text), visibleLabel);

        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        if (open)
            ctx.PopupStack.Push(id);
        return open;
    }

    public static void EndMenu()
    {
        if (_currentContext == null)
            throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        if (_currentContext.PopupStack.Count > 0)
            _currentContext.PopupStack.Pop();
    }

    public static bool MenuItem(string label, string? shortcut = null, bool selected = false, bool enabled = true)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        if (!enabled)
            return false;
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        float height = Math.Max(labelSize.y, GetTextLineHeight());
        var pos = window.DC.CursorPos;
        var bb = new ImRect(pos, new ImVec2(pos.x + Math.Max(0, ctx.IO.DisplaySize.x > 0 ? ctx.IO.DisplaySize.x : labelSize.x + style.FramePadding.x * 2), pos.y + height + style.FramePadding.y * 2));
        ItemAdd(ctx, window, bb, id);
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, false);
        hovered = true;
        pressed = true;
        uint bg = selected ? GetColorU32(ImGuiCol_.ImGuiCol_Header) : GetColorU32(ImGuiCol_.ImGuiCol_WindowBg);
        if (hovered && ctx.IO.MouseDown[0]) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderActive);
        else if (hovered) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, bg);
        window.DrawList.AddText(new ImVec2(bbScreen.Min.x + style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y), GetColorU32(ImGuiCol_.ImGuiCol_Text), visibleLabel);
        if (!string.IsNullOrEmpty(shortcut))
        {
            var scSize = CalcTextSize(shortcut, ctx, hideTextAfterDoubleHash: false);
            window.DrawList.AddText(new ImVec2(bbScreen.Max.x - scSize.x - style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y), GetColorU32(ImGuiCol_.ImGuiCol_Text), shortcut);
        }
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        return pressed;
    }

    public static bool BeginChild(string str_id, ImVec2 size, bool border = false)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var parent = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");

        if (ctx.CurrentWindow != null)
            ctx.WindowStack.Push(ctx.CurrentWindow);

        var available = GetContentRegionAvail();
        if (size.x <= 0)
            size.x = available.x;
        if (size.y <= 0)
            size.y = available.y;

        string name = parent.Name + "/" + str_id;
        var child = ctx.Windows.Find(w => w.Name == name);
        if (child == null)
        {
            child = new ImGuiWindow(name);
            ctx.Windows.Add(child);
        }
        ctx.CurrentWindow = child;
        child.ID = ImHash.Hash(name);
        ctx.IDStack.Push(ImHash.Hash(name, ctx.IDStack.Peek()));
        child.DrawList.Clear();
        child.Pos = ToScreen(parent, parent.DC.CursorPos);
        child.Size = size;
        child.DC.IndentX = 0;
        child.DC.CursorStartPos = new ImVec2(ctx.Style.WindowPadding.x + child.DC.IndentX, ctx.Style.WindowPadding.y);
        child.DC.CursorPos = child.DC.CursorStartPos;
        child.DC.LastItemRect = new ImRect(child.DC.CursorPos, child.DC.CursorPos);
        child.DC.CursorMax = child.DC.CursorPos;
        child.DC.ClipRect = new ImRect(child.Pos, new ImVec2(child.Pos.x + child.Size.x, child.Pos.y + child.Size.y));

        var childBb = new ImRect(parent.DC.CursorPos, new ImVec2(parent.DC.CursorPos.x + size.x, parent.DC.CursorPos.y + size.y));
        parent.DC.LastItemRect = childBb;
        parent.DC.LastItemId = 0;
        if (border)
            parent.DrawList.AddRect(ToScreen(parent, childBb.Min), ToScreen(parent, childBb.Max), GetColorU32(ImGuiCol_.ImGuiCol_Border));
        return true;
    }

    public static void EndChild()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var child = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        if (ctx.IDStack.Count > 1)
            ctx.IDStack.Pop();
        ctx.CurrentWindow = ctx.WindowStack.Count > 0 ? ctx.WindowStack.Pop() : null;
        var parent = ctx.CurrentWindow;
        if (parent != null)
        {
            var bb = parent.DC.LastItemRect;
            AdvanceCursorForItem(ctx, parent, bb);
            parent.DC.LastItemId = 0;
            ctx.LastItemID = 0;
        }
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

    public static void PushID(int id)
    {
        PushID(id.ToString());
    }

    public static ImGuiID GetID(string label)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ImHash.Hash(label, ctx.IDStack.Peek());
    }

    public static void Indent(float indent_w = 0.0f)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        float indent = indent_w != 0.0f ? indent_w : ctx.Style.IndentSpacing;
        window.DC.IndentX += indent;
        window.DC.CursorStartPos = new ImVec2(ctx.Style.WindowPadding.x + window.DC.IndentX, window.DC.CursorStartPos.y);
        window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, window.DC.CursorPos.y);
        window.DC.TreeDepth++;
    }

    public static void Unindent(float indent_w = 0.0f)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        float indent = indent_w != 0.0f ? indent_w : ctx.Style.IndentSpacing;
        window.DC.IndentX = Math.Max(0, window.DC.IndentX - indent);
        window.DC.CursorStartPos = new ImVec2(ctx.Style.WindowPadding.x + window.DC.IndentX, window.DC.CursorStartPos.y);
        window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, window.DC.CursorPos.y);
        if (window.DC.TreeDepth > 0)
            window.DC.TreeDepth--;
    }

    public static void BeginGroup()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var data = new ImGuiGroupData
        {
            BackupCursorPos = window.DC.CursorPos,
            BackupCursorStartPos = window.DC.CursorStartPos,
            BackupIndentX = window.DC.IndentX,
            GroupMin = window.DC.CursorPos,
            GroupMax = window.DC.CursorPos
        };
        window.DC.GroupStack.Push(data);
    }

    public static void EndGroup()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        if (window.DC.GroupStack.Count == 0)
            return;
        var data = window.DC.GroupStack.Pop();
        float groupMaxX = Math.Max(data.GroupMax.x, window.DC.LastItemRect.Max.x);
        float groupMaxY = Math.Max(window.DC.CursorPos.y, window.DC.LastItemRect.Max.y);
        var bb = new ImRect(data.GroupMin, new ImVec2(groupMaxX, groupMaxY));
        window.DC.LastItemRect = bb;
        window.DC.LastItemId = 0;
        window.DC.CursorMax = new ImVec2(Math.Max(window.DC.CursorMax.x, bb.Max.x), Math.Max(window.DC.CursorMax.y, bb.Max.y));
        window.DC.IndentX = data.BackupIndentX;
        window.DC.CursorStartPos = data.BackupCursorStartPos;
        float newY = bb.Max.y + ctx.Style.ItemSpacing.y;
        window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, newY);
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

    public static void SetCursorPosX(float x)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        window.DC.CursorPos = new ImVec2(x, window.DC.CursorPos.y);
    }

    public static void SetCursorPosY(float y)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        window.DC.CursorPos = new ImVec2(window.DC.CursorPos.x, y);
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

    public static ImVec2 GetCursorStartPos()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        return window.DC.CursorStartPos;
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

    public static bool BeginTable(string str_id, int columns, ImGuiTableFlags_ flags = ImGuiTableFlags_.ImGuiTableFlags_None, ImVec2 outer_size = new ImVec2(), float inner_width = 0.0f)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        if (columns <= 0)
            return false;

        var avail = GetContentRegionAvail();
        var size = outer_size;
        if (size.x <= 0) size.x = avail.x;
        if (size.y < 0) size.y = 0;

        var tableId = GetID(str_id);
        var rowHeight = GetTextLineHeightWithSpacing();
        var table = new ImGuiTable(tableId, columns, size, window.DC.CursorPos, rowHeight);
        ctx.TableStack.Push(table);
        ctx.CurrentTable = table;
        return true;
    }

    public static void EndTable()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        if (ctx.CurrentTable == null)
            return;
        var table = ctx.CurrentTable;
        float rows = table.CurrentRow >= 0 ? table.CurrentRow + 1 : 0;
        float height = rows * table.RowHeight;
        window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, table.WorkPos.y + height + ctx.Style.CellPadding.y);
        ctx.TableStack.Pop();
        ctx.CurrentTable = ctx.TableStack.Count > 0 ? ctx.TableStack.Peek() : null;
    }

    public static bool TableNextRow(ImGuiTableRowFlags_ flags = ImGuiTableRowFlags_.ImGuiTableRowFlags_None, float min_row_height = 0.0f)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var table = ctx.CurrentTable ?? throw new InvalidOperationException("No current table. Call BeginTable() first.");
        table.CurrentRow++;
        table.CurrentColumn = -1;
        float baseHeight = GetTextLineHeightWithSpacing();
        table.RowHeight = Math.Max(min_row_height > 0 ? min_row_height : baseHeight, baseHeight);
        table.WorkPos = new ImVec2(table.WorkPos.x, table.WorkPos.y + table.RowHeight);
        return true;
    }

    public static bool TableNextColumn()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var table = ctx.CurrentTable ?? throw new InvalidOperationException("No current table. Call BeginTable() first.");
        table.CurrentColumn++;
        if (table.CurrentColumn >= table.ColumnsCount)
            return false;
        float columnWidth = table.ColumnsCount > 0 ? table.OuterSize.x / table.ColumnsCount : table.OuterSize.x;
        float x = table.WorkPos.x + columnWidth * table.CurrentColumn;
        float y = table.WorkPos.y;
        window.DC.CursorPos = new ImVec2(x, y);
        window.DC.CursorStartPos = new ImVec2(x, y);
        return true;
    }

    public static int TableGetColumnIndex()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var table = ctx.CurrentTable;
        return table?.CurrentColumn ?? -1;
    }

    public static int TableGetRowIndex()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var table = ctx.CurrentTable;
        return table?.CurrentRow ?? -1;
    }

    public static void TableSetupColumn(string label, ImGuiTableColumnFlags_ flags = ImGuiTableColumnFlags_.ImGuiTableColumnFlags_None, float init_width_or_weight = 0.0f, ImGuiID user_id = 0)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var table = ctx.CurrentTable ?? throw new InvalidOperationException("No current table. Call BeginTable() first.");
        if (table.ColumnNames.Count < table.ColumnsCount)
            table.ColumnNames.Add(label);
        else if (table.ColumnNames.Count > 0)
            table.ColumnNames[Math.Min(table.ColumnNames.Count - 1, table.ColumnsCount - 1)] = label;
    }

    public static string? TableGetColumnName(int column_n)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call BeginTable() first.");
        var table = ctx.CurrentTable ?? throw new InvalidOperationException("No current table. Call BeginTable() first.");
        if ((uint)column_n >= table.ColumnNames.Count)
            return null;
        return table.ColumnNames[column_n];
    }

    public static ImGuiTableColumnFlags_ TableGetColumnFlags()
    {
        return ImGuiTableColumnFlags_.ImGuiTableColumnFlags_None;
    }

    public static void TableSetColumnIndex(int column_n)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var table = ctx.CurrentTable ?? throw new InvalidOperationException("No current table. Call BeginTable() first.");
        table.CurrentColumn = column_n - 1;
        TableNextColumn();
    }

    public static ImGuiTableColumnSortSpecs TableGetColumnSortSpecs(int column_n)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var table = ctx.CurrentTable ?? throw new InvalidOperationException("No current table. Call BeginTable() first.");
        if ((uint)column_n >= table.ColumnsCount)
            throw new ArgumentOutOfRangeException(nameof(column_n));
        EnsureTableSortSpecs(table);
        return table.SortSpecsData[column_n];
    }

    public static ImGuiTableSortSpecs TableGetSortSpecs()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call BeginTable() first.");
        var table = ctx.CurrentTable ?? throw new InvalidOperationException("No current table. Call BeginTable() first.");
        EnsureTableSortSpecs(table);
        return table.SortSpecs;
    }

    private static void EnsureTableSortSpecs(ImGuiTable table)
    {
        if (table.SortSpecsData.Length == 0)
        {
            table.SortSpecsData = new ImGuiTableColumnSortSpecs[table.ColumnsCount];
            for (int i = 0; i < table.ColumnsCount; i++)
            {
                table.SortSpecsData[i] = new ImGuiTableColumnSortSpecs { ColumnIndex = i, SortDirection = ImGuiSortDirection.ImGuiSortDirection_None };
            }
        }
        table.SortSpecs.Specs = table.SortSpecsData;
        table.SortSpecs.SpecsCount = table.SortSpecsData.Length;
    }

    public static void TableHeadersRow()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var table = ctx.CurrentTable ?? throw new InvalidOperationException("No current table. Call BeginTable() first.");
        TableNextRow(ImGuiTableRowFlags_.ImGuiTableRowFlags_Headers, GetTextLineHeightWithSpacing());
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        float columnWidth = table.ColumnsCount > 0 ? table.OuterSize.x / table.ColumnsCount : table.OuterSize.x;
        for (int col = 0; col < table.ColumnsCount; col++)
        {
            TableNextColumn();
            string label = (col < table.ColumnNames.Count && table.ColumnNames[col] != null) ? table.ColumnNames[col]! : string.Empty;
            var pos = window.DC.CursorPos;
            var bb = new ImRect(pos, new ImVec2(pos.x + columnWidth, pos.y + table.RowHeight));
            var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
            window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, GetColorU32(ImGuiCol_.ImGuiCol_TableHeaderBg));
            window.DrawList.AddText(new ImVec2(bbScreen.Min.x + ctx.Style.CellPadding.x, bbScreen.Min.y + ctx.Style.CellPadding.y), GetColorU32(ImGuiCol_.ImGuiCol_Text), label);
            var specs = TableGetColumnSortSpecs(col);
            if (ctx.IO.MouseClicked[0])
            {
                specs.SortDirection = specs.SortDirection == ImGuiSortDirection.ImGuiSortDirection_Ascending
                    ? ImGuiSortDirection.ImGuiSortDirection_Descending
                    : ImGuiSortDirection.ImGuiSortDirection_Ascending;
                table.SortSpecs!.SpecsDirty = true;
            }
        }
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

    public static void AlignTextToFramePadding()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        window.DC.CursorPos = new ImVec2(window.DC.CursorPos.x, window.DC.CursorPos.y + ctx.Style.FramePadding.y);
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
        text ??= string.Empty;
        int end = FindRenderedTextEnd(text);
        string display = end == text.Length ? text : text.Substring(0, end);
        var size = CalcTextSize(display, ctx, hideTextAfterDoubleHash: false);
        var pos = window.DC.CursorPos;
        var posScreen = ToScreen(window, pos);
        uint col = GetColorU32(ImGuiCol_.ImGuiCol_Text);
        window.DrawList.AddText(posScreen, col, display);
        AdvanceCursorForItem(ctx, window, new ImRect(pos, new ImVec2(pos.x + size.x, pos.y + size.y)));
        window.DC.LastItemId = 0;
        ctx.LastItemID = 0;
    }

    public static void TextColored(ImVec4 col, string text)
    {
        PushStyleColor(ImGuiCol_.ImGuiCol_Text, col);
        TextUnformatted(text);
        PopStyleColor();
    }

    public static void TextDisabled(string text)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var disabledCol = ctx.Style.Colors[(int)ImGuiCol_.ImGuiCol_TextDisabled];
        PushStyleColor(ImGuiCol_.ImGuiCol_Text, disabledCol);
        TextUnformatted(text);
        PopStyleColor();
    }

    public static void TextWrapped(string text)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        float wrapWidth = GetContentRegionAvail().x;
        if (wrapWidth <= 0)
        {
            TextUnformatted(text);
            return;
        }

        text ??= string.Empty;
        int end = FindRenderedTextEnd(text);
        string display = end == text.Length ? text : text.Substring(0, end);
        var hardLines = display.Split('\n');
        var lines = new List<string>();
        foreach (var hard in hardLines)
        {
            var words = hard.Split(' ');
            string current = "";
            foreach (var word in words)
            {
                var candidate = string.IsNullOrEmpty(current) ? word : current + " " + word;
                float candidateWidth = CalcTextSize(candidate, ctx, hideTextAfterDoubleHash: false).x;
                if (candidateWidth > wrapWidth && !string.IsNullOrEmpty(current))
                {
                    lines.Add(current);
                    current = word;
                }
                else
                {
                    current = candidate;
                }
            }
            lines.Add(current);
        }

        var basePos = window.DC.CursorPos;
        float maxLineWidth = 0.0f;
        float y = basePos.y;
        foreach (var line in lines)
        {
            var lineSize = CalcTextSize(line, ctx, hideTextAfterDoubleHash: false);
            window.DrawList.AddText(ToScreen(window, new ImVec2(basePos.x, y)), GetColorU32(ImGuiCol_.ImGuiCol_Text), line);
            maxLineWidth = Math.Max(maxLineWidth, lineSize.x);
            y += lineSize.y;
        }
        var bb = new ImRect(basePos, new ImVec2(basePos.x + maxLineWidth, y));
        AdvanceCursorForItem(ctx, window, bb);
        window.DC.LastItemId = 0;
        ctx.LastItemID = 0;
    }

    public static void BulletText(string text)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        text ??= string.Empty;
        int end = FindRenderedTextEnd(text);
        string display = end == text.Length ? text : text.Substring(0, end);
        float lineHeight = GetTextLineHeight();
        float bulletRadius = lineHeight * 0.2f;
        var pos = window.DC.CursorPos;
        var bulletMin = ToScreen(window, new ImVec2(pos.x, pos.y + lineHeight * 0.5f - bulletRadius));
        var bulletMax = ToScreen(window, new ImVec2(pos.x + bulletRadius * 2, pos.y + lineHeight * 0.5f + bulletRadius));
        window.DrawList.AddRectFilled(bulletMin, bulletMax, GetColorU32(ImGuiCol_.ImGuiCol_Text));
        var textPos = new ImVec2(pos.x + bulletRadius * 2 + ctx.Style.ItemSpacing.x, pos.y);
        window.DrawList.AddText(ToScreen(window, textPos), GetColorU32(ImGuiCol_.ImGuiCol_Text), display);
        var textSize = CalcTextSize(display, ctx, hideTextAfterDoubleHash: false);
        var bb = new ImRect(pos, new ImVec2(textPos.x + textSize.x, pos.y + Math.Max(textSize.y, lineHeight)));
        AdvanceCursorForItem(ctx, window, bb);
        window.DC.LastItemId = 0;
        ctx.LastItemID = 0;
    }

    public static bool TreeNode(string label)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        ImGuiID id = GetID(label);
        bool open = window.StateStorage.GetBool(id, false);
        var visibleLabel = GetLabelText(label);
        var style = ctx.Style;
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        float height = GetTextLineHeightWithSpacing();
        var pos = window.DC.CursorPos;
        var bb = new ImRect(pos, new ImVec2(pos.x + labelSize.x + style.FramePadding.x * 2 + height, pos.y + height));
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));

        ref var io = ref ctx.IO;
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered = !disabled && bbScreen.Contains(io.MousePos);
        bool pressed = hovered && io.MouseClicked[0];
        _lastItemToggledOpen = false;
        if (hovered && !disabled)
            ctx.HoveredId = id;
        if (pressed && !disabled)
        {
            ctx.ActiveId = id;
            ctx.ActiveIdMouseButton = 0;
            ctx.ActiveIdJustActivated = true;
            open = !open;
            _lastItemToggledOpen = true;
        }
        if (ctx.ActiveId == id && io.MouseReleased[0])
        {
            ctx.ActiveId = 0;
            ctx.ActiveIdMouseButton = -1;
        }

        uint bg = GetColorU32(ImGuiCol_.ImGuiCol_Header);
        if (hovered && io.MouseDown[0]) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderActive);
        else if (hovered) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, bg);

        string arrow = open ? "v " : "> ";
        var textPos = new ImVec2(bbScreen.Min.x + style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y);
        window.DrawList.AddText(textPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), arrow + visibleLabel);

        window.StateStorage.SetBool(id, open);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        if (ctx.NavInitRequest && ctx.NavInitResultId == 0)
            ctx.NavInitResultId = id;
        AdvanceCursorForItem(ctx, window, bb);
        if (open)
        {
            window.DC.TreeDepth++;
            window.DC.IndentX += style.IndentSpacing;
            window.DC.CursorStartPos = new ImVec2(style.WindowPadding.x + window.DC.IndentX, window.DC.CursorStartPos.y);
            window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, window.DC.CursorPos.y);
        }
        ctx.NextItemData.Clear();
        return open;
    }

    public static void TreePop()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        if (window.DC.TreeDepth > 0)
        {
            window.DC.TreeDepth--;
            window.DC.IndentX = Math.Max(0, window.DC.IndentX - ctx.Style.IndentSpacing);
            window.DC.CursorStartPos = new ImVec2(ctx.Style.WindowPadding.x + window.DC.IndentX, window.DC.CursorStartPos.y);
            window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, window.DC.CursorPos.y);
        }
    }

    public static void TreePush(string str_id)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        ctx.IDStack.Push(ImHash.Hash(str_id, ctx.IDStack.Peek()));
        window.DC.TreeDepth++;
        window.DC.IndentX += ctx.Style.IndentSpacing;
        window.DC.CursorStartPos = new ImVec2(ctx.Style.WindowPadding.x + window.DC.IndentX, window.DC.CursorStartPos.y);
        window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, window.DC.CursorPos.y);
    }

    public static void TreePush()
    {
        TreePush(string.Empty);
    }

    public static void SetItemDefaultFocus()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        if (ctx.LastItemID != 0)
        {
            ctx.NavId = ctx.LastItemID;
            ctx.NavInitRequest = false;
            ctx.NavInitResultId = 0;
            ctx.IO.NavActive = true;
            ctx.IO.NavVisible = true;
        }
    }

    public static bool CollapsingHeader(string label)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        ImGuiID id = GetID(label);
        bool open = window.StateStorage.GetBool(id, true);
        bool toggled = CollapsingHeaderInternal(label, id, ref open);
        window.StateStorage.SetBool(id, open);
        return open;
    }

    public static bool CollapsingHeader(string label, ref bool open)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        ImGuiID id = GetID(label);
        CollapsingHeaderInternal(label, id, ref open);
        window.StateStorage.SetBool(id, open);
        return open;
    }

    public static bool TreeNodeEx(string label, ImGuiTreeNodeFlags_ flags = ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_None)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        bool defaultOpen = (flags & ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_DefaultOpen) != 0;
        bool leaf = (flags & ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_Leaf) != 0;
        bool framed = (flags & ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_Framed) != 0;
        bool open = window.StateStorage.GetBool(id, defaultOpen);
        if (leaf)
            open = true;

        var style = ctx.Style;
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        float height = framed ? (style.FramePadding.y * 2 + style.FontSizeBase) : GetTextLineHeightWithSpacing();
        var pos = window.DC.CursorPos;
        float padding = framed ? style.FramePadding.x : 0;
        var bb = new ImRect(pos, new ImVec2(pos.x + labelSize.x + padding * 2 + height, pos.y + height));
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));

        ref var io = ref ctx.IO;
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered = !disabled && bbScreen.Contains(io.MousePos);
        bool pressed = hovered && io.MouseClicked[0] && !leaf;
        _lastItemToggledOpen = false;
        if (hovered && !disabled)
            ctx.HoveredId = id;
        if (pressed && !disabled)
        {
            ctx.ActiveId = id;
            ctx.ActiveIdMouseButton = 0;
            ctx.ActiveIdJustActivated = true;
            open = !open;
            _lastItemToggledOpen = true;
        }
        if (ctx.ActiveId == id && io.MouseReleased[0])
        {
            ctx.ActiveId = 0;
            ctx.ActiveIdMouseButton = -1;
        }

        uint bg = framed ? GetColorU32(ImGuiCol_.ImGuiCol_Header) : GetColorU32(ImGuiCol_.ImGuiCol_WindowBg);
        if (framed)
        {
            if (hovered && io.MouseDown[0]) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderActive);
            else if (hovered) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderHovered);
            window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, bg);
        }

        string arrow = leaf ? " " : (open ? "v " : "> ");
        var textPos = new ImVec2(bbScreen.Min.x + style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y);
        window.DrawList.AddText(textPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), arrow + visibleLabel);

        window.StateStorage.SetBool(id, open);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        if (ctx.NavInitRequest && ctx.NavInitResultId == 0)
            ctx.NavInitResultId = id;
        AdvanceCursorForItem(ctx, window, bb);
        if (open && !leaf)
        {
            window.DC.TreeDepth++;
            window.DC.IndentX += style.IndentSpacing;
            window.DC.CursorStartPos = new ImVec2(style.WindowPadding.x + window.DC.IndentX, window.DC.CursorStartPos.y);
            window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, window.DC.CursorPos.y);
        }
        ctx.NextItemData.Clear();
        return open;
    }

    public static void BeginDisabled(bool disabled = true)
    {
        if (!disabled)
            return;
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.StyleAlphaStack.Push(ctx.Style.Alpha);
        ctx.Style.Alpha *= ctx.Style.DisabledAlpha;
        ctx.DisabledDepth++;
    }

    public static void EndDisabled()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        if (ctx.DisabledDepth <= 0)
            return;
        ctx.DisabledDepth--;
        if (ctx.StyleAlphaStack.Count > 0)
            ctx.Style.Alpha = ctx.StyleAlphaStack.Pop();
    }

    public static bool Button(string label, bool repeat = false)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        var pos = window.DC.CursorPos;
        var style = ctx.Style;
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        var size = new ImVec2(labelSize.x + style.FramePadding.x * 2, labelSize.y + style.FramePadding.y * 2);
        if (ctx.NextItemData.HasSize && ctx.NextItemData.ItemSize.x > 0)
            size.x = ctx.NextItemData.ItemSize.x;
        var min = pos;
        var max = new ImVec2(pos.x + size.x, pos.y + size.y);
        var bb = new ImRect(min, max);
        if (!ItemAdd(ctx, window, bb, id))
        {
            ItemSize(ctx, window, size);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));

        ref var io = ref ctx.IO;
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled, repeat);
        uint col = GetColorU32(ImGuiCol_.ImGuiCol_Button);
        if (hovered && io.MouseDown[0])
            col = GetColorU32(ImGuiCol_.ImGuiCol_ButtonActive);
        else if (hovered)
            col = GetColorU32(ImGuiCol_.ImGuiCol_ButtonHovered);

        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, col);
        var textPos = new ImVec2(bbScreen.Min.x + style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y);
        window.DrawList.AddText(textPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), visibleLabel);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        return pressed;
    }

    public static bool Checkbox(string label, ref bool v)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        float squareSz = ctx.Style.FontSizeBase + style.FramePadding.y * 2;
        var pos = window.DC.CursorPos;
        var boxMin = pos;
        var boxMax = new ImVec2(pos.x + squareSz, pos.y + squareSz);
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        var textPos = new ImVec2(boxMax.x + style.ItemSpacing.x, pos.y + style.FramePadding.y);
        var bb = new ImRect(pos, new ImVec2(textPos.x + labelSize.x, boxMax.y));
        if (!ItemAdd(ctx, window, bb, id))
        {
            ItemSize(ctx, window, bb.Size);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));

        ref var io = ref ctx.IO;
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled);
        if (pressed && !disabled)
            v = !v;

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
        window.DrawList.AddText(textPos + window.Pos, GetColorU32(ImGuiCol_.ImGuiCol_Text), visibleLabel);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        return pressed;
    }

    public static bool Selectable(string label, ImGuiSelectableFlags_ flags = ImGuiSelectableFlags_.ImGuiSelectableFlags_None, ImVec2? size = null)
    {
        bool selected = false;
        return Selectable(label, ref selected, flags, size);
    }

    public static bool Selectable(string label, ref bool selected, ImGuiSelectableFlags_ flags = ImGuiSelectableFlags_.ImGuiSelectableFlags_None, ImVec2? size = null)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        var itemSize = new ImVec2(
            size?.x > 0 ? size.Value.x : labelSize.x + style.FramePadding.x * 2,
            size?.y > 0 ? size.Value.y : Math.Max(labelSize.y + style.FramePadding.y * 2, GetTextLineHeight()));
        var pos = window.DC.CursorPos;
        var bb = new ImRect(pos, new ImVec2(pos.x + itemSize.x, pos.y + itemSize.y));
        if (!ItemAdd(ctx, window, bb, id))
        {
            ItemSize(ctx, window, itemSize);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled);
        if (pressed && !disabled)
            selected = !selected;

        uint bg = selected ? GetColorU32(ImGuiCol_.ImGuiCol_Header) : GetColorU32(ImGuiCol_.ImGuiCol_WindowBg);
        if (hovered && ctx.IO.MouseDown[0]) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderActive);
        else if (hovered) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, bg);
        var textPos = new ImVec2(bbScreen.Min.x + style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y);
        window.DrawList.AddText(textPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), visibleLabel);

        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        return pressed;
    }

    public static bool SmallButton(string label)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        var padding = new ImVec2(style.FramePadding.x * 0.5f, style.FramePadding.y * 0.5f);
        var size = new ImVec2(labelSize.x + padding.x * 2, labelSize.y + padding.y * 2);
        var pos = window.DC.CursorPos;
        var bb = new ImRect(pos, new ImVec2(pos.x + size.x, pos.y + size.y));
        if (!ItemAdd(ctx, window, bb, id))
        {
            ItemSize(ctx, window, size);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled);
        uint col = GetColorU32(ImGuiCol_.ImGuiCol_Button);
        if (hovered && ctx.IO.MouseDown[0]) col = GetColorU32(ImGuiCol_.ImGuiCol_ButtonActive);
        else if (hovered) col = GetColorU32(ImGuiCol_.ImGuiCol_ButtonHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, col);
        var textPos = new ImVec2(bbScreen.Min.x + padding.x, bbScreen.Min.y + padding.y);
        window.DrawList.AddText(textPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), visibleLabel);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        return pressed;
    }

    public static bool ArrowButton(string str_id, ImGuiDir dir)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        ImGuiID id = GetID(str_id);
        float sz = GetTextLineHeight();
        var pos = window.DC.CursorPos;
        var bb = new ImRect(pos, new ImVec2(pos.x + sz, pos.y + sz));
        if (!ItemAdd(ctx, window, bb, id))
        {
            ItemSize(ctx, window, bb.Size);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled);
        uint col = GetColorU32(ImGuiCol_.ImGuiCol_Button);
        if (hovered && ctx.IO.MouseDown[0]) col = GetColorU32(ImGuiCol_.ImGuiCol_ButtonActive);
        else if (hovered) col = GetColorU32(ImGuiCol_.ImGuiCol_ButtonHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, col);
        string arrow = dir switch
        {
            ImGuiDir.ImGuiDir_Left => "<",
            ImGuiDir.ImGuiDir_Right => ">",
            ImGuiDir.ImGuiDir_Up => "^",
            _ => "v"
        };
        var textPos = new ImVec2(bbScreen.Min.x + ctx.Style.FramePadding.x, bbScreen.Min.y + ctx.Style.FramePadding.y);
        window.DrawList.AddText(textPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), arrow);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        return pressed;
    }

    public static bool InvisibleButton(string str_id, ImVec2 size)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        ImGuiID id = GetID(str_id);
        var pos = window.DC.CursorPos;
        var bb = new ImRect(pos, new ImVec2(pos.x + size.x, pos.y + size.y));
        if (!ItemAdd(ctx, window, bb, id))
        {
            ItemSize(ctx, window, size);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        return pressed;
    }

    public static bool RadioButton(string label, ref bool active)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        float radius = (ctx.Style.FontSizeBase + style.FramePadding.y * 2) * 0.5f;
        var pos = window.DC.CursorPos;
        var center = new ImVec2(pos.x + radius, pos.y + radius);
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        var textPos = new ImVec2(pos.x + radius * 2 + style.ItemSpacing.x, pos.y + style.FramePadding.y);
        var bb = new ImRect(pos, new ImVec2(textPos.x + labelSize.x, pos.y + radius * 2));
        if (!ItemAdd(ctx, window, bb, id))
        {
            ItemSize(ctx, window, bb.Size);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));

        ref var io = ref ctx.IO;
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled);
        if (pressed && !disabled)
            active = true;

        uint frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBg);
        if (hovered && io.MouseDown[0]) frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgActive);
        else if (hovered) frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgHovered);

        // Outer circle approximated by filled rect for now.
        window.DrawList.AddRectFilled(ToScreen(window, pos), ToScreen(window, new ImVec2(pos.x + radius * 2, pos.y + radius * 2)), frameCol);
        if (active)
        {
            var pad = radius * 0.5f;
            window.DrawList.AddRectFilled(
                ToScreen(window, new ImVec2(pos.x + pad, pos.y + pad)),
                ToScreen(window, new ImVec2(pos.x + radius * 2 - pad, pos.y + radius * 2 - pad)),
                GetColorU32(ImGuiCol_.ImGuiCol_CheckMark));
        }
        window.DrawList.AddText(textPos + window.Pos, GetColorU32(ImGuiCol_.ImGuiCol_Text), visibleLabel);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        return pressed;
    }

    public static bool SliderFloat(string label, ref float v, float v_min, float v_max)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var visibleLabel = GetLabelText(label);
        v = Math.Clamp(v, v_min, v_max);
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        float sliderWidth = 150.0f;
        sliderWidth = GetEffectiveItemWidth(ctx, sliderWidth);

        var pos = window.DC.CursorPos;
        var grabHeight = style.FramePadding.y * 2 + style.FontSizeBase;
        var bb = new ImRect(pos, new ImVec2(pos.x + sliderWidth, pos.y + grabHeight));
        if (!ItemAdd(ctx, window, bb, id))
        {
            ItemSize(ctx, window, bb.Size);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
        var labelPos = new ImVec2(bb.Max.x + style.ItemSpacing.x, pos.y + style.FramePadding.y);

        ref var io = ref ctx.IO;
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered = !disabled && bbScreen.Contains(io.MousePos);
        bool held = !disabled && ctx.ActiveId == id && io.MouseDown[0];
        bool pressed = hovered && io.MouseClicked[0];
        if (hovered && !disabled)
            ctx.HoveredId = id;
        if (pressed && !disabled)
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

        float oldV = v;
        if (held || (pressed && io.MouseDown[0]))
        {
            float t = (io.MousePos.x - bbScreen.Min.x) / Math.Max(1.0f, bbScreen.Max.x - bbScreen.Min.x);
            t = Math.Clamp(t, 0.0f, 1.0f);
            v = v_min + (v_max - v_min) * t;
        }

        uint frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBg);
        if (hovered && io.MouseDown[0]) frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgActive);
        else if (hovered) frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, frameCol);

        float grabT = (v_max - v_min) > 0 ? (v - v_min) / (v_max - v_min) : 0.0f;
        grabT = Math.Clamp(grabT, 0.0f, 1.0f);
        float grabWidth = Math.Max(8.0f, style.GrabMinSize);
        float grabX0 = bbScreen.Min.x + grabT * (bbScreen.Max.x - bbScreen.Min.x - grabWidth);
        var grabMin = new ImVec2(grabX0, bbScreen.Min.y);
        var grabMax = new ImVec2(grabX0 + grabWidth, bbScreen.Max.y);
        window.DrawList.AddRectFilled(grabMin, grabMax, GetColorU32(ImGuiCol_.ImGuiCol_SliderGrab));

        RenderTextClipped(window, labelPos, new ImVec2(labelPos.x + labelSize.x, labelPos.y + labelSize.y), visibleLabel, labelSize);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, new ImRect(bb.Min, new ImVec2(bb.Max.x + labelSize.x + style.ItemSpacing.x, bb.Max.y)));
        ctx.NextItemData.Clear();
        return Math.Abs(v - oldV) > float.Epsilon;
    }

    public static bool DragFloat(string label, ref float v, float v_speed = 1.0f, float v_min = float.MinValue, float v_max = float.MaxValue, string format = "%.3f")
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        float dragWidth = GetEffectiveItemWidth(ctx, 150.0f);
        var pos = window.DC.CursorPos;
        float frameHeight = style.FramePadding.y * 2 + style.FontSizeBase;
        var bb = new ImRect(pos, new ImVec2(pos.x + dragWidth, pos.y + frameHeight));
        if (!ItemAdd(ctx, window, bb, id))
        {
            ItemSize(ctx, window, bb.Size);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
        var labelPos = new ImVec2(bb.Max.x + style.ItemSpacing.x, pos.y + style.FramePadding.y);

        ref var io = ref ctx.IO;
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled, repeat: true);
        if (pressed && !disabled)
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

        float old = v;
        if ((held || pressed) && !disabled)
        {
            float delta = io.MouseDelta.x * v_speed;
            if (float.IsInfinity(delta) || float.IsNaN(delta))
                delta = 0.0f;
            v += delta;
            if (v_min <= v_max)
                v = Math.Clamp(v, v_min, v_max);
        }

        uint frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBg);
        if (hovered && io.MouseDown[0]) frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgActive);
        else if (hovered) frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, frameCol);

        string valueText = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.###}", v);
        RenderTextClipped(window, bb.Min + style.FramePadding, bb.Max - style.FramePadding, valueText, CalcTextSize(valueText, ctx, hideTextAfterDoubleHash: false));
        window.DrawList.AddText(labelPos + window.Pos, GetColorU32(ImGuiCol_.ImGuiCol_Text), visibleLabel);

        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, new ImRect(bb.Min, new ImVec2(bb.Max.x + labelSize.x + style.ItemSpacing.x, bb.Max.y)));
        ctx.NextItemData.Clear();
        return Math.Abs(v - old) > float.Epsilon;
    }

    public static bool InputText(string label, ref string buf, ImGuiInputTextFlags_ flags = ImGuiInputTextFlags_.ImGuiInputTextFlags_None)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        float width = GetEffectiveItemWidth(ctx, 200.0f);
        float height = style.FramePadding.y * 2 + style.FontSizeBase;
        if (ctx.NextItemData.HasSize && ctx.NextItemData.ItemSize.y > 0)
            height = ctx.NextItemData.ItemSize.y;
        var pos = window.DC.CursorPos;
        var frameBb = new ImRect(pos, new ImVec2(pos.x + width, pos.y + height));
        var totalBb = new ImRect(pos, new ImVec2(frameBb.Max.x + style.ItemSpacing.x + labelSize.x, frameBb.Max.y));
        if (!ItemAdd(ctx, window, totalBb, id))
        {
            ItemSize(ctx, window, totalBb.Size);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, frameBb.Min), ToScreen(window, frameBb.Max));

        ref var io = ref ctx.IO;
        bool disabled = ctx.DisabledDepth > 0 || (flags & ImGuiInputTextFlags_.ImGuiInputTextFlags_ReadOnly) != 0;
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled);
        if (pressed && !disabled)
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

        if (!ctx.InputTextStates.TryGetValue(id, out var textState))
        {
            textState = new ImGuiInputTextState(buf ?? string.Empty);
            textState.EditState.Cursor = textState.Buffer.Length;
            textState.EditState.SelectStart = textState.EditState.SelectEnd = textState.EditState.Cursor;
            ctx.InputTextStates[id] = textState;
        }

        bool changed = false;
        if (ctx.ActiveId == id && !disabled)
        {
            foreach (var cp in io.InputQueueCharacters)
            {
                if (cp == 0)
                    continue;
                var ch = (char)cp;
                textState.Buffer.Insert(textState.EditState.Cursor, ch.ToString());
                textState.EditState.Cursor++;
                textState.EditState.SelectStart = textState.EditState.SelectEnd = textState.EditState.Cursor;
                changed = true;
            }

            ref var back = ref GetKeyData(ref io, ImGuiKey.ImGuiKey_Backspace);
            bool initialBack = back.Down && back.DownDuration <= io.DeltaTime && back.DownDuration >= 0;
            bool repeatBack = back.Down && back.DownDuration > io.KeyRepeatDelay && Math.Abs((back.DownDuration - io.KeyRepeatDelay) % io.KeyRepeatRate) < io.DeltaTime;
            bool backPressed = initialBack || repeatBack;
            if (backPressed && textState.EditState.Cursor > 0)
            {
                textState.Buffer.Delete(textState.EditState.Cursor - 1, 1);
                textState.EditState.Cursor = Math.Max(0, textState.EditState.Cursor - 1);
                textState.EditState.SelectStart = textState.EditState.SelectEnd = textState.EditState.Cursor;
                changed = true;
            }
        }

        buf = textState.Buffer.ToString();

        uint frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBg);
        if (hovered && io.MouseDown[0]) frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgActive);
        else if (hovered) frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, frameCol);
        var textPos = frameBb.Min + style.FramePadding;
        RenderTextClipped(window, textPos, frameBb.Max - style.FramePadding, buf, CalcTextSize(buf, ctx, hideTextAfterDoubleHash: false));

        var labelPos = new ImVec2(frameBb.Max.x + style.ItemSpacing.x, pos.y + style.FramePadding.y);
        window.DrawList.AddText(labelPos + window.Pos, GetColorU32(ImGuiCol_.ImGuiCol_Text), visibleLabel);

        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, totalBb);
        ctx.NextItemData.Clear();
        return changed;
    }

    public static bool InputTextMultiline(string label, ref string buf, ImVec2 size, ImGuiInputTextFlags_ flags = ImGuiInputTextFlags_.ImGuiInputTextFlags_None)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        ctx.NextItemData.ItemSize = size;
        ctx.NextItemData.HasSize = true;
        return InputText(label, ref buf, flags);
    }

    public static bool BeginCombo(string label, string previewValue, ImGuiComboFlags_ flags = ImGuiComboFlags_.ImGuiComboFlags_None)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var visibleLabel = GetLabelText(label);
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        float width = GetEffectiveItemWidth(ctx, 200.0f);
        float height = style.FramePadding.y * 2 + style.FontSizeBase;
        var pos = window.DC.CursorPos;
        var frameBb = new ImRect(pos, new ImVec2(pos.x + width, pos.y + height));
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        var totalBb = new ImRect(pos, new ImVec2(frameBb.Max.x + style.ItemSpacing.x + labelSize.x, frameBb.Max.y));
        if (!ItemAdd(ctx, window, totalBb, id))
        {
            ItemSize(ctx, window, totalBb.Size);
            ctx.NextItemData.Clear();
            return false;
        }
        var bbScreen = new ImRect(ToScreen(window, frameBb.Min), ToScreen(window, frameBb.Max));
        bool disabled = ctx.DisabledDepth > 0;
        bool hovered, held, pressed;
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled);
        bool open = window.StateStorage.GetBool(id, false);
        if (pressed && !disabled)
            open = !open;
        window.StateStorage.SetBool(id, open);

        uint frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBg);
        if (hovered && ctx.IO.MouseDown[0]) frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgActive);
        else if (hovered) frameCol = GetColorU32(ImGuiCol_.ImGuiCol_FrameBgHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, frameCol);
        RenderTextClipped(window, frameBb.Min + style.FramePadding, frameBb.Max - style.FramePadding, previewValue, CalcTextSize(previewValue, ctx, hideTextAfterDoubleHash: false));
        var arrowPos = new ImVec2(bbScreen.Max.x - style.FramePadding.x * 2, bbScreen.Min.y + style.FramePadding.y);
        window.DrawList.AddText(arrowPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), open ? "v" : ">");
        var labelPos = new ImVec2(frameBb.Max.x + style.ItemSpacing.x, pos.y + style.FramePadding.y);
        window.DrawList.AddText(labelPos + window.Pos, GetColorU32(ImGuiCol_.ImGuiCol_Text), visibleLabel);

        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, totalBb);
        ctx.NextItemData.Clear();
        return open;
    }

    public static void EndCombo()
    {
        // Stub: no popup stack yet.
    }

    public static bool Combo(string label, ref int current_item, string[] items, int popup_max_height_in_items = -1)
    {
        if (items == null || items.Length == 0)
            return false;
        string preview = (current_item >= 0 && current_item < items.Length) ? items[current_item] : "";
        bool open = BeginCombo(label, preview);
        if (!open)
            return false;
        // Simplified: cycle selection on each open.
        current_item = (current_item + 1) % items.Length;
        EndCombo();
        return true;
    }

    public static bool ListBox(string label, ref int current_item, string[] items, int height_in_items = -1)
    {
        return Combo(label, ref current_item, items, height_in_items);
    }

    public static bool BeginDragDropSource(ImGuiDragDropFlags_ flags = ImGuiDragDropFlags_.ImGuiDragDropFlags_None)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        if (ctx.ActiveId == 0 && ctx.LastItemID == 0)
            return false;
        ctx.DragDropActive = true;
        ctx.DragDropSourceId = ctx.LastItemID;
        ctx.DragDropPayload.Clear();
        return true;
    }

    public static void EndDragDropSource()
    {
    }

    public static bool SetDragDropPayload(string type, byte[] data, ImGuiCond_ cond = ImGuiCond_.ImGuiCond_Always)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.DragDropPayload.DataType = type;
        ctx.DragDropPayload.Data = data;
        ctx.DragDropPayload.DataSize = data?.Length ?? 0;
        ctx.DragDropPayload.IsDelivery = false;
        ctx.DragDropPayload.IsPreview = false;
        return true;
    }

    public static ImGuiPayload? AcceptDragDropPayload(string type, ImGuiDragDropFlags_ flags = ImGuiDragDropFlags_.ImGuiDragDropFlags_None)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        if (!ctx.DragDropActive || ctx.DragDropPayload.DataType != type)
            return null;
        ctx.DragDropPayload.IsPreview = true;
        if (ctx.IO.MouseReleased[0])
        {
            ctx.DragDropPayload.IsDelivery = true;
            ctx.DragDropTargetId = ctx.LastItemID;
            ctx.DragDropActive = false;
        }
        return ctx.DragDropPayload;
    }

    public static bool BeginDragDropTarget()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ctx.DragDropActive;
    }

    public static void EndDragDropTarget()
    {
    }

    public static void OpenPopup(string str_id)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ImGuiID id = GetID(str_id);
        ctx.OpenPopups.Add(id);
    }

    public static bool BeginPopup(string str_id)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ImGuiID id = GetID(str_id);
        if (!ctx.OpenPopups.Contains(id))
            return false;

        if (ctx.CurrentWindow != null)
            ctx.WindowStack.Push(ctx.CurrentWindow);

        string name = "##Popup_" + str_id;
        var window = ctx.Windows.Find(w => w.Name == name);
        if (window == null)
        {
            window = new ImGuiWindow(name);
            ctx.Windows.Add(window);
        }
        ctx.CurrentWindow = window;
        window.ID = id;
        ctx.IDStack.Push(ImHash.Hash(name, ctx.IDStack.Peek()));
        window.DrawList.Clear();
        var pos = ctx.IO.MousePos;
        if (pos.x < 0 || pos.y < 0)
            pos = ImVec2.Zero;
        window.Pos = pos;
        if (window.Size.x <= 0 || window.Size.y <= 0)
            window.Size = new ImVec2(200, 120);
        window.DC.IndentX = 0;
        window.DC.CursorStartPos = new ImVec2(ctx.Style.WindowPadding.x, ctx.Style.WindowPadding.y);
        window.DC.CursorPos = window.DC.CursorStartPos;
        window.DC.CursorMax = window.DC.CursorPos;
        window.DC.LastItemRect = new ImRect(window.DC.CursorPos, window.DC.CursorPos);
        window.DC.ClipRect = new ImRect(window.Pos, new ImVec2(window.Pos.x + window.Size.x, window.Pos.y + window.Size.y));
        ctx.PopupStack.Push(id);
        return true;
    }

    public static bool BeginPopupModal(string name)
    {
        return BeginPopup(name);
    }

    public static void EndPopup()
    {
        End();
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        if (ctx.PopupStack.Count > 0)
            ctx.PopupStack.Pop();
        ctx.CurrentWindow = ctx.WindowStack.Count > 0 ? ctx.WindowStack.Pop() : null;
    }

    public static void CloseCurrentPopup()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        if (ctx.PopupStack.Count == 0)
            return;
        var id = ctx.PopupStack.Peek();
        ctx.OpenPopups.Remove(id);
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

    public static bool IsItemToggledOpen()
    {
        return _lastItemToggledOpen;
    }

    public static bool IsItemFocused()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        return ctx.LastItemID != 0 && ctx.NavId == ctx.LastItemID;
    }

    public static bool IsItemClicked(int mouse_button = 0)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var bb = new ImRect(ToScreen(window, window.DC.LastItemRect.Min), ToScreen(window, window.DC.LastItemRect.Max));
        ref var io = ref ctx.IO;
        if ((uint)mouse_button >= io.MouseClicked.Length)
            return false;
        return bb.Contains(io.MousePos) && io.MouseClicked[mouse_button];
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

    public static ImVec4 GetStyleColorVec4(ImGuiCol_ idx)
    {
        ref var style = ref GetStyle();
        return style.Colors[(int)idx];
    }

    public static void PushStyleColor(ImGuiCol_ idx, ImVec4 col)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var prev = ctx.Style.Colors[(int)idx];
        ctx.ColorStack.Push(((int)idx, prev));
        ctx.Style.Colors[(int)idx] = col;
    }

    public static void PopStyleColor(int count = 1)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        for (int i = 0; i < count; i++)
        {
            if (ctx.ColorStack.Count == 0)
                break;
            var entry = ctx.ColorStack.Pop();
            ctx.Style.Colors[entry.idx] = entry.previous;
        }
    }

    public static void PushStyleVar(ImGuiStyleVar_ idx, float val)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.StyleVarStack.Push((idx, GetStyleVarFloat(idx), ImVec2.Zero, false));
        SetStyleVar(idx, val);
    }

    public static void PushStyleVar(ImGuiStyleVar_ idx, ImVec2 val)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        ctx.StyleVarStack.Push((idx, 0f, GetStyleVarVec2(idx), true));
        SetStyleVar(idx, val);
    }

    public static void PopStyleVar(int count = 1)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        for (int i = 0; i < count; i++)
        {
            if (ctx.StyleVarStack.Count == 0)
                return;
            var entry = ctx.StyleVarStack.Pop();
            if (entry.isVec2)
                SetStyleVar(entry.idx, entry.v);
            else
                SetStyleVar(entry.idx, entry.f);
        }
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

    private static float GetStyleVarFloat(ImGuiStyleVar_ idx)
    {
        ref var style = ref GetStyle();
        return idx switch
        {
            ImGuiStyleVar_.ImGuiStyleVar_Alpha => style.Alpha,
            ImGuiStyleVar_.ImGuiStyleVar_DisabledAlpha => style.DisabledAlpha,
            ImGuiStyleVar_.ImGuiStyleVar_IndentSpacing => style.IndentSpacing,
            ImGuiStyleVar_.ImGuiStyleVar_ScrollbarSize => style.ScrollbarSize,
            ImGuiStyleVar_.ImGuiStyleVar_ScrollbarRounding => style.ScrollbarRounding,
            ImGuiStyleVar_.ImGuiStyleVar_GrabMinSize => style.GrabMinSize,
            ImGuiStyleVar_.ImGuiStyleVar_GrabRounding => style.GrabRounding,
            _ => 0f
        };
    }

    private static ImVec2 GetStyleVarVec2(ImGuiStyleVar_ idx)
    {
        ref var style = ref GetStyle();
        return idx switch
        {
            ImGuiStyleVar_.ImGuiStyleVar_WindowPadding => style.WindowPadding,
            ImGuiStyleVar_.ImGuiStyleVar_WindowMinSize => style.WindowMinSize,
            ImGuiStyleVar_.ImGuiStyleVar_WindowTitleAlign => style.WindowTitleAlign,
            ImGuiStyleVar_.ImGuiStyleVar_FramePadding => style.FramePadding,
            ImGuiStyleVar_.ImGuiStyleVar_ItemSpacing => style.ItemSpacing,
            ImGuiStyleVar_.ImGuiStyleVar_ItemInnerSpacing => style.ItemInnerSpacing,
            ImGuiStyleVar_.ImGuiStyleVar_CellPadding => style.CellPadding,
            ImGuiStyleVar_.ImGuiStyleVar_ButtonTextAlign => style.ButtonTextAlign,
            ImGuiStyleVar_.ImGuiStyleVar_SelectableTextAlign => style.SelectableTextAlign,
            _ => ImVec2.Zero
        };
    }

    private static void SetStyleVar(ImGuiStyleVar_ idx, float val)
    {
        ref var style = ref GetStyle();
        switch (idx)
        {
            case ImGuiStyleVar_.ImGuiStyleVar_Alpha: style.Alpha = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_DisabledAlpha: style.DisabledAlpha = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_IndentSpacing: style.IndentSpacing = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_ScrollbarSize: style.ScrollbarSize = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_ScrollbarRounding: style.ScrollbarRounding = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_GrabMinSize: style.GrabMinSize = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_GrabRounding: style.GrabRounding = val; break;
        }
    }

    private static void SetStyleVar(ImGuiStyleVar_ idx, ImVec2 val)
    {
        ref var style = ref GetStyle();
        switch (idx)
        {
            case ImGuiStyleVar_.ImGuiStyleVar_WindowPadding: style.WindowPadding = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_WindowMinSize: style.WindowMinSize = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_WindowTitleAlign: style.WindowTitleAlign = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_FramePadding: style.FramePadding = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_ItemSpacing: style.ItemSpacing = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_ItemInnerSpacing: style.ItemInnerSpacing = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_CellPadding: style.CellPadding = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_ButtonTextAlign: style.ButtonTextAlign = val; break;
            case ImGuiStyleVar_.ImGuiStyleVar_SelectableTextAlign: style.SelectableTextAlign = val; break;
        }
    }

    private static uint ColorConvertFloat4ToU32(ImVec4 col)
    {
        uint r = (uint)(col.x * 255.0f + 0.5f);
        uint g = (uint)(col.y * 255.0f + 0.5f);
        uint b = (uint)(col.z * 255.0f + 0.5f);
        uint a = (uint)(col.w * 255.0f + 0.5f);
        return (a << 24) | (b << 16) | (g << 8) | r;
    }

    private static int FindRenderedTextEnd(string text, int textEnd = -1)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        int length = text.Length;
        int end = textEnd >= 0 && textEnd < length ? textEnd : length;
        int hashPos = text.IndexOf("##", 0, end, StringComparison.Ordinal);
        return hashPos >= 0 ? hashPos : end;
    }

    private static string GetLabelText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        int end = FindRenderedTextEnd(text);
        return end == text.Length ? text : text.Substring(0, end);
    }

    private static ImVec2 CalcTextSize(string text, ImGuiContext ctx, bool hideTextAfterDoubleHash = true, float wrapWidth = -1.0f)
    {
        text ??= string.Empty;
        int end = hideTextAfterDoubleHash ? FindRenderedTextEnd(text) : text.Length;
        if (end <= 0)
        {
            float emptyHeight = (ctx.Style.FontSizeBase > 0 ? ctx.Style.FontSizeBase : 13.0f) * (ctx.IO.FontGlobalScale > 0 ? ctx.IO.FontGlobalScale : 1.0f);
            return new ImVec2(0, emptyHeight);
        }

        string visibleText = text.AsSpan(0, end).ToString();
        float fontSize = ctx.Style.FontSizeBase > 0 ? ctx.Style.FontSizeBase : 13.0f;
        float scale = ctx.IO.FontGlobalScale > 0 ? ctx.IO.FontGlobalScale : 1.0f;
        fontSize *= scale;
        float charWidth = fontSize * 0.55f;
        float maxWidth = 0.0f;
        float totalHeight = fontSize;
        float wrapLimit = wrapWidth > 0 ? wrapWidth : float.MaxValue;

        var hardLines = visibleText.Split('\n');
        for (int lineIndex = 0; lineIndex < hardLines.Length; lineIndex++)
        {
            var hard = hardLines[lineIndex];
            float lineWidth = 0.0f;
            var words = hard.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                float wordWidth = word.Length * charWidth;
                float space = i == 0 ? 0.0f : charWidth;
                if (wrapWidth > 0 && lineWidth + space + wordWidth > wrapLimit && lineWidth > 0.0f)
                {
                    maxWidth = Math.Max(maxWidth, lineWidth);
                    totalHeight += fontSize;
                    lineWidth = wordWidth;
                }
                else
                {
                    if (i != 0)
                        lineWidth += space;
                    lineWidth += wordWidth;
                }
            }
            maxWidth = Math.Max(maxWidth, lineWidth);
            if (lineIndex < hardLines.Length - 1)
                totalHeight += fontSize;
        }

        return new ImVec2(maxWidth, totalHeight);
    }

    private static void AdvanceCursorForItem(ImGuiContext ctx, ImGuiWindow window, ImRect bb)
    {
        window.DC.LastItemRect = bb;
        window.DC.CursorMax = new ImVec2(Math.Max(window.DC.CursorMax.x, bb.Max.x), Math.Max(window.DC.CursorMax.y, bb.Max.y));
        if (window.DC.GroupStack.Count > 0)
        {
            var top = window.DC.GroupStack.Pop();
            top.GroupMax = new ImVec2(Math.Max(top.GroupMax.x, bb.Max.x), Math.Max(top.GroupMax.y, bb.Max.y));
            window.DC.GroupStack.Push(top);
        }
        window.DC.CursorPos = new ImVec2(window.DC.CursorStartPos.x, bb.Max.y + ctx.Style.ItemSpacing.y);
    }

    private static ImVec2 ToScreen(ImGuiWindow window, ImVec2 local)
    {
        return new ImVec2(window.Pos.x + local.x, window.Pos.y + local.y);
    }

    public static void PushClipRect(ImVec2 min, ImVec2 max)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var clip = new ImRect(ToScreen(window, min), ToScreen(window, max));
        window.DC.ClipRect = clip;
        window.DrawList.PushClipRect(new ImVec4(clip.Min.x, clip.Min.y, clip.Max.x, clip.Max.y));
    }

    public static void PopClipRect()
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        window.DrawList.PopClipRect();
        window.DC.ClipRect = new ImRect(window.Pos, new ImVec2(window.Pos.x + window.Size.x, window.Pos.y + window.Size.y));
    }

    private static void RenderTextClipped(ImGuiWindow window, ImVec2 posMin, ImVec2 posMax, string text, ImVec2 textSize, ImVec2? align = null)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        text ??= string.Empty;
        int end = FindRenderedTextEnd(text);
        string display = end == text.Length ? text : text.Substring(0, end);
        var alignment = align ?? ImVec2.Zero;
        var avail = new ImVec2(Math.Max(0, posMax.x - posMin.x), Math.Max(0, posMax.y - posMin.y));
        var offset = new ImVec2(alignment.x * Math.Max(0, avail.x - textSize.x), alignment.y * Math.Max(0, avail.y - textSize.y));
        var pos = new ImVec2(posMin.x + offset.x, posMin.y + offset.y);
        window.DrawList.AddText(ToScreen(window, pos), GetColorU32(ImGuiCol_.ImGuiCol_Text), display);
    }

    public static void RenderFrame(ImVec2 p_min, ImVec2 p_max, uint fill_col)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        window.DrawList.AddRectFilled(ToScreen(window, p_min), ToScreen(window, p_max), fill_col);
    }

    public static void RenderArrow(ImVec2 pos, ImGuiDir dir, float size, uint col)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        string glyph = dir switch
        {
            ImGuiDir.ImGuiDir_Left => "<",
            ImGuiDir.ImGuiDir_Right => ">",
            ImGuiDir.ImGuiDir_Up => "^",
            _ => "v"
        };
        window.DrawList.AddText(ToScreen(window, pos), col, glyph);
    }

    public static void RenderNavHighlight(ImRect bb, uint col)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));
        window.DrawList.AddRect(bbScreen.Min, bbScreen.Max, col);
    }

    private static float GetEffectiveItemWidth(ImGuiContext ctx, float defaultWidth)
    {
        if (ctx.NextItemData.HasSize && ctx.NextItemData.ItemSize.x > 0)
            return ctx.NextItemData.ItemSize.x;
        if (ctx.ItemWidthStack.Count > 0)
            return ctx.ItemWidthStack.Peek();
        return defaultWidth;
    }

    private static bool ButtonBehavior(ImGuiContext ctx, ImRect bbScreen, ImGuiID id, out bool hovered, out bool held, out bool pressed, bool disabled, bool repeat = false)
    {
        ref var io = ref ctx.IO;
        hovered = !disabled && bbScreen.Contains(io.MousePos);
        pressed = hovered && io.MouseClicked[0];
        held = ctx.ActiveId == id && io.MouseDown[0];

        if (hovered && !disabled)
            ctx.HoveredId = id;
        if (pressed && !disabled)
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

        if (repeat && hovered && io.MouseDown[0] && !disabled)
        {
            float t = io.MouseDownDuration[0];
            if (t >= 0.0f)
            {
                float delay = io.KeyRepeatDelay;
                float rate = io.KeyRepeatRate;
                if (t == 0.0f || t > delay && Math.Abs((t - delay) % rate) < io.DeltaTime)
                    pressed = true;
            }
        }

        if (repeat && held && !disabled)
            pressed = true;

        return pressed;
    }

    private static ref ImGuiKeyData GetKeyData(ref ImGuiIO io, ImGuiKey key)
    {
        int index = (int)key - (int)ImGuiKey.ImGuiKey_NamedKey_BEGIN;
        if ((uint)index >= (uint)io.KeysData.Length)
            throw new ArgumentOutOfRangeException(nameof(key));
        return ref io.KeysData[index];
    }

    private static bool CollapsingHeaderInternal(string label, ImGuiID id, ref bool open)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var style = ctx.Style;
        var visibleLabel = GetLabelText(label);
        var labelSize = CalcTextSize(visibleLabel, ctx, hideTextAfterDoubleHash: false);
        float headerWidth = GetContentRegionAvail().x;
        if (headerWidth <= 0)
            headerWidth = labelSize.x + style.FramePadding.x * 2;
        var pos = window.DC.CursorPos;
        var bb = new ImRect(pos, new ImVec2(pos.x + headerWidth, pos.y + labelSize.y + style.FramePadding.y * 2));
        var bbScreen = new ImRect(ToScreen(window, bb.Min), ToScreen(window, bb.Max));

        ref var io = ref ctx.IO;
        bool hovered = bbScreen.Contains(io.MousePos);
        bool pressed = hovered && io.MouseClicked[0];
        if (hovered && ctx.DisabledDepth == 0)
            ctx.HoveredId = id;
        if (pressed && ctx.DisabledDepth == 0)
        {
            ctx.ActiveId = id;
            ctx.ActiveIdMouseButton = 0;
            ctx.ActiveIdJustActivated = true;
            open = !open;
        }
        if (ctx.ActiveId == id && io.MouseReleased[0])
        {
            ctx.ActiveId = 0;
            ctx.ActiveIdMouseButton = -1;
        }

        uint bg = GetColorU32(ImGuiCol_.ImGuiCol_Header);
        if (hovered && io.MouseDown[0]) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderActive);
        else if (hovered) bg = GetColorU32(ImGuiCol_.ImGuiCol_HeaderHovered);
        window.DrawList.AddRectFilled(bbScreen.Min, bbScreen.Max, bg);

        string prefix = open ? "v " : "> ";
        var textPos = new ImVec2(bbScreen.Min.x + style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y);
        window.DrawList.AddText(textPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), prefix + visibleLabel);

        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        _lastItemToggledOpen = pressed;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        return pressed;
    }
}
