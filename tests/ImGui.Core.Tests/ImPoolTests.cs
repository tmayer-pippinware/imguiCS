using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class ImPoolTests
{
    [Fact]
    public void Add_and_remove_reuses_slots()
    {
        var pool = new ImPool<int>();
        var a = pool.GetOrAdd();
        pool.GetOrAdd();
        pool.Remove(a);
        var b = pool.GetOrAdd();
        Assert.Equal(a, b);
    }
}
