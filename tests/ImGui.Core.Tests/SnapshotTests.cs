using System;
using System.IO;
using System.Text.Json;
using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class SnapshotTests
{
    private static string Serialize(object obj) => JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });

    private static string NormalizeJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
    }

    [Fact]
    public void Style_snapshot_matches_baseline()
    {
        ImGui.CreateContext();
        ref var style = ref ImGui.GetStyle();
        var snap = new
        {
            Alpha = style.Alpha,
            DisabledAlpha = style.DisabledAlpha,
            WindowPadding = new[] { style.WindowPadding.x, style.WindowPadding.y },
            FramePadding = new[] { style.FramePadding.x, style.FramePadding.y },
            ItemSpacing = new[] { style.ItemSpacing.x, style.ItemSpacing.y },
            ItemInnerSpacing = new[] { style.ItemInnerSpacing.x, style.ItemInnerSpacing.y },
            CellPadding = new[] { style.CellPadding.x, style.CellPadding.y },
            IndentSpacing = style.IndentSpacing,
            ScrollbarSize = style.ScrollbarSize,
            ScrollbarRounding = style.ScrollbarRounding,
            GrabMinSize = style.GrabMinSize,
            GrabRounding = style.GrabRounding,
            WindowMinSize = new[] { style.WindowMinSize.x, style.WindowMinSize.y },
            FontSizeBase = style.FontSizeBase
        };
        var current = NormalizeJson(Serialize(snap));
        var baseline = NormalizeJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Snapshots", "style-default.json")));
        Assert.Equal(baseline, current);
    }

    [Fact]
    public void IO_snapshot_matches_baseline()
    {
        ImGui.CreateContext();
        ref var io = ref ImGui.GetIO();
        var snap = new
        {
            DisplaySize = new[] { io.DisplaySize.x, io.DisplaySize.y },
            DeltaTime = Math.Round(io.DeltaTime, 9),
            FontGlobalScale = io.FontGlobalScale,
            ConfigFlags = (int)io.ConfigFlags,
            BackendFlags = (int)io.BackendFlags
        };
        var current = NormalizeJson(Serialize(snap));
        var baseline = NormalizeJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Snapshots", "io-default.json")));
        Assert.Equal(baseline, current);
    }
}
