namespace ImGui;

/// <summary>
/// Placeholder FreeType helper mirroring misc/freetype bindings.
/// </summary>
public static class ImGuiFreeType
{
    public static bool BuildFontAtlas(ImFontAtlas atlas)
    {
        // Stub: provide a 1x1 white pixel atlas to unblock backends; replace with SharpFont/SkiaSharp.
        atlas.TexWidth = 1;
        atlas.TexHeight = 1;
        atlas.TexPixelsRGBA32 = new byte[] { 255, 255, 255, 255 };
        return true;
    }
}
