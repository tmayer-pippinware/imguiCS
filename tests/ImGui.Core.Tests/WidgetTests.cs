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
        var cursor = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(cursor.x + 1, cursor.y + 1);
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
        var firstMin = dl.VtxBuffer[0].pos;
        var firstMax = dl.VtxBuffer[2].pos;
        var secondMin = dl.VtxBuffer[4].pos;
        var style = ImGui.GetStyle();
        Assert.Equal(firstMin.y, secondMin.y);
        Assert.InRange(secondMin.x, firstMax.x + style.ItemSpacing.x - 0.01f, firstMax.x + style.ItemSpacing.x + 0.01f);
    }

    [Fact]
    public void NextWindow_pos_and_size_apply_once()
    {
        ImGui.CreateContext();
        ImGui.SetNextWindowPos(new ImVec2(100, 50));
        ImGui.SetNextWindowSize(new ImVec2(300, 200));
        ImGui.Begin("Layout");
        Assert.Equal(new ImVec2(100, 50), ImGui.GetWindowPos());
        Assert.Equal(new ImVec2(300, 200), ImGui.GetWindowSize());
        ImGui.End();
    }

    [Fact]
    public void Cursor_and_content_region_respect_padding_and_advances()
    {
        ImGui.CreateContext();
        var style = ImGui.GetStyle();
        ImGui.SetNextWindowPos(new ImVec2(10, 20));
        ImGui.SetNextWindowSize(new ImVec2(120, 80));
        ImGui.Begin("Cursor");
        var screenPos = ImGui.GetCursorScreenPos();
        Assert.Equal(new ImVec2(10 + style.WindowPadding.x, 20 + style.WindowPadding.y), screenPos);
        var avail = ImGui.GetContentRegionAvail();
        ImGui.Dummy(new ImVec2(10, 10));
        var availAfter = ImGui.GetContentRegionAvail();
        Assert.Equal(avail.x, availAfter.x);
        Assert.True(availAfter.y < avail.y);
        ImGui.End();
    }

    [Fact]
    public void Separator_adds_draw_command()
    {
        ImGui.CreateContext();
        ImGui.Begin("Lines");
        var dl = ImGui.GetWindowDrawList();
        int idxBefore = dl.IdxBuffer.Count;
        ImGui.Separator();
        Assert.True(dl.IdxBuffer.Count >= idxBefore + 6);
        ImGui.End();
    }

    [Fact]
    public void Hover_and_active_state_track_last_item()
    {
        ImGui.CreateContext();
        ImGui.Begin("Hover");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 1, pos.y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        ImGui.Button("X");
        Assert.True(ImGui.IsItemHovered());
        Assert.True(ImGui.IsItemActive());
        Assert.Equal(ImGui.GetItemID(), ImGui.GetCurrentContext()!.ActiveId);
        ImGui.End();
    }
}
