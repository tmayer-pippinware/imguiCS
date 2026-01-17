using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class InputEventTests
{
    [Fact]
    public void AddInputCharacter_enqueues_text_event()
    {
        ImGui.CreateContext();
        ref var io = ref ImGui.GetIO();

        ImGui.AddInputCharacter('A');
        ImGui.AddInputCharacter('B');

        var ctx = ImGui.GetCurrentContext()!;
        Assert.Equal(2, ctx.InputEventsQueue.Count);
        Assert.Equal((uint)'A', ctx.InputEventsQueue[0].Text.Char);
        Assert.Equal(1u, ctx.InputEventsQueue[0].EventId);
        Assert.Equal((uint)'B', ctx.InputEventsQueue[1].Text.Char);
        Assert.Equal(2u, ctx.InputEventsQueue[1].EventId);
    }

    [Fact]
    public void AddInputCharacterUTF16_handles_surrogates()
    {
        ImGui.CreateContext();
        ref var io = ref ImGui.GetIO();

        // high surrogate followed by low surrogate (unicode smiley U+1F60A)
        io.AddInputCharacterUTF16(0xD83D);
        io.AddInputCharacterUTF16(0xDE0A);

        var ctx = ImGui.GetCurrentContext()!;
        Assert.Single(ctx.InputEventsQueue);
        Assert.Equal(0x1F60Au, ctx.InputEventsQueue[0].Text.Char);
    }
}
