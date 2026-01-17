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
    public ImVec2 Pos;
    public ImVec2 Size;
    public ImGuiID ID;

    public ImGuiWindow(string name)
    {
        Name = name;
        DC.CursorPos = ImVec2.Zero;
        DC.CursorStartPos = ImVec2.Zero;
        DC.LastItemRect = new ImRect(ImVec2.Zero, ImVec2.Zero);
    }
}

internal sealed class ImGuiWindowTempData
{
    public ImVec2 CursorPos;
    public ImVec2 CursorStartPos;
    public ImGuiID LastItemId;
    public ImRect LastItemRect;
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
