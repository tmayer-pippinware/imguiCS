using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class ImGuiContextTests
{
    [Fact]
    public void CreateContext_sets_default_io_and_style()
    {
        var ctx = ImGui.CreateContext();
        Assert.NotNull(ctx);

        ref var io = ref ImGui.GetIO();
        Assert.Equal(-1.0f, io.DisplaySize.x);
        Assert.Equal(-1.0f, io.DisplaySize.y);
        Assert.Equal(1.0f / 60.0f, io.DeltaTime, 5);
        Assert.True(io.ConfigNavCaptureKeyboard);
        Assert.True(io.ConfigDebugHighlightIdConflicts);

        ref var style = ref ImGui.GetStyle();
        Assert.Equal(1.0f, style.Alpha);
        Assert.Equal(8.0f, style.WindowPadding.x);
        Assert.Equal(8.0f, style.WindowPadding.y);
        Assert.Equal(14.0f, style.ScrollbarSize);
        Assert.Equal((int)ImGuiCol_.ImGuiCol_COUNT, style.Colors.Length);
        Assert.Equal(new ImVec4(1f, 1f, 1f, 1f), style.Colors[(int)ImGuiCol_.ImGuiCol_Text]);
        Assert.Equal(new ImVec4(0.06f, 0.06f, 0.06f, 0.94f), style.Colors[(int)ImGuiCol_.ImGuiCol_WindowBg]);
    }

    [Fact]
    public void NewFrame_increments_frame_count()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.NewFrame();
        var ctx = ImGui.GetCurrentContext();
        Assert.NotNull(ctx);
        Assert.Equal(2, ctx!.FrameCount);
    }
}
