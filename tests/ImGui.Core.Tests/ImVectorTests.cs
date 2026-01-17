using System;
using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class ImVectorTests
{
    [Fact]
    public void Push_and_pop_roundtrip()
    {
        using var vec = new ImVector<int>();
        vec.PushBack(1);
        vec.PushBack(2);
        Assert.Equal(2, vec.Size);
        Assert.Equal(1, vec[0]);
        Assert.Equal(2, vec[1]);

        var popped = vec.PopBack();
        Assert.Equal(2, popped);
        Assert.Equal(1, vec.Size);
    }

    [Fact]
    public void Resize_clears_new_slots()
    {
        using var vec = new ImVector<int>();
        vec.Resize(3);
        vec[0] = 42;
        vec[1] = 7;
        Assert.Equal(0, vec[2]);
    }
}
