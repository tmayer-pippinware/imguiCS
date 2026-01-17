using System.Runtime.InteropServices;

namespace ImGui;

public enum ImGuiInputEventType
{
    None = 0,
    Text = 1
}

public enum ImGuiInputSource
{
    None = 0,
    Mouse,
    Keyboard,
    Gamepad,
    Clipboard
}

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiInputEvent
{
    public ImGuiInputEventType Type;
    public ImGuiInputSource Source;
    public uint EventId;
    public ImGuiInputEventText Text;
}

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiInputEventText
{
    public uint Char;
}
