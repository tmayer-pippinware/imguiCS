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

public sealed class ImDrawList
{
    public ImDrawListFlags_ Flags { get; private set; }
    public List<ImDrawCmd> CmdBuffer { get; } = new();
    public List<ImDrawIdx> IdxBuffer { get; } = new();
    public List<ImDrawVert> VtxBuffer { get; } = new();
    private ImVec4 _clipRect = new(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
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
        _clipRect = new ImVec4(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);
        _textureId = default;
    }

    public void PushClipRect(ImVec4 clipRect)
    {
        _clipRect = clipRect;
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
        AddRectFilled(p_min, p_max, col, rounding, flags);
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
}
