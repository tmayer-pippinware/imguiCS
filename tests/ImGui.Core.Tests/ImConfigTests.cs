using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class ImConfigTests
{
    [Fact]
    public void Defaults_match_current()
    {
        Assert.Equal(ImConfig.CreateDefault(), ImConfig.Current);
    }

    [Fact]
    public void Enables_stb_truetype_by_default()
    {
        Assert.True(ImConfig.Current.EnableStbTrueType);
    }

    [Fact]
    public void Disables_freetype_by_default()
    {
        Assert.False(ImConfig.Current.EnableFreeType);
    }
}
