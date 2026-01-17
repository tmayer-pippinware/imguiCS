using System;
using System.Runtime.InteropServices;
using ImGui;
using SDL2;

namespace ImGui.Backends.Sdl;

/// <summary>
/// Renderer backend for SDL's 2D renderer. Defaults to a no-op unless native SDL is enabled.
/// </summary>
public sealed class ImGuiSDLRenderer
{
    private IntPtr _renderer;
    private IntPtr _window;
    private bool _ownsRenderer;
    private bool _nativeEnabled;
    private ImGuiSDLRendererOptions _options = new();
    private IntPtr _fontTexture;
    private readonly Dictionary<IntPtr, TextureInfo> _textures = new();

    public bool Init(IntPtr window, ImGuiSDLRendererOptions? options = null)
    {
        _window = window;
        _options = options ?? new ImGuiSDLRendererOptions();
        _nativeEnabled = _options.UseSDLRenderer;

        ref var io = ref ImGui.GetIO();
        io.BackendRendererName = "imgui_impl_sdlrenderer_cs";

        if (_nativeEnabled && _window != IntPtr.Zero)
        {
            try
            {
                _renderer = _options.ExistingRenderer != IntPtr.Zero
                    ? _options.ExistingRenderer
                    : SDL.SDL_CreateRenderer(_window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
                _ownsRenderer = _renderer != IntPtr.Zero && _options.ExistingRenderer == IntPtr.Zero;

                // Upload font atlas (or fallback) as an SDL texture.
                if (_renderer != IntPtr.Zero)
                {
                    UploadFontTexture(ref io);
                    io.Fonts ??= new ImFontAtlas();
                    io.Fonts.SetTexID((nint)_fontTexture);
                }
            }
            catch (DllNotFoundException)
            {
                _nativeEnabled = false;
                _renderer = IntPtr.Zero;
            }
        }

        return true;
    }

    public void NewFrame()
    {
    }

    public void RenderDrawData(ImDrawData drawData)
    {
        if (!_nativeEnabled || _renderer == IntPtr.Zero)
            return;

        SDL.SDL_SetRenderDrawBlendMode(_renderer, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
        SDL.SDL_SetRenderDrawColor(_renderer, _options.ClearColor.r, _options.ClearColor.g, _options.ClearColor.b, _options.ClearColor.a);
        SDL.SDL_RenderClear(_renderer);

        for (int n = 0; n < drawData.CmdLists.Count; n++)
        {
            var list = drawData.CmdLists[n];
            var verts = list.VtxBuffer;
            var idx = list.IdxBuffer;
            var cmdBuffer = list.CmdBuffer;
            int idxOffset = 0;
            for (int cmd_i = 0; cmd_i < cmdBuffer.Count; cmd_i++)
            {
                var cmd = cmdBuffer[cmd_i];
                var clip = cmd.ClipRect;
                float clipX1 = MathF.Max(clip.x - drawData.DisplayPos.x, 0f);
                float clipY1 = MathF.Max(clip.y - drawData.DisplayPos.y, 0f);
                float clipX2 = MathF.Min(clip.z - drawData.DisplayPos.x, drawData.DisplaySize.x);
                float clipY2 = MathF.Min(clip.w - drawData.DisplayPos.y, drawData.DisplaySize.y);
                if (clipX2 <= clipX1 || clipY2 <= clipY1)
                {
                    idxOffset += cmd.ElemCount;
                    continue;
                }

                SDL.SDL_Rect clipRect = new()
                {
                    x = (int)MathF.Floor(clipX1),
                    y = (int)MathF.Floor(clipY1),
                    w = (int)MathF.Ceiling(clipX2 - clipX1),
                    h = (int)MathF.Ceiling(clipY2 - clipY1)
                };

                SDL.SDL_RenderSetClipRect(_renderer, ref clipRect);

                int end = idxOffset + cmd.ElemCount;
                int baseIndex = idxOffset + (int)cmd.IdxOffset;
                int baseVertex = (int)cmd.VtxOffset;

                IntPtr texture = cmd.TextureId != IntPtr.Zero ? (IntPtr)cmd.TextureId : _fontTexture;
                for (int i = baseIndex; i + 2 < end && i + 2 < idx.Count; i += 3)
                {
                    int i0 = baseVertex + idx[i];
                    int i1 = baseVertex + idx[i + 1];
                    int i2 = baseVertex + idx[i + 2];
                    if ((uint)i2 >= (uint)verts.Count)
                        continue;

                    var v0 = OffsetVertex(verts[i0], drawData.DisplayPos);
                    var v1 = OffsetVertex(verts[i1], drawData.DisplayPos);
                    var v2 = OffsetVertex(verts[i2], drawData.DisplayPos);

                    if (_options.FillTriangles)
                        DrawTexturedTriangle(v0, v1, v2, texture);

                    if (_options.DrawWireframe)
                    {
                        SDL.SDL_SetRenderDrawColor(_renderer, ColorChannel(v0.col, 0), ColorChannel(v0.col, 8), ColorChannel(v0.col, 16), ColorChannel(v0.col, 24));
                        SDL.SDL_RenderDrawLine(_renderer, (int)v0.pos.x, (int)v0.pos.y, (int)v1.pos.x, (int)v1.pos.y);
                        SDL.SDL_RenderDrawLine(_renderer, (int)v1.pos.x, (int)v1.pos.y, (int)v2.pos.x, (int)v2.pos.y);
                        SDL.SDL_RenderDrawLine(_renderer, (int)v2.pos.x, (int)v2.pos.y, (int)v0.pos.x, (int)v0.pos.y);
                    }
                }

                idxOffset = end;
            }

            // Fallback text visualization: draw a tinted rect for each text command (no glyph atlas yet).
            for (int t = 0; t < list.TextBuffer.Count; t++)
            {
                var text = list.TextBuffer[t];
                DrawTextFallback(text, drawData.DisplayPos);
            }
        }

        SDL.SDL_RenderSetClipRect(_renderer, IntPtr.Zero);
        SDL.SDL_RenderPresent(_renderer);
    }

    public void Shutdown()
    {
        if (_ownsRenderer && _renderer != IntPtr.Zero)
            SDL.SDL_DestroyRenderer(_renderer);
        if (_fontTexture != IntPtr.Zero && _ownsRenderer)
            SDL.SDL_DestroyTexture(_fontTexture);
        _renderer = IntPtr.Zero;
        _fontTexture = IntPtr.Zero;
        _nativeEnabled = false;
    }

    private static byte ColorChannel(uint packedColor, int shift) => (byte)((packedColor >> shift) & 0xFF);

    private static ImDrawVert OffsetVertex(in ImDrawVert v, ImVec2 displayPos)
    {
        return new ImDrawVert(new ImVec2(v.pos.x - displayPos.x, v.pos.y - displayPos.y), v.uv, v.col);
    }

    private void DrawTextFallback(in ImDrawTextCommand cmd, ImVec2 displayPos)
    {
        const int cell = 8;
        int cursorX = (int)(cmd.Pos.x - displayPos.x);
        int cursorY = (int)(cmd.Pos.y - displayPos.y);
        IntPtr texture = _fontTexture;

        for (int i = 0; i < cmd.Text.Length; i++)
        {
            char c = cmd.Text[i];
            if (c < 32 || c > 126)
            {
                cursorX += cell;
                continue;
            }
            int glyphIndex = c - 32;
            const int cols = 16;
            int gx = glyphIndex % cols;
            int gy = glyphIndex / cols;
            float u0 = (gx * cell) / (float)_textures[texture].Width;
            float vv0 = (gy * cell) / (float)_textures[texture].Height;
            float u1 = ((gx + 1) * cell) / (float)_textures[texture].Width;
            float vv1 = ((gy + 1) * cell) / (float)_textures[texture].Height;

            ImDrawVert tv0 = new(new ImVec2(cursorX, cursorY), new ImVec2(u0, vv0), cmd.Color);
            ImDrawVert tv1 = new(new ImVec2(cursorX + cell, cursorY), new ImVec2(u1, vv0), cmd.Color);
            ImDrawVert tv2 = new(new ImVec2(cursorX + cell, cursorY + cell), new ImVec2(u1, vv1), cmd.Color);
            ImDrawVert tv3 = new(new ImVec2(cursorX, cursorY + cell), new ImVec2(u0, vv1), cmd.Color);

            DrawTexturedTriangle(tv0, tv1, tv2, texture);
            DrawTexturedTriangle(tv0, tv2, tv3, texture);
            cursorX += cell;
        }
    }

    private void DrawFilledTriangle(in ImDrawVert v0, in ImDrawVert v1, in ImDrawVert v2)
    {
        byte r = ColorChannel(v0.col, 0);
        byte g = ColorChannel(v0.col, 8);
        byte b = ColorChannel(v0.col, 16);
        byte a = ColorChannel(v0.col, 24);
        SDL.SDL_SetRenderDrawColor(_renderer, r, g, b, a);

        float minX = MathF.Min(v0.pos.x, MathF.Min(v1.pos.x, v2.pos.x));
        float minY = MathF.Min(v0.pos.y, MathF.Min(v1.pos.y, v2.pos.y));
        float maxX = MathF.Max(v0.pos.x, MathF.Max(v1.pos.x, v2.pos.x));
        float maxY = MathF.Max(v0.pos.y, MathF.Max(v1.pos.y, v2.pos.y));

        int minXi = (int)MathF.Floor(minX);
        int minYi = (int)MathF.Floor(minY);
        int maxXi = (int)MathF.Ceiling(maxX);
        int maxYi = (int)MathF.Ceiling(maxY);

        for (int y = minYi; y <= maxYi; y++)
        {
            for (int x = minXi; x <= maxXi; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                if (PointInTriangle(px, py, v0.pos, v1.pos, v2.pos))
                    SDL.SDL_RenderDrawPoint(_renderer, x, y);
            }
        }
    }

    private void DrawTexturedTriangle(in ImDrawVert v0, in ImDrawVert v1, in ImDrawVert v2, IntPtr textureId)
    {
        if (!_textures.TryGetValue(textureId, out var tex))
        {
            DrawFilledTriangle(v0, v1, v2);
            return;
        }

        float minX = MathF.Min(v0.pos.x, MathF.Min(v1.pos.x, v2.pos.x));
        float minY = MathF.Min(v0.pos.y, MathF.Min(v1.pos.y, v2.pos.y));
        float maxX = MathF.Max(v0.pos.x, MathF.Max(v1.pos.x, v2.pos.x));
        float maxY = MathF.Max(v0.pos.y, MathF.Max(v1.pos.y, v2.pos.y));

        int minXi = (int)MathF.Floor(minX);
        int minYi = (int)MathF.Floor(minY);
        int maxXi = (int)MathF.Ceiling(maxX);
        int maxYi = (int)MathF.Ceiling(maxY);

        for (int y = minYi; y <= maxYi; y++)
        {
            for (int x = minXi; x <= maxXi; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float w0, w1, w2;
                Barycentric(px, py, v0.pos, v1.pos, v2.pos, out w0, out w1, out w2);
                if (w0 < 0 || w1 < 0 || w2 < 0)
                    continue;

                float u = w0 * v0.uv.x + w1 * v1.uv.x + w2 * v2.uv.x;
                float v = w0 * v0.uv.y + w1 * v1.uv.y + w2 * v2.uv.y;
                int tx = Clamp((int)(u * tex.Width), 0, tex.Width - 1);
                int ty = Clamp((int)(v * tex.Height), 0, tex.Height - 1);
                var sample = tex.GetPixel(tx, ty);

                byte r = (byte)(sample.r * (w0 * ColorChannel(v0.col, 0) + w1 * ColorChannel(v1.col, 0) + w2 * ColorChannel(v2.col, 0)) / 255f);
                byte g = (byte)(sample.g * (w0 * ColorChannel(v0.col, 8) + w1 * ColorChannel(v1.col, 8) + w2 * ColorChannel(v2.col, 8)) / 255f);
                byte b = (byte)(sample.b * (w0 * ColorChannel(v0.col, 16) + w1 * ColorChannel(v1.col, 16) + w2 * ColorChannel(v2.col, 16)) / 255f);
                byte a = (byte)(sample.a * (w0 * ColorChannel(v0.col, 24) + w1 * ColorChannel(v1.col, 24) + w2 * ColorChannel(v2.col, 24)) / 255f);

                SDL.SDL_SetRenderDrawColor(_renderer, r, g, b, a);
                SDL.SDL_RenderDrawPoint(_renderer, x, y);
            }
        }
    }

    private void UploadFontTexture(ref ImGuiIO io)
    {
        int width = 1;
        int height = 1;
        byte[] pixels = new byte[] { 255, 255, 255, 255 };

        if (io.Fonts != null && io.Fonts.TexPixelsRGBA32 != null && io.Fonts.TexPixelsRGBA32.Length >= 4)
        {
            pixels = io.Fonts.TexPixelsRGBA32;
            width = io.Fonts.TexWidth > 0 ? io.Fonts.TexWidth : 1;
            height = io.Fonts.TexHeight > 0 ? io.Fonts.TexHeight : 1;
        }

        _fontTexture = SDL.SDL_CreateTexture(_renderer, SDL.SDL_PIXELFORMAT_ABGR8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STATIC, width, height);
        SDL.SDL_Rect rect = new() { x = 0, y = 0, w = width, h = height };
        unsafe
        {
            fixed (byte* ptr = pixels)
            {
                SDL.SDL_UpdateTexture(_fontTexture, ref rect, new IntPtr(ptr), width * 4);
            }
        }

        _textures[_fontTexture] = new TextureInfo(width, height, pixels);
        if (io.Fonts != null)
        {
            io.Fonts.TexWidth = width;
            io.Fonts.TexHeight = height;
        }
    }

    private static bool PointInTriangle(float px, float py, ImVec2 a, ImVec2 b, ImVec2 c)
    {
        float d1 = Cross(px, py, a, b);
        float d2 = Cross(px, py, b, c);
        float d3 = Cross(px, py, c, a);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    private static float Cross(float px, float py, ImVec2 v1, ImVec2 v2)
    {
        return (px - v2.x) * (v1.y - v2.y) - (v1.x - v2.x) * (py - v2.y);
    }

    private static void Barycentric(float px, float py, ImVec2 a, ImVec2 b, ImVec2 c, out float w0, out float w1, out float w2)
    {
        float denom = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
        if (MathF.Abs(denom) < 1e-6f)
        {
            w0 = w1 = w2 = -1f;
            return;
        }
        w0 = ((b.y - c.y) * (px - c.x) + (c.x - b.x) * (py - c.y)) / denom;
        w1 = ((c.y - a.y) * (px - c.x) + (a.x - c.x) * (py - c.y)) / denom;
        w2 = 1f - w0 - w1;
    }

    private static int Clamp(int v, int min, int max)
    {
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }

    private readonly struct TextureInfo
    {
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Data; // RGBA

        public TextureInfo(int width, int height, byte[] data)
        {
            Width = width;
            Height = height;
            Data = data;
        }

        public (byte r, byte g, byte b, byte a) GetPixel(int x, int y)
        {
            int idx = (y * Width + x) * 4;
            return (Data[idx + 0], Data[idx + 1], Data[idx + 2], Data[idx + 3]);
        }

        public static TextureInfo CreateSolid(int w, int h, byte r, byte g, byte b, byte a)
        {
            var data = new byte[w * h * 4];
            for (int i = 0; i < w * h; i++)
            {
                int idx = i * 4;
                data[idx + 0] = r;
                data[idx + 1] = g;
                data[idx + 2] = b;
                data[idx + 3] = a;
            }
            return new TextureInfo(w, h, data);
        }
    }
}

public sealed class ImGuiSDLRendererOptions
{
    /// <summary>
    /// When true, create or use a native SDL_Renderer to visualize ImGui draw lists.
    /// </summary>
    public bool UseSDLRenderer { get; init; }

    /// <summary>
    /// Optional externally-created renderer to use instead of creating a new one.
    /// </summary>
    public IntPtr ExistingRenderer { get; init; }

    /// <summary>
    /// Clear color used before drawing ImGui geometry.
    /// </summary>
    public SDL.SDL_Color ClearColor { get; init; } = new SDL.SDL_Color { r = 32, g = 32, b = 36, a = 255 };

    /// <summary>
    /// When true, rasterize filled triangles from draw lists into the SDL renderer (slow but functional fallback).
    /// </summary>
    public bool FillTriangles { get; init; } = true;

    /// <summary>
    /// When true, also draw triangle edges (wireframe) for debugging.
    /// </summary>
    public bool DrawWireframe { get; init; }
}
