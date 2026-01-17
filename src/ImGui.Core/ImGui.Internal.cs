using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ImGui;

internal sealed class ImGuiWindow
{
    public string Name { get; }
    public ImDrawList DrawList { get; } = new ImDrawList();
    public ImGuiWindowTempData DC { get; } = new ImGuiWindowTempData();
    public ImGuiStorage StateStorage { get; } = new ImGuiStorage();
    public ImVec2 Pos;
    public ImVec2 Size;
    public ImGuiID ID;

    public ImGuiWindow(string name)
    {
        Name = name;
        DC.CursorPos = ImVec2.Zero;
        DC.CursorStartPos = ImVec2.Zero;
        DC.LastItemRect = new ImRect(ImVec2.Zero, ImVec2.Zero);
        DC.TreeDepth = 0;
    }
}

internal sealed class ImGuiWindowTempData
{
    public ImVec2 CursorPos;
    public ImVec2 CursorStartPos;
    public ImVec2 CursorMax;
    public float IndentX;
    public ImRect ClipRect;
    public int TreeDepth;
    public ImGuiID LastItemId;
    public ImRect LastItemRect;
    public ImGuiStorage StateStorage { get; } = new ImGuiStorage();
    public Stack<ImGuiGroupData> GroupStack { get; } = new();
}

internal sealed class ImGuiTable
{
    public ImGuiID ID;
    public int ColumnsCount;
    public int CurrentColumn;
    public int CurrentRow;
    public ImVec2 OuterSize;
    public ImVec2 WorkPos;
    public float RowHeight;
    public List<string?> ColumnNames { get; } = new();

    public ImGuiTable(ImGuiID id, int columns, ImVec2 outerSize, ImVec2 workPos, float rowHeight)
    {
        ID = id;
        ColumnsCount = columns;
        CurrentColumn = -1;
        CurrentRow = -1;
        OuterSize = outerSize;
        WorkPos = workPos;
        RowHeight = rowHeight;
        ColumnNames.Capacity = columns;
    }
}

internal struct ImGuiNextWindowData
{
    public bool HasPos;
    public bool HasSize;
    public ImVec2 Pos;
    public ImVec2 Size;

    public void Clear()
    {
        HasPos = false;
        HasSize = false;
        Pos = ImVec2.Zero;
        Size = ImVec2.Zero;
    }
}

internal struct ImGuiNextItemData
{
    public bool HasSize;
    public ImVec2 ItemSize;

    public void Clear()
    {
        HasSize = false;
        ItemSize = ImVec2.Zero;
    }
}

internal struct ImGuiGroupData
{
    public ImVec2 BackupCursorPos;
    public ImVec2 BackupCursorStartPos;
    public float BackupIndentX;
    public ImVec2 GroupMin;
    public ImVec2 GroupMax;
}

internal static class ImHash
{
    public static ImGuiID Hash(ReadOnlySpan<char> data, ImGuiID seed = 0)
    {
        // FNV-1a 32-bit
        unchecked
        {
            uint hash = seed != 0 ? seed : 2166136261;
            foreach (char c in data)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash;
        }
    }
}
