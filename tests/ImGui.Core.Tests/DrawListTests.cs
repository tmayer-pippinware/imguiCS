using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class DrawListTests
{
    [Fact]
    public void AddRect_builds_vertices_and_indices()
    {
        ImGui.CreateContext();
        var dl = ImGui.GetForegroundDrawList();
        dl.Clear();

        dl.AddRect(new ImVec2(0, 0), new ImVec2(10, 10), 0xFFFFFFFF);

        Assert.Single(dl.CmdBuffer);
        Assert.Equal(4, dl.VtxBuffer.Count);
        Assert.Equal(6, dl.IdxBuffer.Count);
        Assert.Equal(6, dl.CmdBuffer[0].ElemCount);
    }

    [Fact]
    public void Render_populates_draw_data()
    {
        ImGui.CreateContext();
        var dl = ImGui.GetForegroundDrawList();
        dl.Clear();
        dl.AddRect(new ImVec2(0, 0), new ImVec2(10, 10), 0xFFFFFFFF);

        ImGui.Render();
        var dd = ImGui.GetDrawData();
        Assert.True(dd.Valid);
        Assert.Single(dd.CmdLists);
        Assert.Equal(4, dd.TotalVtxCount);
        Assert.Equal(6, dd.TotalIdxCount);
    }

    [Fact]
    public void AddLine_generates_geometry()
    {
        ImGui.CreateContext();
        var dl = ImGui.GetForegroundDrawList();
        dl.Clear();

        dl.AddLine(new ImVec2(0, 0), new ImVec2(10, 0), 0xFFFFFFFF, 2.0f);

        Assert.True(dl.VtxBuffer.Count >= 4);
        Assert.True(dl.IdxBuffer.Count >= 6);
    }
}
