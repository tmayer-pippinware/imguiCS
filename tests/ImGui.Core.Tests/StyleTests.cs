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

    [Fact]
    public void StyleColorsLight_sets_expected_palette()
    {
        var style = new ImGuiStyle();
        ImGuiStyle.StyleColorsLight(ref style);
        Assert.Equal(new ImVec4(0.94f, 0.94f, 0.94f, 1.00f), style.Colors[(int)ImGuiCol_.ImGuiCol_WindowBg]);
        Assert.Equal(new ImVec4(0.26f, 0.59f, 0.98f, 0.40f), style.Colors[(int)ImGuiCol_.ImGuiCol_Button]);
    }

    [Fact]
    public void Push_and_pop_style_color_restores_previous_value()
    {
        ImGui.CreateContext();
        var original = ImGui.GetStyle().Colors[(int)ImGuiCol_.ImGuiCol_Text];
        ImGui.PushStyleColor(ImGuiCol_.ImGuiCol_Text, new ImVec4(1, 0, 0, 1));
        Assert.Equal(new ImVec4(1, 0, 0, 1), ImGui.GetStyle().Colors[(int)ImGuiCol_.ImGuiCol_Text]);
        ImGui.PopStyleColor();
        Assert.Equal(original, ImGui.GetStyle().Colors[(int)ImGuiCol_.ImGuiCol_Text]);
    }
}
