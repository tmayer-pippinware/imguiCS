using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ImGui;

/// <summary>
/// Minimal managed port of imstb_textedit.h to support InputText-style editing.
/// This implementation focuses on correctness over strict feature parity (undo/redo is not implemented yet).
/// </summary>
public static class ImStbTextEdit
{
    // Key constants (mirroring stb_textedit.h expectation)
    public const int STB_TEXTEDIT_K_LEFT = 1;
    public const int STB_TEXTEDIT_K_RIGHT = 2;
    public const int STB_TEXTEDIT_K_UP = 3;
    public const int STB_TEXTEDIT_K_DOWN = 4;
    public const int STB_TEXTEDIT_K_DELETE = 5;
    public const int STB_TEXTEDIT_K_BACKSPACE = 6;
    public const int STB_TEXTEDIT_K_LINESTART = 7;
    public const int STB_TEXTEDIT_K_LINEEND = 8;
    public const int STB_TEXTEDIT_K_TEXTSTART = 9;
    public const int STB_TEXTEDIT_K_TEXTEND = 10;
    public const int STB_TEXTEDIT_K_PGUP = 11;
    public const int STB_TEXTEDIT_K_PGDOWN = 12;
    public const int STB_TEXTEDIT_K_SHIFT = 0x40000000;

    public static void stb_textedit_initialize_state(ref StbTexteditState state, bool single_line)
    {
        state = new StbTexteditState
        {
            Cursor = 0,
            SelectStart = 0,
            SelectEnd = 0,
            InsertMode = 0,
            RowCountPerPage = single_line ? 1 : 16,
            CursorAtEndOfLine = 0,
            HasPreferredX = 0,
            PreferredX = 0.0f
        };
    }

    public static void stb_textedit_click(IStbTextEditString str, ref StbTexteditState state, int cursor, bool shiftHeld)
    {
        cursor = Clamp(cursor, 0, str.Length);
        if (shiftHeld)
        {
            state.SelectEnd = cursor;
        }
        else
        {
            state.SelectStart = cursor;
            state.SelectEnd = cursor;
        }
        state.Cursor = cursor;
        state.HasPreferredX = 0;
    }

    public static void stb_textedit_drag(IStbTextEditString str, ref StbTexteditState state, int cursor)
    {
        cursor = Clamp(cursor, 0, str.Length);
        state.Cursor = cursor;
        state.SelectEnd = cursor;
    }

    public static bool stb_textedit_paste(IStbTextEditString str, ref StbTexteditState state, ReadOnlySpan<char> text)
    {
        DeleteSelection(str, ref state);
        if (text.Length == 0)
            return false;

        str.Insert(state.Cursor, text);
        state.Cursor += text.Length;
        state.SelectStart = state.SelectEnd = state.Cursor;
        return true;
    }

    public static bool stb_textedit_cut(IStbTextEditString str, ref StbTexteditState state, out string cutText)
    {
        if (!HasSelection(state))
        {
            cutText = string.Empty;
            return false;
        }

        int a = Math.Min(state.SelectStart, state.SelectEnd);
        int b = Math.Max(state.SelectStart, state.SelectEnd);
        cutText = str.Substring(a, b - a);
        str.Delete(a, b - a);
        state.Cursor = a;
        state.SelectStart = state.SelectEnd = a;
        return true;
    }

    public static bool stb_textedit_key(IStbTextEditString str, ref StbTexteditState state, int key)
    {
        bool selecting = (key & STB_TEXTEDIT_K_SHIFT) != 0;
        key &= ~STB_TEXTEDIT_K_SHIFT;

        switch (key)
        {
            case STB_TEXTEDIT_K_LEFT:
                return MoveCursor(str, ref state, -1, selecting);
            case STB_TEXTEDIT_K_RIGHT:
                return MoveCursor(str, ref state, +1, selecting);
            case STB_TEXTEDIT_K_LINESTART:
            case STB_TEXTEDIT_K_TEXTSTART:
                return MoveCursorTo(str, ref state, 0, selecting);
            case STB_TEXTEDIT_K_LINEEND:
            case STB_TEXTEDIT_K_TEXTEND:
                return MoveCursorTo(str, ref state, str.Length, selecting);
            case STB_TEXTEDIT_K_BACKSPACE:
                return Backspace(str, ref state);
            case STB_TEXTEDIT_K_DELETE:
                return Delete(str, ref state);
            default:
                return false;
        }
    }

    private static bool MoveCursor(IStbTextEditString str, ref StbTexteditState state, int delta, bool selecting)
    {
        int dest = Clamp(state.Cursor + delta, 0, str.Length);
        return MoveCursorTo(str, ref state, dest, selecting);
    }

    private static bool MoveCursorTo(IStbTextEditString str, ref StbTexteditState state, int dest, bool selecting)
    {
        dest = Clamp(dest, 0, str.Length);
        state.Cursor = dest;
        if (selecting)
        {
            state.SelectEnd = dest;
        }
        else
        {
            state.SelectStart = dest;
            state.SelectEnd = dest;
        }
        return true;
    }

    private static bool Backspace(IStbTextEditString str, ref StbTexteditState state)
    {
        if (HasSelection(state))
        {
            DeleteSelection(str, ref state);
            return true;
        }

        if (state.Cursor == 0)
            return false;

        str.Delete(state.Cursor - 1, 1);
        state.Cursor--;
        state.SelectStart = state.SelectEnd = state.Cursor;
        return true;
    }

    private static bool Delete(IStbTextEditString str, ref StbTexteditState state)
    {
        if (HasSelection(state))
        {
            DeleteSelection(str, ref state);
            return true;
        }

        if (state.Cursor >= str.Length)
            return false;

        str.Delete(state.Cursor, 1);
        state.SelectStart = state.SelectEnd = state.Cursor;
        return true;
    }

    private static void DeleteSelection(IStbTextEditString str, ref StbTexteditState state)
    {
        if (!HasSelection(state))
            return;

        int a = Math.Min(state.SelectStart, state.SelectEnd);
        int b = Math.Max(state.SelectStart, state.SelectEnd);
        str.Delete(a, b - a);
        state.Cursor = a;
        state.SelectStart = state.SelectEnd = a;
    }

    private static bool HasSelection(in StbTexteditState state) => state.SelectStart != state.SelectEnd;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
}

/// <summary>
/// Represents a mutable text buffer for stb_textedit operations.
/// </summary>
public interface IStbTextEditString
{
    int Length { get; }
    char this[int index] { get; set; }
    void Insert(int index, ReadOnlySpan<char> text);
    void Delete(int index, int count);
    string Substring(int start, int length);
}

/// <summary>
/// Holds editing state (cursor, selection).
/// </summary>
public struct StbTexteditState
{
    public int Cursor;
    public int SelectStart;
    public int SelectEnd;
    public byte InsertMode;
    public byte CursorAtEndOfLine;
    public byte HasPreferredX;
    public float PreferredX;
    public int RowCountPerPage;
}

/// <summary>
/// Simple text buffer implementation used for tests and InputText backing until a tighter integration is needed.
/// </summary>
public sealed class StbTextBuffer : IStbTextEditString
{
    private readonly System.Text.StringBuilder _builder;

    public StbTextBuffer(string initial = "")
    {
        _builder = new System.Text.StringBuilder(initial);
    }

    public int Length => _builder.Length;

    public char this[int index]
    {
        get => _builder[index];
        set => _builder[index] = value;
    }

    public void Insert(int index, ReadOnlySpan<char> text)
    {
        if ((uint)index > (uint)_builder.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        _builder.Insert(index, text);
    }

    public void Delete(int index, int count)
    {
        if (count == 0)
            return;
        if ((uint)index > (uint)_builder.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        count = Math.Min(count, _builder.Length - index);
        _builder.Remove(index, count);
    }

    public override string ToString() => _builder.ToString();

    public string Substring(int start, int length) => _builder.ToString(start, length);
}
