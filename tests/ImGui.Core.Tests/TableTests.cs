using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class TableTests
{
    [Fact]
    public void TableNextColumn_advances_cursor()
    {
        ImGui.CreateContext();
        ImGui.SetNextWindowSize(new ImVec2(200, 200));
        ImGui.NewFrame();
        ImGui.Begin("T");
        Assert.True(ImGui.BeginTable("tbl", 2));
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var c0 = ImGui.GetCursorPos();
        ImGui.TableNextColumn();
        var c1 = ImGui.GetCursorPos();
        Assert.True(c1.x > c0.x);
        ImGui.EndTable();
        ImGui.End();
    }

    [Fact]
    public void TableNextRow_moves_downward()
    {
        ImGui.CreateContext();
        ImGui.SetNextWindowSize(new ImVec2(200, 200));
        ImGui.NewFrame();
        ImGui.Begin("T");
        ImGui.BeginTable("tbl", 1);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var firstRow = ImGui.GetCursorPos();
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var secondRow = ImGui.GetCursorPos();
        Assert.True(secondRow.y > firstRow.y);
        ImGui.EndTable();
        ImGui.End();
    }

    [Fact]
    public void TableGetIndices_reflect_current_position()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("T");
        ImGui.BeginTable("tbl", 3);
        ImGui.TableSetupColumn("A");
        ImGui.TableSetupColumn("B");
        ImGui.TableSetupColumn("C");
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        Assert.Equal(0, ImGui.TableGetColumnIndex());
        ImGui.TableNextColumn();
        Assert.Equal(1, ImGui.TableGetColumnIndex());
        Assert.Equal(0, ImGui.TableGetRowIndex());
        ImGui.EndTable();
        ImGui.End();
    }

    [Fact]
    public void TableHeadersRow_writes_headers()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("T");
        ImGui.BeginTable("tbl", 2);
        ImGui.TableSetupColumn("Col1");
        ImGui.TableSetupColumn("Col2");
        ImGui.TableHeadersRow();
        ImGui.EndTable();
        ImGui.End();
        ImGui.Render();
        var dd = ImGui.GetDrawData();
        Assert.Equal(2, dd.CmdLists.Count);
        var drawList = dd.CmdLists[0];
        Assert.Contains(drawList.TextBuffer, t => t.Text.Contains("Col1"));
        Assert.Contains(drawList.TextBuffer, t => t.Text.Contains("Col2"));
    }

    [Fact]
    public void Table_sort_specs_toggle_on_header_click()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("T");
        ImGui.BeginTable("tbl", 2);
        ImGui.TableSetupColumn("Col1");
        ImGui.TableSetupColumn("Col2");
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        ImGui.TableHeadersRow();
        var specs = ImGui.TableGetSortSpecs();
        Assert.True(specs.SpecsDirty);
        Assert.Equal(2, specs.SpecsCount);
        ImGui.EndTable();
        ImGui.End();
    }
}
