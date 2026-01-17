using System.Runtime.InteropServices;

namespace ImGui;

public enum ImGuiInputEventType
{
    None = 0,
    Text,
    MousePos,
    MouseButton,
    MouseWheel,
    MouseSource,
    Key,
    Focus
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
    public ImGuiMouseSource MouseSource;
    public ImGuiInputEventText Text;
    public ImGuiInputEventMousePos MousePos;
    public ImGuiInputEventMouseButton MouseButton;
    public ImGuiInputEventMouseWheel MouseWheel;
    public ImGuiInputEventKey Key;
    public ImGuiInputEventFocus Focus;
}

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiInputEventText
{
    public uint Char;
}

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiInputEventMousePos
{
    public ImVec2 Pos;
}

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiInputEventMouseButton
{
    public int Button;
    public bool Down;
}

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiInputEventMouseWheel
{
    public float WheelX;
    public float WheelY;
}

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiInputEventKey
{
    public ImGuiKey Key;
    public bool Down;
    public float AnalogValue;
}

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiInputEventFocus
{
    public bool Focused;
}
