using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class ImStbTextEditTests
{
    [Fact]
    public void Paste_and_backspace()
    {
        var buf = new StbTextBuffer("hi");
        var state = new StbTexteditState();
        ImStbTextEdit.stb_textedit_initialize_state(ref state, single_line: true);
        state.Cursor = buf.Length;

        Assert.True(ImStbTextEdit.stb_textedit_paste(buf, ref state, " there"));
        Assert.Equal("hi there", buf.ToString());

        ImStbTextEdit.stb_textedit_key(buf, ref state, ImStbTextEdit.STB_TEXTEDIT_K_BACKSPACE);
        Assert.Equal("hi ther", buf.ToString());
    }

    [Fact]
    public void Selection_cut()
    {
        var buf = new StbTextBuffer("hello");
        var state = new StbTexteditState { Cursor = 1, SelectStart = 1, SelectEnd = 4 };

        Assert.True(ImStbTextEdit.stb_textedit_cut(buf, ref state, out var cut));
        Assert.Equal("ell", cut);
        Assert.Equal("ho", buf.ToString());
        Assert.Equal(1, state.Cursor);
    }
}
