global using ImS8 = sbyte;
global using ImU8 = byte;
global using ImS16 = short;
global using ImU16 = ushort;
global using ImS32 = int;
global using ImU32 = uint;
global using ImS64 = long;
global using ImU64 = ulong;
global using ImTextureID = nint;
global using ImGuiID = uint;
global using ImGuiKeyChord = int;
global using ImFileHandle = nint;
global using ImWchar = char;
global using ImWchar16 = ushort;
global using ImWchar32 = uint;
global using ImDrawIdx = ushort;
global using ImGuiCol = int; // convenience when treating enums as ints

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ImGui;

/// <summary>
/// 2D vector (matches ImVec2 layout).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ImVec2
{
    public float x;
    public float y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImVec2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public static ImVec2 Zero => new(0f, 0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImVec2 operator +(ImVec2 a, ImVec2 b) => new(a.x + b.x, a.y + b.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImVec2 operator -(ImVec2 a, ImVec2 b) => new(a.x - b.x, a.y - b.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImVec2 operator *(ImVec2 a, float b) => new(a.x * b, a.y * b);
}

/// <summary>
/// 4D vector (matches ImVec4 layout).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ImVec4
{
    public float x;
    public float y;
    public float z;
    public float w;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImVec4(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
}
