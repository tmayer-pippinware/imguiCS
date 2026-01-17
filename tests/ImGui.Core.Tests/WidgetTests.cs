using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class WidgetTests
{
    [Fact]
    public void Button_returns_true_on_click()
    {
        ImGui.CreateContext();
        ImGui.Begin("Test");
        ImGui.AddMousePosEvent(5, 5);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        var pressed = ImGui.Button("Btn");
        Assert.True(pressed);
        ImGui.End();
    }

    [Fact]
    public void PushID_changes_GetID()
    {
        ImGui.CreateContext();
        ImGui.Begin("Test");
        var baseId = ImGui.GetID("Label");
        ImGui.PushID("Scope");
        var scopedId = ImGui.GetID("Label");
        Assert.NotEqual(baseId, scopedId);
        ImGui.PopID();
        ImGui.End();
    }

    [Fact]
    public void Text_records_draw_command()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Test");
        ImGui.Text("Hello");
        ImGui.End();
        ImGui.Render();

        var dd = ImGui.GetDrawData();
        Assert.Equal(2, dd.CmdLists.Count);
        var windowList = dd.CmdLists[0];
        Assert.Single(windowList.TextBuffer);
        Assert.Equal("Hello", windowList.TextBuffer[0].Text);
    }

    [Fact]
    public void SameLine_places_items_horizontally()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Test");
        ImGui.Button("A");
        ImGui.SameLine();
        ImGui.Button("B");
        ImGui.End();
        ImGui.Render();

        var dl = ImGui.GetDrawData().CmdLists[0];
        Assert.True(dl.VtxBuffer.Count >= 8);
        Assert.Equal(0f, dl.VtxBuffer[0].pos.x);
        Assert.Equal(0f, dl.VtxBuffer[0].pos.y);
        Assert.Equal(108f, dl.VtxBuffer[4].pos.x);
        Assert.Equal(0f, dl.VtxBuffer[4].pos.y);
    }
}
