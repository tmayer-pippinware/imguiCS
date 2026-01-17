using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class StyleTests
{
    [Fact]
    public void ScaleAllSizes_scales_padding_and_rounding()
    {
        var style = new ImGuiStyle();
        style.ScaleAllSizes(2.0f);
        Assert.Equal(new ImVec2(16, 16), style.WindowPadding);
        Assert.Equal(2.0f, style.WindowBorderSize);
        Assert.Equal(28.0f, style.ScrollbarSize);
        Assert.Equal(10.0f, style.TabRounding);
    }
}
