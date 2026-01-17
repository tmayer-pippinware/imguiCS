using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class StorageTests
{
    [Fact]
    public void Storage_set_get_values()
    {
        var storage = new ImGuiStorage();
        storage.SetInt(1, 42);
        storage.SetBool(2, true);
        storage.SetFloat(3, 3.14f);
        storage.SetVoidPtr(4, (nint)1234);

        Assert.Equal(42, storage.GetInt(1));
        Assert.True(storage.GetBool(2));
        Assert.Equal(3.14f, storage.GetFloat(3));
        Assert.Equal((nint)1234, storage.GetVoidPtr(4));
        Assert.Equal(7, storage.GetInt(99, 7));
    }
}
