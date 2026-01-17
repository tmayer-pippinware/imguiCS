using System;
using System.Collections.Generic;

namespace ImGui;

public struct ImDrawVert
{
    public ImVec2 pos;
    public ImVec2 uv;
    public uint col;

    public ImDrawVert(ImVec2 pos, ImVec2 uv, uint col)
    {
        this.pos = pos;
        this.uv = uv;
        this.col = col;
    }
}

public struct ImDrawCmd
{
    public int ElemCount;
    public ImVec4 ClipRect;
    public ImTextureID TextureId;
    public uint VtxOffset;
    public uint IdxOffset;
}

public struct ImDrawTextCommand
{
    public ImVec2 Pos;
    public uint Color;
    public string Text;

    public ImDrawTextCommand(ImVec2 pos, uint color, string text)
    {
        Pos = pos;
        Color = color;
        Text = text;
    }
}

public sealed class ImDrawList
{
    public ImDrawListFlags_ Flags { get; private set; }
    public List<ImDrawCmd> CmdBuffer { get; } = new();
    public List<ImDrawIdx> IdxBuffer { get; } = new();
    public List<ImDrawVert> VtxBuffer { get; } = new();
    public List<ImDrawTextCommand> TextBuffer { get; } = new();
    private readonly Stack<ImVec4> _clipStack = new();
    private ImVec4 _clipRect = new(0, 0, float.MaxValue, float.MaxValue);
    private ImTextureID _textureId = default;

    public ImDrawList(ImDrawListFlags_ flags = ImDrawListFlags_.ImDrawListFlags_None)
    {
        Flags = flags;
    }

    public void Clear()
    {
        CmdBuffer.Clear();
        IdxBuffer.Clear();
        VtxBuffer.Clear();
        TextBuffer.Clear();
        _clipStack.Clear();
        _clipRect = new ImVec4(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
        _textureId = default;
    }

    public void PushClipRect(ImVec4 clipRect)
    {
        _clipStack.Push(_clipRect);
        _clipRect = clipRect;
        AddDrawCmdIfNeeded();
    }

    public void PopClipRect()
    {
        if (_clipStack.Count == 0)
            return;
        _clipRect = _clipStack.Pop();
        AddDrawCmdIfNeeded();
    }

    public void PushTextureID(ImTextureID textureId)
    {
        _textureId = textureId;
    }

    private void AddDrawCmdIfNeeded()
    {
        if (CmdBuffer.Count == 0)
        {
            CmdBuffer.Add(new ImDrawCmd
            {
                ClipRect = _clipRect,
                TextureId = _textureId,
                VtxOffset = 0,
                IdxOffset = 0,
                ElemCount = 0
            });
            return;
        }

        var last = CmdBuffer[^1];
        if (last.ClipRect.Equals(_clipRect) && last.TextureId.Equals(_textureId))
            return;

        CmdBuffer.Add(new ImDrawCmd
        {
            ClipRect = _clipRect,
            TextureId = _textureId,
            VtxOffset = (uint)VtxBuffer.Count,
            IdxOffset = (uint)IdxBuffer.Count,
            ElemCount = 0
        });
    }

    public void AddRect(ImVec2 p_min, ImVec2 p_max, uint col, float rounding = 0.0f, ImDrawFlags_ flags = ImDrawFlags_.ImDrawFlags_None, float thickness = 1.0f)
    {
        AddDrawCmdIfNeeded();
        int vtxOffset = VtxBuffer.Count;
        VtxBuffer.Add(new ImDrawVert(new ImVec2(p_min.x, p_min.y), ImVec2.Zero, col));
        VtxBuffer.Add(new ImDrawVert(new ImVec2(p_max.x, p_min.y), ImVec2.Zero, col));
        VtxBuffer.Add(new ImDrawVert(new ImVec2(p_max.x, p_max.y), ImVec2.Zero, col));
        VtxBuffer.Add(new ImDrawVert(new ImVec2(p_min.x, p_max.y), ImVec2.Zero, col));

        IdxBuffer.Add((ushort)(vtxOffset + 0));
        IdxBuffer.Add((ushort)(vtxOffset + 1));
        IdxBuffer.Add((ushort)(vtxOffset + 2));
        IdxBuffer.Add((ushort)(vtxOffset + 2));
        IdxBuffer.Add((ushort)(vtxOffset + 3));
        IdxBuffer.Add((ushort)(vtxOffset + 0));

        var lastIndex = CmdBuffer.Count - 1;
        var cmd = CmdBuffer[lastIndex];
        cmd.ElemCount += 6;
        CmdBuffer[lastIndex] = cmd;
    }

    public void AddLine(ImVec2 p1, ImVec2 p2, uint col, float thickness = 1.0f)
    {
        // Simple axis-aligned rectangle approximation for the line.
        AddDrawCmdIfNeeded();
        var min = new ImVec2(Math.Min(p1.x, p2.x), Math.Min(p1.y, p2.y));
        var max = new ImVec2(Math.Max(p1.x, p2.x), Math.Max(p1.y, p2.y));
        if (Math.Abs(p1.x - p2.x) < 0.0001f)
        {
            min.x -= thickness * 0.5f;
            max.x += thickness * 0.5f;
        }
        else if (Math.Abs(p1.y - p2.y) < 0.0001f)
        {
            min.y -= thickness * 0.5f;
            max.y += thickness * 0.5f;
        }
        AddRectFilled(min, max, col);
    }

    public void AddRectFilled(ImVec2 p_min, ImVec2 p_max, uint col, float rounding = 0.0f, ImDrawFlags_ flags = ImDrawFlags_.ImDrawFlags_None)
    {
        AddDrawCmdIfNeeded();
        int vtxOffset = VtxBuffer.Count;
        VtxBuffer.Add(new ImDrawVert(new ImVec2(p_min.x, p_min.y), ImVec2.Zero, col));
        VtxBuffer.Add(new ImDrawVert(new ImVec2(p_max.x, p_min.y), ImVec2.Zero, col));
        VtxBuffer.Add(new ImDrawVert(new ImVec2(p_max.x, p_max.y), ImVec2.Zero, col));
        VtxBuffer.Add(new ImDrawVert(new ImVec2(p_min.x, p_max.y), ImVec2.Zero, col));

        IdxBuffer.Add((ushort)(vtxOffset + 0));
        IdxBuffer.Add((ushort)(vtxOffset + 1));
        IdxBuffer.Add((ushort)(vtxOffset + 2));
        IdxBuffer.Add((ushort)(vtxOffset + 2));
        IdxBuffer.Add((ushort)(vtxOffset + 3));
        IdxBuffer.Add((ushort)(vtxOffset + 0));

        var lastIndex = CmdBuffer.Count - 1;
        var cmd = CmdBuffer[lastIndex];
        cmd.ElemCount += 6;
        CmdBuffer[lastIndex] = cmd;
    }

    public void AddText(ImVec2 pos, uint col, string text)
    {
        AddDrawCmdIfNeeded();
        TextBuffer.Add(new ImDrawTextCommand(pos, col, text));
    }
}
