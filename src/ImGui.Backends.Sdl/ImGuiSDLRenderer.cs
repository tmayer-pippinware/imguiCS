using System;
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
                for (int i = idxOffset; i + 2 < end && i + 2 < idx.Count; i += 3)
                {
                    var v0 = verts[idx[i]];
                    var v1 = verts[idx[i + 1]];
                    var v2 = verts[idx[i + 2]];

                    if (_options.FillTriangles)
                        DrawFilledTriangle(v0, v1, v2);

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
                DrawTextFallback(text);
            }
        }

        SDL.SDL_RenderSetClipRect(_renderer, IntPtr.Zero);
        SDL.SDL_RenderPresent(_renderer);
    }

    public void Shutdown()
    {
        if (_ownsRenderer && _renderer != IntPtr.Zero)
            SDL.SDL_DestroyRenderer(_renderer);
        _renderer = IntPtr.Zero;
        _nativeEnabled = false;
    }

    private static byte ColorChannel(uint packedColor, int shift) => (byte)((packedColor >> shift) & 0xFF);

    private void DrawTextFallback(in ImDrawTextCommand cmd)
    {
        // Approximate text as a filled rectangle sized by character count.
        int width = Math.Max(1, (int)(cmd.Text.Length * 7));
        int height = 14;
        byte r = ColorChannel(cmd.Color, 0);
        byte g = ColorChannel(cmd.Color, 8);
        byte b = ColorChannel(cmd.Color, 16);
        byte a = ColorChannel(cmd.Color, 24);
        SDL.SDL_SetRenderDrawColor(_renderer, r, g, b, a);
        SDL.SDL_Rect rect = new()
        {
            x = (int)cmd.Pos.x,
            y = (int)cmd.Pos.y,
            w = width,
            h = height
        };
        SDL.SDL_RenderFillRect(_renderer, ref rect);
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
