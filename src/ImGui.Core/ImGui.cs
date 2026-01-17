using System;

namespace ImGui;

public static partial class ImGui
{
    private static ImGuiContext? _currentContext;
    private static readonly ImDrawData _drawData = new();
    private static bool _lastItemToggledOpen;

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
        float y = table.WorkPos.y + table.RowHeight * Math.Max(0, table.CurrentRow);
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

    public static void TextColored(ImVec4 col, string text)
    {
        PushStyleColor(ImGuiCol_.ImGuiCol_Text, col);
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

        var words = text.Split(' ');
        string line = "";
        for (int i = 0; i < words.Length; i++)
        {
            var candidate = string.IsNullOrEmpty(line) ? words[i] : line + " " + words[i];
            float lineWidth = CalcTextSize(candidate, ctx).x;
            if (lineWidth > wrapWidth && !string.IsNullOrEmpty(line))
            {
                TextUnformatted(line);
                line = words[i];
            }
            else
            {
                line = candidate;
            }
        }
        if (!string.IsNullOrEmpty(line))
            TextUnformatted(line);
    }

    public static void BulletText(string text)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        float bulletRadius = GetTextLineHeight() * 0.2f;
        var pos = window.DC.CursorPos;
        var bulletMin = ToScreen(window, new ImVec2(pos.x, pos.y + GetTextLineHeight() * 0.5f - bulletRadius));
        var bulletMax = ToScreen(window, new ImVec2(pos.x + bulletRadius * 2, pos.y + GetTextLineHeight() * 0.5f + bulletRadius));
        window.DrawList.AddRectFilled(bulletMin, bulletMax, GetColorU32(ImGuiCol_.ImGuiCol_Text));
        window.DC.CursorPos = new ImVec2(window.DC.CursorPos.x + bulletRadius * 2 + ctx.Style.ItemSpacing.x, window.DC.CursorPos.y);
        TextUnformatted(text);
    }

    public static bool TreeNode(string label)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        ImGuiID id = GetID(label);
        bool open = window.StateStorage.GetBool(id, false);
        var style = ctx.Style;
        var labelSize = CalcTextSize(label, ctx);
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

        string arrow = open ? "▼ " : "▶ ";
        var textPos = new ImVec2(bbScreen.Min.x + style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y);
        window.DrawList.AddText(textPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), arrow + label);

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

    public static bool Button(string label)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call Begin() first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        ImGuiID id = GetID(label);
        var pos = window.DC.CursorPos;
        var style = ctx.Style;
        var labelSize = CalcTextSize(label, ctx);
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
        ButtonBehavior(ctx, bbScreen, id, out hovered, out held, out pressed, disabled);
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
        ctx.NextItemData.Clear();
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
        window.DrawList.AddText(textPos + window.Pos, GetColorU32(ImGuiCol_.ImGuiCol_Text), label);
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
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        float radius = (ctx.Style.FontSizeBase + style.FramePadding.y * 2) * 0.5f;
        var pos = window.DC.CursorPos;
        var center = new ImVec2(pos.x + radius, pos.y + radius);
        var labelSize = CalcTextSize(label, ctx);
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
        window.DrawList.AddText(textPos + window.Pos, GetColorU32(ImGuiCol_.ImGuiCol_Text), label);
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
        ImGuiID id = GetID(label);
        var style = ctx.Style;
        var labelSize = CalcTextSize(label, ctx);
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

        window.DrawList.AddText(labelPos + window.Pos, GetColorU32(ImGuiCol_.ImGuiCol_Text), label);
        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, new ImRect(bb.Min, new ImVec2(bb.Max.x + labelSize.x + style.ItemSpacing.x, bb.Max.y)));
        ctx.NextItemData.Clear();
        return Math.Abs(v - oldV) > float.Epsilon;
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

    private static float GetEffectiveItemWidth(ImGuiContext ctx, float defaultWidth)
    {
        if (ctx.NextItemData.HasSize && ctx.NextItemData.ItemSize.x > 0)
            return ctx.NextItemData.ItemSize.x;
        if (ctx.ItemWidthStack.Count > 0)
            return ctx.ItemWidthStack.Peek();
        return defaultWidth;
    }

    private static bool ButtonBehavior(ImGuiContext ctx, ImRect bbScreen, ImGuiID id, out bool hovered, out bool held, out bool pressed, bool disabled)
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

        return pressed;
    }

    private static bool CollapsingHeaderInternal(string label, ImGuiID id, ref bool open)
    {
        var ctx = _currentContext ?? throw new InvalidOperationException("No current ImGui context. Call CreateContext first.");
        var window = ctx.CurrentWindow ?? throw new InvalidOperationException("No current window. Call Begin() first.");
        var style = ctx.Style;
        var labelSize = CalcTextSize(label, ctx);
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

        string prefix = open ? "▾ " : "▸ ";
        var textPos = new ImVec2(bbScreen.Min.x + style.FramePadding.x, bbScreen.Min.y + style.FramePadding.y);
        window.DrawList.AddText(textPos, GetColorU32(ImGuiCol_.ImGuiCol_Text), prefix + label);

        window.DC.LastItemId = id;
        ctx.LastItemID = id;
        AdvanceCursorForItem(ctx, window, bb);
        ctx.NextItemData.Clear();
        return pressed;
    }
}
