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
        var ctx = ImGui.GetCurrentContext()!;
        ctx.InputEventsQueue.Clear();
        io.InputQueueCharacters.Clear();

        ImGui.AddInputCharacter('A');
        ImGui.AddInputCharacter('B');
        ImGui.NewFrame();

        Assert.Equal(2, io.InputQueueCharacters.Count);
        Assert.Equal((uint)'A', io.InputQueueCharacters[0]);
        Assert.Equal((uint)'B', io.InputQueueCharacters[1]);
    }

    [Fact]
    public void AddInputCharacterUTF16_handles_surrogates()
    {
        ImGui.CreateContext();
        ref var io = ref ImGui.GetIO();

        // high surrogate followed by low surrogate (unicode smiley U+1F60A)
        io.AddInputCharacterUTF16(0xD83D);
        io.AddInputCharacterUTF16(0xDE0A);
        ImGui.NewFrame();

        var ctx = ImGui.GetCurrentContext()!;
        Assert.Empty(ctx.InputEventsQueue);
        Assert.Single(io.InputQueueCharacters);
        Assert.Equal(0x1F60Au, io.InputQueueCharacters[0]);
    }

    [Fact]
    public void AddKeyEvent_updates_mods_after_new_frame()
    {
        ImGui.CreateContext();
        ref var io = ref ImGui.GetIO();
        io.AddKeyEvent(ImGuiKey.ImGuiKey_LeftCtrl, true);
        ImGui.NewFrame();

        Assert.True(io.KeyCtrl);
        Assert.True((io.KeyMods & (int)ImGuiKey.ImGuiMod_Ctrl) != 0);

        io.AddKeyEvent(ImGuiKey.ImGuiKey_LeftCtrl, false);
        ImGui.NewFrame();
        Assert.False(io.KeyCtrl);
    }

    [Fact]
    public void Focus_event_clears_inputs_on_loss()
    {
        ImGui.CreateContext();
        ref var io = ref ImGui.GetIO();
        io.AddMouseButtonEvent(0, true);
        io.AddFocusEvent(false);
        ImGui.NewFrame();
        Assert.False(io.MouseDown[0]);
        Assert.True(io.AppFocusLost);
    }
}
